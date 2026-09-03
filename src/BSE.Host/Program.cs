using BSE.Host.Cache;
using BSE.Host.Authentication;
using BSE.Host.HealthChecks;
using BSE.Infrastructure;
using BSE.Infrastructure.Cache;
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
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using Sustainsys.Saml2;
using Sustainsys.Saml2.AspNetCore2;
using Sustainsys.Saml2.Metadata;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Json;

// Bootstrap logger captures startup errors before full Serilog is configured.
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // ── Structured logging ──────────────────────────────────────────────────
    builder.Host.UseSerilog((context, config) =>
    {
        config
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
            .ReadFrom.Configuration(context.Configuration)   // ← picks up Serilog:MinimumLevel:Override from appsettings
            .Enrich.FromLogContext();

        // Structured JSON in non-Development environments; plain text locally.
        if (context.HostingEnvironment.IsDevelopment())
            config.WriteTo.Console();
        else
            config.WriteTo.Console(new JsonFormatter());

        config.WriteTo.File("logs/bse-.txt", rollingInterval: RollingInterval.Day);
    });

    // ── Data access infrastructure ──────────────────────────────────────────
    builder.Services.AddSingleton<IDbConnectionFactory, SqlConnectionFactory>();
    builder.Services.AddScoped<IDbRepository, DapperRepository>();

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
    //   Authentication:BypassEnabled = false — SAML 2.0 via Sustainsys + Entra ID
    //
    // SAML config is read from the Saml2 section in appsettings.json /
    // appsettings.{Environment}.json or environment variables SAML2__*.
    
    var bypassAuthentication = builder.Environment.IsDevelopment()
        && builder.Configuration.GetValue<bool>("Authentication:BypassEnabled");

    builder.Services.Configure<Saml2Configuration>(
        builder.Configuration.GetSection(Saml2Configuration.SectionName));

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
                    opts.NtLogin            = builder.Configuration["Authentication:DevUserNtLogin"] ?? "dev-user";
                    opts.UseWindowsIdentity = builder.Configuration.GetValue<bool>("Authentication:UseWindowsIdentity", defaultValue: true);
                });
    }
    else
    {
        // ── SAML 2.0 / Entra ID ────────────────────────────────────────────
        var saml2Config = builder.Configuration
            .GetSection(Saml2Configuration.SectionName)
            .Get<Saml2Configuration>() ?? new Saml2Configuration();

        builder.Services
            .AddAuthentication(options =>
            {
                options.DefaultScheme          = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = Saml2Defaults.Scheme;
            })
            .AddCookie(options =>
            {
                options.Events.OnRedirectToLogin = async ctx =>

                {
                    await ctx.HttpContext.ChallengeAsync(
                        Saml2Defaults.Scheme,
                        new AuthenticationProperties
                        {
                            RedirectUri = "/Home"
                        });
                };
                // Redirect authenticated users with insufficient permissions to Home, not a 403.
                options.Events.OnRedirectToAccessDenied = ctx =>
                {
                    ctx.Response.Redirect("/Home");
                    return Task.CompletedTask;
                };
            })
            .AddSaml2(options =>
            {
                options.SPOptions.EntityId = new EntityId(saml2Config.SPEntityId);

                // Pin the public origin so the correct ACS Reply URL is sent to Entra ID
                // regardless of which port/protocol the request arrives on locally.
                if (!string.IsNullOrWhiteSpace(saml2Config.PublicOrigin))
                    options.SPOptions.PublicOrigin = new Uri(saml2Config.PublicOrigin);

                // SP signing certificate — placeholder; wire Key Vault reference before production deploy.
                // Leaving ServiceCertificates empty is acceptable for local SAML testing only.
                // Add the certificate here when the thumbprint is provisioned:
                // if (!string.IsNullOrEmpty(saml2Config.SPCertificateThumbprint))
                // {
                //     var cert = GetCertificateByThumbprint(saml2Config.SPCertificateThumbprint);
                //     options.SPOptions.ServiceCertificates.Add(cert);
                // }

                var idp = new IdentityProvider(
                    new EntityId(saml2Config.IdPEntityId),
                    options.SPOptions)
                {
                    MetadataLocation = saml2Config.IdPMetadataUrl,
                    LoadMetadata     = true,
                };

                options.IdentityProviders.Add(idp);

                // Extract the user's email from the Entra ID SAML assertion and add it
                // as the canonical 'emailaddress' claim so GroupClaimsTransformation
                // can look up the user in the database.
                //
                // Also rewrite the post-ACS Location redirect to use PublicOrigin.
                // Sustainsys builds the redirect URL from Request.Host, which behind
                // Azure Front Door can resolve to devbsewebaw1401.azurewebsites.net
                // before UseForwardedHeaders() corrects Request.Host. This guarantees
                // the 303 Location header always uses dev-bse.azure.defra.cloud.
                // Assertion payloads are never logged (GDPR / Defra SDS Logging Standards).
                options.Notifications.AcsCommandResultCreated = (result, _) =>
                {
                    // Fix post-ACS redirect hostname ----------------------------
                    if (!string.IsNullOrWhiteSpace(saml2Config.PublicOrigin)
                        && result.Location is { IsAbsoluteUri: true } location)
                    {
                        var origin = new Uri(saml2Config.PublicOrigin);
                        if (!string.Equals(location.Host, origin.Host, StringComparison.OrdinalIgnoreCase))
                        {
                            result.Location = new UriBuilder(location)
                            {
                                Scheme = origin.Scheme,
                                Host   = origin.Host,
                                Port   = origin.IsDefaultPort ? -1 : origin.Port
                            }.Uri;
                        }
                    }

                    // Extract email claim ---------------------------------------
                    if (result.Principal?.Identity is not System.Security.Claims.ClaimsIdentity identity)
                        return;

                    var email =
                        identity.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value
                        ?? identity.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress")?.Value
                        ?? identity.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

                    if (!string.IsNullOrWhiteSpace(email)
                        && !identity.HasClaim(c => c.Type == ClaimsUserContext.EmailClaimType))
                    {
                        identity.AddClaim(new System.Security.Claims.Claim(ClaimsUserContext.EmailClaimType, email));
                    }
                };
            });
    }

    // ── Forwarded headers (service registration) ────────────────────────────────
    // Registered here (before Build) so the options are available to all middleware.
    // dev-bse.azure.defra.cloud terminates TLS and forwards to the App Service's
    // default hostname. Without this, Request.Scheme/Request.Host reflect the
    // raw azurewebsites.net origin, causing absolute redirects to leak that hostname.
    // Azure's edge proxy IPs are not fixed, so KnownNetworks/KnownProxies are cleared
    // to trust forwarded headers regardless of hop address — safe because the
    // azurewebsites.net endpoint is access-restricted to Front Door / App Gateway only.
    builder.Services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor
                                 | ForwardedHeaders.XForwardedProto
                                 | ForwardedHeaders.XForwardedHost;
        options.KnownNetworks.Clear();
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
        options.Conventions.AllowAnonymousToPage("/Error");
        options.Conventions.AllowAnonymousToPage("/SessionError");
    })
     .AddMvcOptions(o =>
        // ASP.NET Core 6+ treats non-nullable string properties as implicitly [Required]
        // when nullable context is enabled. The legacy .NET Framework app had no such
        // behaviour — all search filter fields are optional. Suppress to match legacy.
        o.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true) ;

    // ── Host services ──────────────────────────────────────────────────────────
    builder.Services.AddScoped<BSE.Host.Services.ICurrentUserService, BSE.Host.Services.CurrentUserService>();
    builder.Services.AddScoped<BSE.Host.Services.IGeoLookupService, BSE.Host.Services.GeoLookupService>();

    // -- Authorisation policies
    // Each policy requires exactly its own name as a role claim.
    // GroupClaimsTransformation currently derives policy claims from a hardcoded switch on luUserGroup.Name
    // Source of truth: docs/Legacy-Page-Level-Access-Control.md (Section 5 access matrix)
    // Redirect authenticated-but-forbidden users to Home instead of returning 403.
    // Applies to both the dev-bypass path and the SAML path.
    builder.Services.AddSingleton<IAuthorizationMiddlewareResultHandler, HomeRedirectAuthorizationHandler>();

    builder.Services.AddAuthorization(options =>
    {
        // All authenticated users - search, audit log, BSESS, case/farm detail views.
        options.AddPolicy("Authenticated",
            p => p.RequireAuthenticatedUser());

        // All five recognised groups - read-only view access.
        options.AddPolicy("ReadOnly",
            p => p.RequireRole("ReadOnly"));

        // DEFRA Data Entry, DEFRA Maintenance, VLA Data Entry, VLA Maintenance.
        options.AddPolicy("DataEntry",
            p => p.RequireRole("DataEntry"));

        // DEFRA Maintenance, VLA Maintenance - ADNS Export, MoveCase, DeleteCase, RbseChange, CphhChange.
        options.AddPolicy("DEFRAMaintenance",
            p => p.RequireRole("DEFRAMaintenance"));

        // DEFRA Viewer, DEFRA Data Entry, DEFRA Maintenance - RBSE lookup on Home page.
        options.AddPolicy("DEFRAAccess",
            p => p.RequireRole("DEFRAAccess"));

        // VLA Data Entry, VLA Maintenance - OSS Export, Print Batch.
        options.AddPolicy("VLAAccess",
            p => p.RequireRole("VLAAccess"));

        // VLA Maintenance only - CaseWork, User Maintenance.
        options.AddPolicy("VLAMaintenance",
            p => p.RequireRole("VLAMaintenance"));

        // DEFRA Maintenance, VLA Data Entry, VLA Maintenance - Pick List Maintenance.
        options.AddPolicy("PickListAccess",
            p => p.RequireRole("PickListAccess"));

        // DEFRA Data Entry, DEFRA Maintenance, VLA Maintenance - Farm creation (not VLA Data Entry).
        options.AddPolicy("FarmCreation",
            p => p.RequireRole("FarmCreation"));

        // All original 5 groups - audit log and BSESS check access; excludes search-only groups (DEFRA AHO User, DEFRA AI Wales Scotland).
        options.AddPolicy("AuditAccess",
            p => p.RequireRole("DEFRAAccess", "VLAAccess"));
    });

    var app = builder.Build();

    app.UseForwardedHeaders(); // options registered via builder.Services.Configure<ForwardedHeadersOptions> above

    app.UseExceptionHandler("/Error");
    app.UseSerilogRequestLogging();
    app.UseAuthentication();
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
    app.MapGet("/", () => Results.Redirect("/Home"));
    app.MapRazorPages();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly.");
}
finally
{
    Log.CloseAndFlush();
}

// Expose Program to integration test projects using WebApplicationFactory<Program>.
public partial class Program { }
