using BSE.Host.Cache;
using BSE.Host.Authentication;
using BSE.Host.Middleware;
using BSE.Infrastructure.Cache;
using BSE.SharedKernel;
using BSE.Modules.UserManagement.Identity;
using BSE.Host.Services;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Identity.Web;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Json;
using Serilog.Sinks.ApplicationInsights.TelemetryConverters;

namespace BSE.Host;

/// <summary>Startup wiring extracted from Program.cs so each concern stays small and readable.</summary>
internal static class HostSetup
{
    internal const string UnauthorizedPath = "/Unauthorized";
    internal const string SessionErrorPath = "/SessionError";
    internal const string HomePath = "/Home";
    internal const string ErrorPath = "/Error";
    internal const string SessionCookieName = "BSE.Session";
    internal const string AuthCookieName = "BSE.Auth";

    // ── Structured logging ──────────────────────────────────────────────────
    internal static void ConfigureSerilog(HostBuilderContext context, LoggerConfiguration config)
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
    }

    // ── Distributed cache (Redis primary / MemoryCache fallback) ────────────
    // When Redis__ConnectionString is set the app runs in distributed mode;
    // otherwise it falls back to MemoryCache (single-replica, local dev safe).
    internal static void AddDistributedCaching(this IServiceCollection services, IConfiguration configuration)
    {
        var redisConnectionString = configuration["Redis:ConnectionString"];
        if (string.IsNullOrWhiteSpace(redisConnectionString))
        {
            services.AddDistributedMemoryCache();
            return;
        }

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
        services.AddSingleton<ResilientDistributedCache>(sp =>
            new ResilientDistributedCache(
                redisCache, memoryCache,
                sp.GetRequiredService<ILogger<ResilientDistributedCache>>()));
        services.AddSingleton<Microsoft.Extensions.Caching.Distributed.IDistributedCache>(
            sp => sp.GetRequiredService<ResilientDistributedCache>());
    }

    // ── Authentication ──────────────────────────────────────────────────────────
    // Two paths:
    //   Authentication:BypassEnabled = true  — DevelopmentAuthHandler (local, no Entra)
    //   Authentication:BypassEnabled = false — OIDC via Microsoft.Identity.Web + Entra ID
    //
    // OIDC config is read from the AzureAd section in appsettings.json /
    // appsettings.{Environment}.json or environment variables AzureAd__*.
    internal static void AddBseAuthentication(
        this WebApplicationBuilder builder, bool bypassAuthentication, TimeSpan idleTimeout, CookieSecurePolicy cookieSecurePolicy)
    {
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

        builder.Services.Configure<Microsoft.AspNetCore.Mvc.CookieTempDataProviderOptions>(options =>
            options.Cookie.SecurePolicy = cookieSecurePolicy);

        builder.Services.AddAntiforgery(options =>
        {
            options.HeaderName = "RequestVerificationToken";
            options.Cookie.HttpOnly = true;
            options.Cookie.SameSite = SameSiteMode.Lax;
            options.Cookie.SecurePolicy = cookieSecurePolicy;
        });

        if (bypassAuthentication)
            AddDevelopmentAuthentication(builder);
        else
            AddEntraAuthentication(builder, idleTimeout, cookieSecurePolicy);
    }

    private static void AddDevelopmentAuthentication(WebApplicationBuilder builder)
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

    // ── OIDC / Entra ID ────────────────────────────────────────────
    private static void AddEntraAuthentication(
        WebApplicationBuilder builder, TimeSpan idleTimeout, CookieSecurePolicy cookieSecurePolicy)
    {
        var azureAdConfig = builder.Configuration
            .GetSection(AzureAdOidcConfiguration.SectionName)
            .Get<AzureAdOidcConfiguration>() ?? new AzureAdOidcConfiguration();

        // TEMPORARY DIAGNOSTIC — remove once the OIDC Key Vault secret
        // resolution investigation is complete. Logs ONLY whether ClientSecret
        // is present/non-empty and its length — never the value itself.
        Log.Information(
            "AzureAd OIDC config at startup: TenantId={TenantId} ClientId={ClientId} " +
            "ClientSecretPresent={ClientSecretPresent} ClientSecretLength={ClientSecretLength} " +
            "ClientSecretLooksLikeUnresolvedKeyVaultRef={LooksUnresolved}",
            azureAdConfig.TenantId,
            azureAdConfig.ClientId,
            !string.IsNullOrEmpty(azureAdConfig.ClientSecret),
            azureAdConfig.ClientSecret?.Length ?? 0,
            azureAdConfig.ClientSecret?.StartsWith("@Microsoft.KeyVault", StringComparison.OrdinalIgnoreCase) ?? false);

        builder.Services
            .AddAuthentication(options =>
            {
                options.DefaultScheme          = CookieAuthenticationDefaults.AuthenticationScheme;
                // Cookie challenge runs OnRedirectToLogin (timeout page, else Entra); an OIDC default would skip it.
                options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            })
            .AddMicrosoftIdentityWebApp(
                options => ConfigureOidc(options, azureAdConfig),
                cookieOptions =>
                {
                    AuthCookieConfiguration.Apply(cookieOptions, AuthCookieName, idleTimeout, cookieSecurePolicy);
                    cookieOptions.Events.OnRedirectToLogin = RedirectToLoginAsync;
                    // Redirect authenticated users with insufficient permissions to Unauthorized, not a 403.
                    cookieOptions.Events.OnRedirectToAccessDenied = RedirectToAccessDenied;
                });
    }

    private static void ConfigureOidc(MicrosoftIdentityOptions options, AzureAdOidcConfiguration azureAdConfig)
    {
        options.Instance              = azureAdConfig.Instance;
        options.TenantId              = azureAdConfig.TenantId;
        options.ClientId              = azureAdConfig.ClientId;
        options.ClientSecret          = azureAdConfig.ClientSecret;
        options.CallbackPath          = azureAdConfig.CallbackPath;
        options.SignedOutCallbackPath = azureAdConfig.SignedOutCallbackPath;

        // If this app registration has an optional-claims / claims-mapping policy assigned,
        // Entra ID signs its tokens with an app-specific key that is NOT present in the
        // tenant-wide JWKS document. Requesting the metadata with ?appid=<ClientId> makes
        // Entra return the app-specific signing keys instead, which is required for
        // signature validation to succeed in that scenario (IDX10503 otherwise).
        options.MetadataAddress =
            $"{azureAdConfig.Instance.TrimEnd('/')}/{azureAdConfig.TenantId}/v2.0/.well-known/openid-configuration?appid={azureAdConfig.ClientId}";

        // Force the pure authorization code flow explicitly rather than relying on
        // Microsoft.Identity.Web's auto-detection, which has been observed to fall back
        // to pure implicit flow (response_type=id_token) in some cases. With response_type=code,
        // the id_token/access_token are only ever returned via the back-channel token exchange
        // (using ClientSecret), never in the front-channel redirect — so the "ID tokens
        // (implicit/hybrid flows)" setting in the Entra App Registration is not required.
        options.ResponseType = Microsoft.IdentityModel.Protocols.OpenIdConnect.OpenIdConnectResponseType.Code;
        options.UsePkce      = true;
        options.SaveTokens   = true;

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

            // Force the pure authorization code flow on the outgoing request itself.
            // Setting options.ResponseType above is not sufficient — Microsoft.Identity.Web
            // re-derives/overwrites ProtocolMessage.ResponseType internally before this event runs,
            // so it must be reasserted here, immediately before the request is sent to Entra ID.
            ctx.ProtocolMessage.ResponseType =
                Microsoft.IdentityModel.Protocols.OpenIdConnect.OpenIdConnectResponseType.Code;

            ctx.ProtocolMessage.RedirectUri =
                OidcHelpers.RewriteRedirectUriHost(ctx.ProtocolMessage.RedirectUri, publicOrigin);

            if (originalOnRedirectToIdentityProvider is not null)
                await originalOnRedirectToIdentityProvider(ctx);
        };

        var originalOnTokenValidated = options.Events.OnTokenValidated;
        options.Events.OnTokenValidated = async ctx =>
        {
            AddEmailClaim(ctx);

            if (originalOnTokenValidated is not null)
                await originalOnTokenValidated(ctx);
        };
    }

    // Extract the user's email from the Entra ID OIDC token and add it
    // as the canonical 'emailaddress' claim so GroupClaimsTransformation
    // can look up the user in the database.
    // Token payloads are never logged (GDPR / Defra SDS Logging Standards).
    private static void AddEmailClaim(TokenValidatedContext ctx)
    {
        if (ctx.Principal?.Identity is not System.Security.Claims.ClaimsIdentity identity)
            return;

        var email = OidcHelpers.ResolveEmailClaim(identity);

        if (!string.IsNullOrWhiteSpace(email)
            && !identity.HasClaim(c => c.Type == ClaimsUserContext.EmailClaimType))
        {
            identity.AddClaim(new System.Security.Claims.Claim(ClaimsUserContext.EmailClaimType, email));
        }
    }

    private static async Task RedirectToLoginAsync(RedirectContext<CookieAuthenticationOptions> ctx)
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
    }

    private static Task RedirectToAccessDenied(RedirectContext<CookieAuthenticationOptions> ctx)
    {
        var logger = ctx.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>()
            .CreateLogger("AuthSecurity");
        logger.LogWarning(
            "Access denied redirect for {Path} (User={User})",
            ctx.Request.Path.Value,
            ctx.HttpContext.User.Identity?.Name ?? "unknown");

        ctx.Response.Redirect(UnauthorizedPath);
        return Task.CompletedTask;
    }

    // ── Forwarded headers (service registration) ────────────────────────────────
    // Registered here (before Build) so the options are available to all middleware.
    // dev-bse.azure.defra.cloud terminates TLS and forwards to the App Service's
    // default hostname. Without this, Request.Scheme/Request.Host reflect the
    // raw azurewebsites.net origin, causing absolute redirects to leak that hostname.
    // Azure's edge proxy IPs are not fixed, so KnownIPNetworks/KnownProxies are cleared
    // to trust forwarded headers regardless of hop address — safe because the
    // azurewebsites.net endpoint is access-restricted to Front Door / App Gateway only.
    //
    // Cookie redirect URIs are forced relative: the default cookie challenge/forbid handlers
    // build an ABSOLUTE redirect URI from Request.Scheme/Request.Host, which behind a reverse
    // proxy can leak the App Service origin hostname instead of the public custom domain.
    // Mirrors Histo.
    internal static void AddProxyAwareness(this IServiceCollection services)
    {
        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor
                                     | ForwardedHeaders.XForwardedProto
                                     | ForwardedHeaders.XForwardedHost;
            options.KnownIPNetworks.Clear();
            options.KnownProxies.Clear();
        });

        services.PostConfigure<CookieAuthenticationOptions>(
            CookieAuthenticationDefaults.AuthenticationScheme, options =>
        {
            var originalLogin = options.Events.OnRedirectToLogin;
            options.Events.OnRedirectToLogin = context =>
            {
                MakeRedirectRelative(context);
                return originalLogin(context);
            };

            var originalDenied = options.Events.OnRedirectToAccessDenied;
            options.Events.OnRedirectToAccessDenied = context =>
            {
                MakeRedirectRelative(context);
                return originalDenied(context);
            };
        });
    }

    private static void MakeRedirectRelative(RedirectContext<CookieAuthenticationOptions> context)
    {
        if (Uri.TryCreate(context.RedirectUri, UriKind.Absolute, out var abs))
            context.RedirectUri = abs.PathAndQuery;
    }

    // -- Authorisation policies
    // Each policy requires exactly its own name as a role claim.
    // GroupClaimsTransformation currently derives policy claims from a hardcoded switch on luUserGroup.Name
    // Source of truth: docs/Legacy-Page-Level-Access-Control.md (Section 5 access matrix)
    // Redirect authenticated-but-forbidden users to Home instead of returning 403.
    // Applies to both the dev-bypass path and the OIDC path.
    private const string VlaAccessPolicy = "VLAAccess";
    private const string DataEntryPolicy = "DataEntry";
    private const string VlaMaintenancePolicy = "VLAMaintenance";
    private const string DefraAccessPolicy = "DEFRAAccess";

    internal static void AddBseAuthorization(this IServiceCollection services)
    {
        services.AddSingleton<IAuthorizationMiddlewareResultHandler, HomeRedirectAuthorizationHandler>();

        var authorizationBuilder = services.AddAuthorizationBuilder();

        authorizationBuilder.AddPolicy("Authenticated", p => p.RequireAuthenticatedUser());
        authorizationBuilder.AddPolicy("ReadOnly", p => p.RequireRole("ReadOnly"));
        authorizationBuilder.AddPolicy(DataEntryPolicy, p => p.RequireRole(DataEntryPolicy));
        // Chained RequireRole calls are ANDed: the user must hold both roles.
        authorizationBuilder.AddPolicy("DataEntryVla", p => p.RequireRole(DataEntryPolicy).RequireRole(VlaAccessPolicy));
        authorizationBuilder.AddPolicy("DEFRAMaintenance", p => p.RequireRole("DEFRAMaintenance"));
        // Legacy MoveCase.aspx/MoveCaseNewFarm.aspx EnableControls(): DEFRA Maintenance OR VLA Maintenance
        // (docs/Legacy-Page-Level-Access-Control.md section 5.9 "Case Maintenance Operations").
        authorizationBuilder.AddPolicy("DEFRAOrVLAMaintenance", p => p.RequireRole("DEFRAMaintenance", VlaMaintenancePolicy));
        authorizationBuilder.AddPolicy(DefraAccessPolicy, p => p.RequireRole(DefraAccessPolicy));
        authorizationBuilder.AddPolicy(VlaAccessPolicy, p => p.RequireRole(VlaAccessPolicy));
        authorizationBuilder.AddPolicy(VlaMaintenancePolicy, p => p.RequireRole(VlaMaintenancePolicy));
        authorizationBuilder.AddPolicy("PickListAccess", p => p.RequireRole("PickListAccess"));
        authorizationBuilder.AddPolicy("FarmCreation", p => p.RequireRole("FarmCreation"));
        authorizationBuilder.AddPolicy("AuditAccess", p => p.RequireRole(DefraAccessPolicy, VlaAccessPolicy));
        // Case-entry tabs: only the recognised DEFRA/VLA groups (AHO and AI Wales/Scotland groups are excluded).
        authorizationBuilder.AddPolicy("CaseEntryAccess", p => p.RequireRole(DefraAccessPolicy, VlaAccessPolicy));
        // Joint farm fields locked for VLA Data Entry: DEFRA groups or VLA Maintenance with DataEntry.
        authorizationBuilder.AddPolicy("FarmDefraEdit", p => p
            .RequireRole(DataEntryPolicy)
            .RequireAssertion(ctx => !ctx.User.IsInRole(VlaAccessPolicy) || ctx.User.IsInRole(VlaMaintenancePolicy)));
        // Sire/other-relations section: VLA-only, never a DEFRA-named group.
        authorizationBuilder.AddPolicy("VlaSireEdit", p => p
            .RequireRole(DataEntryPolicy)
            .RequireRole(VlaAccessPolicy)
            .RequireAssertion(ctx => ctx.User.FindFirst("bse:group")?.Value is not ("DEFRA Data Entry" or "DEFRA Maintenance" or "DEFRA Viewer")));
    }

    internal static void UseBseRequestLogging(this WebApplication app, int slowRequestThresholdMs)
    {
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
    }

    private static bool IsExcludedPath(PathString path) =>
        path.StartsWithSegments(UnauthorizedPath, StringComparison.OrdinalIgnoreCase)
        || path.StartsWithSegments(ErrorPath, StringComparison.OrdinalIgnoreCase)
        || path.StartsWithSegments(SessionErrorPath, StringComparison.OrdinalIgnoreCase)
        || path.StartsWithSegments("/health", StringComparison.OrdinalIgnoreCase)
        || path.StartsWithSegments("/signin-oidc", StringComparison.OrdinalIgnoreCase)
        || path.StartsWithSegments("/signout-callback-oidc", StringComparison.OrdinalIgnoreCase)
        || path.StartsWithSegments("/signin", StringComparison.OrdinalIgnoreCase)
        || path.StartsWithSegments("/signout", StringComparison.OrdinalIgnoreCase);

    /// <summary>Redirects authenticated users who have no BSE group claim to the Unauthorized page.</summary>
    internal static void UseGroupClaimGuard(this WebApplication app)
    {
        app.Use(async (context, next) =>
        {
            var isStaticAssetRequest = System.IO.Path.HasExtension(context.Request.Path.Value);

            if (!IsExcludedPath(context.Request.Path)
                && !isStaticAssetRequest
                && context.User.Identity?.IsAuthenticated == true
                && !context.User.HasClaim(c => c.Type == ClaimsUserContext.BseGroupIdClaimType))
            {
                context.Response.Redirect(UnauthorizedPath);
                return;
            }

            await next();
        });
    }

    // The ASP.NET session cookie outlives its server-side state, so a cookie with no marker means the
    // session idled out. The case/wizard pages only reach /SessionError on a blank RBSE, never on timeout.
    internal static void UseSessionTimeoutGuard(this WebApplication app)
    {
        app.Use(async (context, next) =>
        {
            var path = context.Request.Path;
            var isExcludedPath = IsExcludedPath(path) || System.IO.Path.HasExtension(path.Value);
            var wantsHtml = context.Request.Headers.Accept.ToString().Contains("text/html", StringComparison.OrdinalIgnoreCase);

            if (!isExcludedPath && wantsHtml && context.User.Identity?.IsAuthenticated == true
                && IsSessionTimedOut(context))
            {
                context.Response.Redirect(SessionErrorPath);
                return;
            }

            await next();
        });
    }

    private static bool IsSessionTimedOut(HttpContext context)
    {
        const string marker = "bse.session.started";
        if (context.Session.GetString(marker) is not null)
            return false;

        context.Session.SetString(marker, "1");
        return context.Request.Cookies.ContainsKey(SessionCookieName);
    }

    internal static void MapBseEndpoints(this WebApplication app, bool bypassAuthentication)
    {
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

        // Header sign-out: clears the app cookies, then ends the Entra session when OIDC is in use.
        app.MapPost("/signout", async (HttpContext ctx, IAntiforgery antiforgery) =>
        {
            if (!await antiforgery.IsRequestValidAsync(ctx))
                return Results.BadRequest();

            ctx.Session.Clear();
            ctx.Response.Cookies.Delete(SessionCookieName);

            var properties = new AuthenticationProperties { RedirectUri = HomePath };
            return bypassAuthentication
                ? Results.SignOut(properties, [CookieAuthenticationDefaults.AuthenticationScheme])
                : Results.SignOut(properties, [CookieAuthenticationDefaults.AuthenticationScheme, OpenIdConnectDefaults.AuthenticationScheme]);
        }).RequireAuthorization();
    }
}
