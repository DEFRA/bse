using BSE.Host;
using BSE.Host.Authentication;
using BSE.Host.HealthChecks;
using BSE.Host.Middleware;
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
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Serilog;
using Serilog.Events;

// Bootstrap logger captures startup errors before full Serilog is configured.
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.WebHost.ConfigureKestrel(options => options.AddServerHeader = false);

    builder.Host.UseSerilog((context, _, config) => HostSetup.ConfigureSerilog(context, config));

    // ── Data access infrastructure ──────────────────────────────────────────
    builder.Services.AddSingleton<IDbConnectionFactory, SqlConnectionFactory>();
    builder.Services.AddScoped<DapperRepository>();
    builder.Services.AddScoped<IDbRepository>(sp =>
        new LoggingDbRepositoryDecorator(
            sp.GetRequiredService<DapperRepository>(),
            sp.GetRequiredService<ILogger<LoggingDbRepositoryDecorator>>(),
            sp.GetRequiredService<IConfiguration>()));

    builder.Services.AddDistributedCaching(builder.Configuration);

    builder.Services.AddSingleton<ICacheKeyProvider, CacheKeyProvider>();

    // ── Feature modules ─────────────────────────────────────────────────────
    builder.Services.AddReferenceDataModule(builder.Configuration);
    builder.Services.AddUserManagementModule(builder.Configuration);
    builder.Services.AddAuditLogModule();
    builder.Services.AddFarmManagementModule();
    builder.Services.AddSearchModule(builder.Configuration);
    builder.Services.AddBatchModule();
    builder.Services.AddCaseManagementModule();
    builder.Services.AddAnimalRelationsModule();
    builder.Services.AddCaseWorkModule();
    builder.Services.AddAdnsExportModule(builder.Configuration);
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

    var bypassAuthentication = builder.Environment.IsDevelopment()
        && builder.Configuration.GetValue<bool>("Authentication:BypassEnabled");

    // One idle timeout drives both the auth cookie and the ASP.NET session so they expire together.
    var idleTimeout = TimeSpan.FromMinutes(
        Math.Max(1, builder.Configuration.GetValue("Session:IdleTimeoutMinutes", 20)));

    var cookieSecurePolicy = builder.Environment.IsDevelopment()
        ? CookieSecurePolicy.SameAsRequest
        : CookieSecurePolicy.Always;

    builder.AddBseAuthentication(bypassAuthentication, idleTimeout, cookieSecurePolicy);
    builder.Services.AddProxyAwareness();

    // ── Razor Pages
    builder.Services.AddRazorPages(options =>
    {
        options.Conventions.AuthorizeFolder("/");
        options.Conventions.ConfigureFilter(_ => new Microsoft.AspNetCore.Mvc.TypeFilterAttribute(typeof(HandlerAuthorizationPageFilter)));
        options.Conventions.AllowAnonymousToPage(HostSetup.ErrorPath);
        options.Conventions.AllowAnonymousToPage(HostSetup.SessionErrorPath);
        options.Conventions.AllowAnonymousToPage(HostSetup.UnauthorizedPath);
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
        options.Cookie.Name = HostSetup.SessionCookieName;
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
    builder.Services.AddScoped<BSE.Host.Services.CaseEditRepositories>();
builder.Services.AddScoped<BSE.Host.Services.ICaseEditOrchestrationService, BSE.Host.Services.CaseEditOrchestrationService>();

    builder.Services.AddBseAuthorization();

    var app = builder.Build();

    app.UseMiddleware<SecurityHeadersMiddleware>(!app.Environment.IsDevelopment());

    app.UseForwardedHeaders(); // options registered via builder.Services.Configure<ForwardedHeadersOptions> above

    app.UseMiddleware<UnhandledExceptionLoggingMiddleware>();
    app.UseExceptionHandler(HostSetup.ErrorPath);

    app.UseBseRequestLogging(builder.Configuration.GetValue<int?>("Serilog:SlowRequestThresholdMs") ?? 1000);

    app.UseAuthentication();
    app.UseSession(); // Session middleware must come after Authentication

    app.UseGroupClaimGuard();
    app.UseSessionTimeoutGuard();

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

    app.MapBseEndpoints(bypassAuthentication);

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
