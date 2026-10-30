using BSE.Host.Cache;
using BSE.Host.Authentication;
using BSE.Host.HealthChecks;
using BSE.Host.Middleware;
using BSE.Infrastructure;
using BSE.Infrastructure.Cache;
using BSE.SharedKernel;
using BSE.Modules.AuditLog;
using BSE.Modules.Batch;
using BSE.Modules.AdnsExport;
using BSE.Modules.AnimalRelations;
using BSE.Modules.BsessIntegration;
using BSE.Modules.CaseManagement;
using BSE.Modules.CaseWork;
using BSE.Modules.OssExport;
using BSE.Modules.FarmManagement;
using BSE.Modules.ReferenceData;
using BSE.Modules.Search;
using BSE.Modules.UserManagement;
using BSE.Modules.UserManagement.Identity;
using BSE.Host.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Identity.Web;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Json;
using Serilog.Sinks.ApplicationInsights.TelemetryConverters;

// Bootstrap logger captures startup errors before full Serilog is configured.
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    const string UnauthorizedPath = "/Unauthorized";
    const string SessionErrorPath = "/SessionError";
    const string HomePath = "/Home";
    const string ErrorPath = "/Error";
    const string SessionCookieName = "BSE.Session";
    const string AuthCookieName = "BSE.Auth";

    var builder = WebApplication.CreateBuilder(args);

    builder.WebHost.ConfigureKestrel(options => options.AddServerHeader = false);

    // ── Structured logging ──────────────────────────────────────────────────
    builder.Host.UseSerilog((context, _, config) =>
    {
        var appInsightsConnectionString = context.Configuration["ApplicationInsights:ConnectionString"];

        config
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
            .ReadFrom.Configuration(context.Configuration)   // ← picks up Serilog:MinimumLevel:Override from appsettings
            .Enrich.FromLogContext();

        var isNonProduction = !context.HostingEnvironment.IsProduction();
        var hasAppInsights = !string.IsNullOrWhiteSpace(appInsightsConnectionString);

        if (isNonProduction && hasAppInsights)
        {
            config.WriteTo.ApplicationInsights(appInsightsConnectionString!, TelemetryConverter.Traces);
            return;
        }

        // Structured JSON in non-Development environments; plain text locally.
        if (context.HostingEnvironment.IsDevelopment())
            config.WriteTo.Console();
        else
            config.WriteTo.Console(new JsonFormatter());

        config.WriteTo.File("logs/bse-.txt", rollingInterval: RollingInterval.Day);
    });

    // ── Data access infrastructure ──────────────────────────────────────────
    builder.Services.AddSingleton<IDbConnectionFactory, SqlConnectionFactory>();
    builder.Services.AddScoped<DapperRepository>();
    builder.Services.AddScoped<IDbRepository>(sp =>
        new LoggingDbRepositoryDecorator(
            sp.GetRequiredService<DapperRepository>(),
            sp.GetRequiredService<ILogger<LoggingDbRepositoryDecorator>>(),
            sp.GetRequiredService<IConfiguration>()));

    // ── Distributed cache (Redis primary / MemoryCache fallback) ────────────
    // When Redis__ConnectionString is set the app runs in distributed mode;
    // otherwise it falls back to MemoryCache (single-replica, local dev safe).
    var redisConnectionString = builder.Configuration["Redis:ConnectionString"];
    if (!string.IsNullOrWhiteSpace(redisConnectionString))
    {
        var redisCache = new Microsoft.Extensions.Caching.StackExchangeRedis.RedisCache(
            Microsoft.Extensions.Options.Options.Create(
                new Microsoft.Extensions.Caching.StackExchangeRedis.RedisCacheOptions
                {
                    Configuration = redisConnectionString,
                    InstanceName = "bse:"
                }));
        var memoryCache = new Microsoft.Extensions.Caching.Distributed.MemoryDistributedCache(
            Microsoft.Extensions.Options.Options.Create(
                new Microsoft.Extensions.Caching.Memory.MemoryDistributedCacheOptions()));
        builder.Services.AddSingleton<ResilientDistributedCache>(sp =>
            new ResilientDistributedCache(
                redisCache, memoryCache,
                sp.GetRequiredService<ILogger<ResilientDistributedCache>>()));
        builder.Services.AddSingleton<Microsoft.Extensions.Caching.Distributed.IDistributedCache>(
            sp => sp.GetRequiredService<ResilientDistributedCache>());
    }
    else
    {
        builder.Services.AddDistributedMemoryCache();
    }

    builder.Services.AddSingleton<ICacheKeyProvider, CacheKeyProvider>();

    // ── Reference Data module ───────────────────────────────────────────────
    builder.Services.AddReferenceDataModule(builder.Configuration);

    // ── User Management module ──────────────────────────────────────────────
    builder.Services.AddUserManagementModule(builder.Configuration);

    // ── Audit Log module ────────────────────────────────────────────────────
    builder.Services.AddAuditLogModule();

    // ── Farm Management module ──────────────────────────────────────────────
    builder.Services.AddFarmManagementModule();

    // ── Search module ───────────────────────────────────────────────────────
    builder.Services.AddSearchModule(builder.Configuration);

    // ── Batch module ────────────────────────────────────────────────────────
    builder.Services.AddBatchModule();

    // ── Case Management module ───────────────────────────────────────────
    builder.Services.AddCaseManagementModule();

    // ── Animal Relations module ──────────────────────────────────────────
    builder.Services.AddAnimalRelationsModule();

    // ── CaseWork module ──────────────────────────────────────────────
    builder.Services.AddCaseWorkModule();

    // ── ADNS Export module ─────────────────────────────────────────
    builder.Services.AddAdnsExportModule(builder.Configuration);

    // ── OSS Export module ──────────────────────────────────────────
    builder.Services.AddOssExportModule();

    // ── BSESS Integration module ───────────────────────────────────
    // Replaces the legacy BSESS Import.dtsx SSIS package.
    // Set source connection string via: BsessEtl__SourceConnectionString
    builder.Services.AddBsessIntegrationModule(builder.Configuration);

    // ── Health checks ───────────────────────────────────────────────────────
    // /health/live  — liveness: always 200 (no checks evaluated)
    // /health/ready — readiness: SQL Server connectivity + cache tier
    //   SQL:   Unhealthy (503) when SQL is unreachable — no traffic can be served
    //   Redis: Degraded (200) when Redis is unreachable — traffic continues via MemoryCache
    builder.Services.AddHealthChecks()
        .AddCheck<SqlReadinessCheck>("sql", tags: ["ready"])
        .AddCheck<RedisReadinessCheck>("redis", tags: ["ready"]);

    // ── Authentication ──────────────────────────────────────────────────────────
    // Two paths:
    //   Authentication:BypassEnabled = true  — DevelopmentAuthHandler (local, no Entra)
    //   Authentication:BypassEnabled = false — OIDC via Microsoft.Identity.Web + Entra ID
    //
    // OIDC config is read from the AzureAd section in appsettings.json /
    // appsettings.{Environment}.json or environment variables AzureAd__*.

    var bypassAuthentication = builder.Environment.IsDevelopment()
        && builder.Configuration.GetValue<bool>("Authentication:BypassEnabled");

    // One idle timeout drives both the auth cookie and the ASP.NET session so they expire together.
    var idleTimeout = TimeSpan.FromMinutes(
        Math.Max(1, builder.Configuration.GetValue("Session:IdleTimeoutMinutes", 20)));

    // Shared key ring so auth/session cookies survive restarts and scale-out (e.g. a /home share on App Service).
    var dataProtectionKeysPath = builder.Configuration["DataProtection:KeysPath"];
    if (!string.IsNullOrWhiteSpace(dataProtectionKeysPath))
    {
        builder.Services.AddDataProtection()
            .SetApplicationName("BSE")
            .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionKeysPath));
    }

    builder.Services.Configure<AzureAdOidcConfiguration>(
        builder.Configuration.GetSection(AzureAdOidcConfiguration.SectionName));

    builder.Services.AddSingleton<ISignInSessionRegistry, SignInSessionRegistry>();

    var cookieSecurePolicy = builder.Environment.IsDevelopment()
        ? CookieSecurePolicy.SameAsRequest
        : CookieSecurePolicy.Always;

    builder.Services.AddAntiforgery(options =>
    {
        options.HeaderName = "RequestVerificationToken";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = cookieSecurePolicy;
    });

    if (bypassAuthentication)
    {
        builder.Services
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = DevelopmentAuthHandler.SchemeName;
                options.DefaultChallengeScheme    = DevelopmentAuthHandler.SchemeName;
                options.DefaultScheme             = DevelopmentAuthHandler.SchemeName;
            })
            .AddScheme<DevelopmentAuthOptions, DevelopmentAuthHandler>(
                DevelopmentAuthHandler.SchemeName,
                opts =>
                {
                    opts.Email              = builder.Configuration["Authentication:DevUserEmail"] ?? "dev-user@defra.gov.uk";
                    opts.UseWindowsIdentity = builder.Configuration.GetValue<bool>("Authentication:UseWindowsIdentity", defaultValue: true);
                });
    }
    else
    {
        // ── SAML 2.0 / Entra ID ────────────────────────────────────────────
        var azureAdConfig = builder.Configuration
            .GetSection(AzureAdOidcConfiguration.SectionName)
            .Get<AzureAdOidcConfiguration>() ?? new AzureAdOidcConfiguration();

        builder.Services
            .AddAuthentication(options =>
            {
                options.DefaultScheme          = CookieAuthenticationDefaults.AuthenticationScheme;
                // Cookie challenge runs OnRedirectToLogin (timeout page, else Entra); an OIDC default would skip it.
                options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            })
            .AddMicrosoftIdentityWebApp(
                options =>
                {
                options.Instance              = azureAdConfig.Instance;
                options.TenantId              = azureAdConfig.TenantId;
                options.ClientId              = azureAdConfig.ClientId;
                options.ClientSecret          = azureAdConfig.ClientSecret;
                options.CallbackPath          = azureAdConfig.CallbackPath;
                options.SignedOutCallbackPath = azureAdConfig.SignedOutCallbackPath;

                // Pin the public origin so the correct redirect_uri is sent to Entra ID
                // regardless of which port/protocol the request arrives on locally, and so the
                // post-authentication redirect never leaks the raw azurewebsites.net hostname
                // (Azure Front Door can resolve Request.Host to it before UseForwardedHeaders() corrects it).
                var publicOrigin = !string.IsNullOrWhiteSpace(azureAdConfig.PublicOrigin)
                    ? new Uri(azureAdConfig.PublicOrigin)
                    : null;

                var originalOnRedirectToIdentityProvider = options.Events.OnRedirectToIdentityProvider;
                options.Events.OnRedirectToIdentityProvider = async ctx =>
                {
                    // Without ForceAuthn (prompt=login), Entra's SSO session silently re-signs the
                    // user in after the idle timeout.
                    if (azureAdConfig.ForceAuthn)
                        ctx.ProtocolMessage.Prompt = "login";

                    ctx.ProtocolMessage.RedirectUri =
                        OidcHelpers.RewriteRedirectUriHost(ctx.ProtocolMessage.RedirectUri, publicOrigin);

                    if (originalOnRedirectToIdentityProvider is not null)
                        await originalOnRedirectToIdentityProvider(ctx);
                };

                // Extract the user's email from the Entra ID OIDC token and add it
                // as the canonical 'emailaddress' claim so GroupClaimsTransformation
                // can look up the user in the database.
                // Token payloads are never logged (GDPR / Defra SDS Logging Standards).
                var originalOnTokenValidated = options.Events.OnTokenValidated;
                options.Events.OnTokenValidated = async ctx =>
                {
                    if (ctx.Principal?.Identity is System.Security.Claims.ClaimsIdentity identity)
                    {
                        var email = OidcHelpers.ResolveEmailClaim(identity);

                        if (!string.IsNullOrWhiteSpace(email)
                            && !identity.HasClaim(c => c.Type == ClaimsUserContext.EmailClaimType))
                        {
                            identity.AddClaim(new System.Security.Claims.Claim(ClaimsUserContext.EmailClaimType, email));
                        }
                    }

                    if (originalOnTokenValidated is not null)
                        await originalOnTokenValidated(ctx);
                };
                },
                cookieOptions =>
                {
                    AuthCookieConfiguration.Apply(cookieOptions, AuthCookieName, idleTimeout, cookieSecurePolicy);

                    cookieOptions.Events.OnRedirectToLogin = async ctx =>
                    {
                        var logger = ctx.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>()
                            .CreateLogger("AuthSecurity");
                        logger.LogWarning(
                            "Authentication challenge redirect for {Path}. ReturnUrl={ReturnUrl}",
                            ctx.Request.Path.Value,
                            ctx.Request.Query["ReturnUrl"].ToString());

                        // A stale auth cookie means the sign-in timed out: tell the user instead of jumping straight to Entra.
                        if (ctx.Request.Cookies.ContainsKey(AuthCookieName)
                            && ctx.Request.Headers.Accept.ToString().Contains("text/html", StringComparison.OrdinalIgnoreCase))
                        {
                            ctx.Response.Redirect(SessionErrorPath);
                            return;
                        }

                        // Return to the page that triggered the challenge after re-authentication (GET only).
                        var returnUri = HttpMethods.IsGet(ctx.Request.Method)
                            ? $"{ctx.Request.PathBase}{ctx.Request.Path}{ctx.Request.QueryString}"
                            : HomePath;

                        await ctx.HttpContext.ChallengeAsync(
                            OpenIdConnectDefaults.AuthenticationScheme,
                            new AuthenticationProperties
                            {
                                RedirectUri = returnUri
                            });
                    };
                    // Redirect authenticated users with insufficient permissions to Unauthorized, not a 403.
                    cookieOptions.Events.OnRedirectToAccessDenied = ctx =>
                    {
                        var logger = ctx.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>()
                            .CreateLogger("AuthSecurity");
                        logger.LogWarning(
                            "Access denied redirect for {Path} (User={User})",
                            ctx.Request.Path.Value,
                            ctx.HttpContext.User.Identity?.Name ?? "unknown");

                        ctx.Response.Redirect(UnauthorizedPath);
                        return Task.CompletedTask;
                    };
                });
    }

    // ── Forwarded headers (service registration) ────────────────────────────────
    // Registered here (before Build) so the options are available to all middleware.
    // dev-bse.azure.defra.cloud terminates TLS and forwards to the App Service's
    // default hostname. Without this, Request.Scheme/Request.Host reflect the
    // raw azurewebsites.net origin, causing absolute redirects to leak that hostname.
    // Azure's edge proxy IPs are not fixed, so KnownIPNetworks/KnownProxies are cleared
    // to trust forwarded headers regardless of hop address — safe because the
    // azurewebsites.net endpoint is access-restricted to Front Door / App Gateway only.
    builder.Services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor
                                 | ForwardedHeaders.XForwardedProto
                                 | ForwardedHeaders.XForwardedHost;
        options.KnownIPNetworks.Clear();
        options.KnownProxies.Clear();
    });

    // ── Cookie redirect URIs — force relative ─────────────────────────────────────
    // The default cookie challenge/forbid handlers build an ABSOLUTE redirect URI from
    // Request.Scheme/Request.Host. Behind a reverse proxy, that can leak the App
    // Service origin hostname instead of the public custom domain. Forcing a relative
    // redirect removes the dependency on Request.Host entirely — the browser resolves
    // it against whatever host is in the address bar. Mirrors Histo.
    builder.Services.PostConfigure<CookieAuthenticationOptions>(
        CookieAuthenticationDefaults.AuthenticationScheme, options =>
    {
        var originalLogin = options.Events.OnRedirectToLogin;
        options.Events.OnRedirectToLogin = context =>
        {
            if (Uri.TryCreate(context.RedirectUri, UriKind.Absolute, out var abs))
                context.RedirectUri = abs.PathAndQuery;
            return originalLogin(context);
        };

        var originalDenied = options.Events.OnRedirectToAccessDenied;
        options.Events.OnRedirectToAccessDenied = context =>
        {
            if (Uri.TryCreate(context.RedirectUri, UriKind.Absolute, out var abs))
                context.RedirectUri = abs.PathAndQuery;
            return originalDenied(context);
        };
    });

    // ── Razor Pages
    builder.Services.AddRazorPages(options =>
    {
        options.Conventions.AuthorizeFolder("/");
        options.Conventions.AllowAnonymousToPage(ErrorPath);
        options.Conventions.AllowAnonymousToPage(SessionErrorPath);
        options.Conventions.AllowAnonymousToPage(UnauthorizedPath);
        options.Conventions.AllowAnonymousToPage("/AccessibilityStatement");
    })
     .AddMvcOptions(o =>
        // ASP.NET Core 6+ treats non-nullable string properties as implicitly [Required]
        // when nullable context is enabled. The legacy .NET Framework app had no such
        // behaviour — all search filter fields are optional. Suppress to match legacy.
        o.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true) ;

    // ── Session support (for grid state persistence in OSS Export and other pages) ──
    builder.Services.AddSession(options =>
    {
        options.IdleTimeout = idleTimeout;
        options.Cookie.Name = SessionCookieName;
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = cookieSecurePolicy;
        options.Cookie.IsEssential = true; // Required for app to function
    });

    // ── Host services ──────────────────────────────────────────────────────────
    builder.Services.AddScoped<BSE.Host.Services.ICurrentUserService, BSE.Host.Services.CurrentUserService>();
    builder.Services.AddScoped<BSE.Host.Services.IGeoLookupService, BSE.Host.Services.GeoLookupService>();
    builder.Services.AddScoped<BSE.Host.Services.ICaseWizardStateService, BSE.Host.Services.CaseWizardStateService>();
    builder.Services.AddScoped<BSE.Host.Services.ICaseEditSessionStateService, BSE.Host.Services.CaseEditSessionStateService>();
    builder.Services.AddScoped<BSE.Host.Services.ICaseDraftDiscardService, BSE.Host.Services.CaseDraftDiscardService>();
    builder.Services.AddScoped<BSE.Host.Services.ICaseEditOrchestrationService, BSE.Host.Services.CaseEditOrchestrationService>();

    // -- Authorisation policies
    // Each policy requires exactly its own name as a role claim.
    // GroupClaimsTransformation currently derives policy claims from a hardcoded switch on luUserGroup.Name
    // Source of truth: docs/Legacy-Page-Level-Access-Control.md (Section 5 access matrix)
    // Redirect authenticated-but-forbidden users to Home instead of returning 403.
    // Applies to both the dev-bypass path and the OIDC path.
    builder.Services.AddSingleton<IAuthorizationMiddlewareResultHandler, HomeRedirectAuthorizationHandler>();

    var authorizationBuilder = builder.Services.AddAuthorizationBuilder();

    authorizationBuilder.AddPolicy("Authenticated", p => p.RequireAuthenticatedUser());
    authorizationBuilder.AddPolicy("ReadOnly", p => p.RequireRole("ReadOnly"));
    authorizationBuilder.AddPolicy("DataEntry", p => p.RequireRole("DataEntry"));
    authorizationBuilder.AddPolicy("DEFRAMaintenance", p => p.RequireRole("DEFRAMaintenance"));
    // Legacy MoveCase.aspx/MoveCaseNewFarm.aspx EnableControls(): DEFRA Maintenance OR VLA Maintenance
    // (docs/Legacy-Page-Level-Access-Control.md section 5.9 "Case Maintenance Operations").
    authorizationBuilder.AddPolicy("DEFRAOrVLAMaintenance", p => p.RequireRole("DEFRAMaintenance", "VLAMaintenance"));
    authorizationBuilder.AddPolicy("DEFRAAccess", p => p.RequireRole("DEFRAAccess"));
    authorizationBuilder.AddPolicy("VLAAccess", p => p.RequireRole("VLAAccess"));
    authorizationBuilder.AddPolicy("VLAMaintenance", p => p.RequireRole("VLAMaintenance"));
    authorizationBuilder.AddPolicy("PickListAccess", p => p.RequireRole("PickListAccess"));
    authorizationBuilder.AddPolicy("FarmCreation", p => p.RequireRole("FarmCreation"));
    authorizationBuilder.AddPolicy("AuditAccess", p => p.RequireRole("DEFRAAccess", "VLAAccess"));

    var app = builder.Build();

    app.UseMiddleware<SecurityHeadersMiddleware>(!app.Environment.IsDevelopment());

    app.UseForwardedHeaders(); // options registered via builder.Services.Configure<ForwardedHeadersOptions> above

    app.UseMiddleware<UnhandledExceptionLoggingMiddleware>();
    app.UseExceptionHandler(ErrorPath);

    var slowRequestThresholdMs = builder.Configuration.GetValue<int?>("Serilog:SlowRequestThresholdMs") ?? 1000;
    app.UseSerilogRequestLogging(options =>
    {
        options.EnrichDiagnosticContext = (diag, http) =>
        {
            diag.Set("RequestHost", http.Request.Host.Value);
            diag.Set("RequestScheme", http.Request.Scheme);
            diag.Set("UserAgent", http.Request.Headers.UserAgent.ToString());
            diag.Set("User", http.User.Identity?.Name ?? "anonymous");
        };

        options.GetLevel = (http, elapsed, ex) =>
        {
            if (ex is not null || http.Response.StatusCode >= 500)
                return LogEventLevel.Error;

            if (elapsed >= slowRequestThresholdMs)
                return LogEventLevel.Warning;

            return LogEventLevel.Information;
        };
    });

    app.UseAuthentication();
    app.UseSession(); // Session middleware must come after Authentication

    app.Use(async (context, next) =>
    {
        var path = context.Request.Path;
        var isExcludedPath =
            path.StartsWithSegments(UnauthorizedPath, StringComparison.OrdinalIgnoreCase)
            || path.StartsWithSegments(ErrorPath, StringComparison.OrdinalIgnoreCase)
            || path.StartsWithSegments(SessionErrorPath, StringComparison.OrdinalIgnoreCase)
            || path.StartsWithSegments("/health", StringComparison.OrdinalIgnoreCase)
            || path.StartsWithSegments("/signin-oidc", StringComparison.OrdinalIgnoreCase)
            || path.StartsWithSegments("/signout-callback-oidc", StringComparison.OrdinalIgnoreCase)
            || path.StartsWithSegments("/signin", StringComparison.OrdinalIgnoreCase)
            || path.StartsWithSegments("/signout", StringComparison.OrdinalIgnoreCase);

        var isStaticAssetRequest = System.IO.Path.HasExtension(path.Value);

        if (!isExcludedPath
            && !isStaticAssetRequest
            && context.User.Identity?.IsAuthenticated == true
            && !context.User.HasClaim(c => c.Type == ClaimsUserContext.BseGroupIdClaimType))
        {
            context.Response.Redirect(UnauthorizedPath);
            return;
        }

        await next();
    });

    // The ASP.NET session cookie outlives its server-side state, so a cookie with no marker means the
    // session idled out. The case/wizard pages only reach /SessionError on a blank RBSE, never on timeout.
    app.Use(async (context, next) =>
    {
        var path = context.Request.Path;
        var isExcludedPath =
            path.StartsWithSegments(UnauthorizedPath, StringComparison.OrdinalIgnoreCase)
            || path.StartsWithSegments(ErrorPath, StringComparison.OrdinalIgnoreCase)
            || path.StartsWithSegments(SessionErrorPath, StringComparison.OrdinalIgnoreCase)
            || path.StartsWithSegments("/health", StringComparison.OrdinalIgnoreCase)
            || path.StartsWithSegments("/signin-oidc", StringComparison.OrdinalIgnoreCase)
            || path.StartsWithSegments("/signout-callback-oidc", StringComparison.OrdinalIgnoreCase)
            || path.StartsWithSegments("/signin", StringComparison.OrdinalIgnoreCase)
            || path.StartsWithSegments("/signout", StringComparison.OrdinalIgnoreCase)
            || System.IO.Path.HasExtension(path.Value);

        var wantsHtml = context.Request.Headers.Accept.ToString().Contains("text/html", StringComparison.OrdinalIgnoreCase);

        if (!isExcludedPath && wantsHtml && context.User.Identity?.IsAuthenticated == true)
        {
            const string marker = "bse.session.started";
            if (context.Session.GetString(marker) is null)
            {
                context.Session.SetString(marker, "1");
                if (context.Request.Cookies.ContainsKey(SessionCookieName))
                {
                    context.Response.Redirect(SessionErrorPath);
                    return;
                }
            }
        }

        await next();
    });

    app.UseAuthorization();

    // Liveness: always returns 200 — no health checks evaluated.
    // AllowAnonymous ensures the liveness probe is reachable even when OIDC is not yet configured.
    app.MapHealthChecks("/health/live", new HealthCheckOptions
    {
        Predicate = _ => false
    }).AllowAnonymous();

    // Readiness: returns 200 when SQL Server is reachable; 503 otherwise.
    app.MapHealthChecks("/health/ready", new HealthCheckOptions
    {
        Predicate = check => check.Tags.Contains("ready")
    }).AllowAnonymous();

    app.UseStaticFiles();
    app.UseMiddleware<UserActivityLoggingMiddleware>();

    app.MapGet("/case/{rbse}/unsaved-status", async (
        string rbse,
        ICaseEditSessionStateService sessionState) =>
    {
        var normalizedRbse = RbseHelper.ParseToRaw(rbse);

        var session = await sessionState.GetAsync(normalizedRbse);
        var hasUnsavedChanges = session is not null
            && (session.HasPendingChanges || session.HasCaseEditChanges || session.HasClinicalVisitsChanges
                || session.HasFarmCollectionsChanges || session.HasFeedsChanges || session.HasRelationsChanges);

        return Results.Json(new { hasUnsavedChanges });
    }).RequireAuthorization();

    app.MapPost("/case/{rbse}/discard-unsaved", async (
        HttpContext httpContext,
        IAntiforgery antiforgery,
        string rbse,
        ICaseDraftDiscardService caseDraftDiscard) =>
    {
        if (!await antiforgery.IsRequestValidAsync(httpContext))
            return Results.BadRequest();

        var normalizedRbse = RbseHelper.ParseToRaw(rbse);

        await caseDraftDiscard.DiscardAllAsync(normalizedRbse);

        return Results.Ok(new { cleared = true });
    }).RequireAuthorization();

    app.MapGet("/", () => Results.Redirect(HomePath));

    // Target of "Sign in again" on the session-timeout page: drops stale cookies, then starts Entra sign-in.
    app.MapGet("/signin", (HttpContext ctx) =>
    {
        ctx.Response.Cookies.Delete(AuthCookieName);
        ctx.Response.Cookies.Delete(SessionCookieName);

        if (bypassAuthentication)
            return Results.Redirect(HomePath);

        return Results.Challenge(
            new AuthenticationProperties { RedirectUri = HomePath },
            [OpenIdConnectDefaults.AuthenticationScheme]);
    }).AllowAnonymous();

    app.MapRazorPages();

    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly.");
}
finally
{
    await Log.CloseAndFlushAsync();
}
