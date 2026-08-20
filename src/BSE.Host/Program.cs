using BSE.Host.Cache;
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
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
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

    // ── Authentication — OIDC stub ──────────────────────────────────────────
    // Authority, ClientId and ClientSecret are read from configuration.
    // Set via appsettings.json, appsettings.{Environment}.json, or environment
    // variables OIDC__Authority / OIDC__ClientId / OIDC__ClientSecret.
    // Full Azure AD wiring is completed when the real tenant is provisioned.
    DevAuth.Register(builder);

    builder.Services
        .AddAuthentication(options =>
        {
            options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = "oidc";
        })
        .AddCookie()
        .AddOpenIdConnect("oidc", options =>
        {
            options.Authority = builder.Configuration["OIDC:Authority"];
            options.ClientId = builder.Configuration["OIDC:ClientId"];
            options.ClientSecret = builder.Configuration["OIDC:ClientSecret"];
            options.ResponseType = "code";
            options.SaveTokens = true;
            options.GetClaimsFromUserInfoEndpoint = true;
            options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
            options.Events.OnRemoteFailure = ctx =>
            {
                ctx.Response.Redirect("/Error?message=Sign-in+failed.+Please+try+again.");
                ctx.HandleResponse();
                return Task.CompletedTask;
            };
        });

    // ── Razor Pages ────────────────────────────────────────────────────────────
    builder.Services.AddRazorPages(options =>
    {
        options.Conventions.AuthorizeFolder("/");
        options.Conventions.AllowAnonymousToPage("/Error");
    })
     .AddMvcOptions(o =>
        // ASP.NET Core 6+ treats non-nullable string properties as implicitly [Required]
        // when nullable context is enabled. The legacy .NET Framework app had no such
        // behaviour — all search filter fields are optional. Suppress to match legacy.
        o.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true) ;

    // ── Host services ──────────────────────────────────────────────────────────
    builder.Services.AddScoped<BSE.Host.Services.ICurrentUserService, BSE.Host.Services.CurrentUserService>();

    // -- Authorisation policies
    // Each policy requires exactly its own name as a role claim.
    // GroupClaimsTransformation calls GetGroupPolicies SP (reading luGroupPolicy) and emits
    // one ClaimTypes.Role per row - so changing luGroupPolicy rows changes access with no code deploy.
    // Source of truth: docs/Legacy-Page-Level-Access-Control.md (Section 5 access matrix)
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
    });

    var app = builder.Build();

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

// Partial void stub — silently dropped by the compiler when Authentication/DevAuth.cs is absent.
internal static partial class DevAuth
{
    static partial void DoRegister(WebApplicationBuilder builder);
    internal static void Register(WebApplicationBuilder builder) => DoRegister(builder);
}
