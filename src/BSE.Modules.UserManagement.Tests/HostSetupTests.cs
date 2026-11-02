using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using BSE.Host;
using BSE.Host.Authentication;
using BSE.Host.Services;
using BSE.Modules.UserManagement.Identity;
using BSE.Modules.UserManagement.Tests.TestAuth;
using FluentAssertions;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using NSubstitute;
using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace BSE.Modules.UserManagement.Tests;

[Collection(Integration.ProgramHostCollection.Name)]
public sealed class HostSetupTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private sealed class CollectingSink : ILogEventSink
    {
        public List<LogEvent> Events { get; } = [];
        public void Emit(LogEvent logEvent) => Events.Add(logEvent);
    }

    private sealed class StubChallengeHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
        : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync() => Task.FromResult(AuthenticateResult.NoResult());

        protected override Task HandleChallengeAsync(AuthenticationProperties properties)
        {
            Response.Redirect("/stub-idp?return=" + Uri.EscapeDataString(properties.RedirectUri ?? ""));
            return Task.CompletedTask;
        }
    }

    private sealed class TestApp(WebApplication app) : IAsyncDisposable
    {
        public WebApplication App { get; } = app;
        public HttpClient Client { get; } = app.GetTestClient();

        public async ValueTask DisposeAsync()
        {
            Client.Dispose();
            await App.DisposeAsync();
        }
    }

    private static async Task<TestApp> StartAsync(
        Action<WebApplicationBuilder>? services, Action<WebApplication> pipeline, params Claim[] claims)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Logging.ClearProviders();
        services?.Invoke(builder);

        var app = builder.Build();
        app.Use(async (ctx, next) =>
        {
            if (claims.Length > 0)
                ctx.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "t"));
            await next();
        });
        pipeline(app);
        await app.StartAsync(Ct);
        return new TestApp(app);
    }

    private static HttpRequestMessage Html(string path)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Accept.ParseAdd("text/html");
        return request;
    }

    // ── Serilog ──────────────────────────────────────────────────────────

    [Theory]
    [InlineData("Development", "")]
    [InlineData("Production", "")]
    [InlineData("Staging", "")]
    [InlineData("Development", "InstrumentationKey=00000000-0000-0000-0000-000000000000")]
    [InlineData("Production", "InstrumentationKey=00000000-0000-0000-0000-000000000000")]
    public void ConfigureSerilog_HandlesEveryEnvironmentAndAppInsightsCombination(string environment, string connectionString)
    {
        var env = Substitute.For<IHostEnvironment>();
        env.EnvironmentName.Returns(environment);
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["ApplicationInsights:ConnectionString"] = connectionString })
            .Build();
        var context = new HostBuilderContext(new Dictionary<object, object>()) { Configuration = configuration, HostingEnvironment = env };
        var loggerConfiguration = new LoggerConfiguration();

        var act = () => HostSetup.ConfigureSerilog(context, loggerConfiguration);

        act.Should().NotThrow();
    }

    // ── Distributed cache ────────────────────────────────────────────────

    [Fact]
    public void AddDistributedCaching_WithoutRedis_UsesMemoryCache()
    {
        var services = new ServiceCollection().AddLogging();
        HostSetup.AddDistributedCaching(services, new ConfigurationBuilder().Build());

        using var provider = services.BuildServiceProvider();

        provider.GetRequiredService<IDistributedCache>().GetType().Name.Should().Contain("Memory");
    }

    [Fact]
    public void AddDistributedCaching_WithRedis_UsesResilientCache()
    {
        var services = new ServiceCollection().AddLogging();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Redis:ConnectionString"] = "localhost:6379,abortConnect=false" })
            .Build();
        HostSetup.AddDistributedCaching(services, configuration);

        using var provider = services.BuildServiceProvider();

        provider.GetRequiredService<IDistributedCache>().GetType().Name.Should().Be("ResilientDistributedCache");
    }

    // ── Authorisation ────────────────────────────────────────────────────

    [Theory]
    [InlineData("AuditAccess", "VLAAccess", true)]
    [InlineData("AuditAccess", "DEFRAAccess", true)]
    [InlineData("AuditAccess", "ReadOnly", false)]
    [InlineData("DEFRAOrVLAMaintenance", "VLAMaintenance", true)]
    [InlineData("DEFRAOrVLAMaintenance", "DEFRAMaintenance", true)]
    [InlineData("DEFRAOrVLAMaintenance", "DataEntry", false)]
    [InlineData("DataEntry", "DataEntry", true)]
    [InlineData("ReadOnly", "ReadOnly", true)]
    [InlineData("DEFRAMaintenance", "DEFRAMaintenance", true)]
    [InlineData("DEFRAAccess", "DEFRAAccess", true)]
    [InlineData("VLAAccess", "VLAAccess", true)]
    [InlineData("VLAMaintenance", "VLAMaintenance", true)]
    [InlineData("PickListAccess", "PickListAccess", true)]
    [InlineData("FarmCreation", "FarmCreation", true)]
    [InlineData("FarmCreation", "DataEntry", false)]
    public async Task AddBseAuthorization_PoliciesRequireTheirOwnRoles(string policy, string role, bool expected)
    {
        var services = new ServiceCollection().AddLogging();
        services.AddBseAuthorization();
        await using var provider = services.BuildServiceProvider();
        var authorization = provider.GetRequiredService<IAuthorizationService>();
        var user = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Role, role)], "t"));

        var result = await authorization.AuthorizeAsync(user, policy);

        result.Succeeded.Should().Be(expected);
    }

    [Fact]
    public async Task AddBseAuthorization_AuthenticatedPolicyAndHandlerRegistered()
    {
        var services = new ServiceCollection().AddLogging();
        services.AddBseAuthorization();
        await using var provider = services.BuildServiceProvider();
        var authorization = provider.GetRequiredService<IAuthorizationService>();

        (await authorization.AuthorizeAsync(new ClaimsPrincipal(new ClaimsIdentity("t")), "Authenticated")).Succeeded.Should().BeTrue();
        (await authorization.AuthorizeAsync(new ClaimsPrincipal(new ClaimsIdentity()), "Authenticated")).Succeeded.Should().BeFalse();
        provider.GetRequiredService<IAuthorizationMiddlewareResultHandler>().Should().BeOfType<HomeRedirectAuthorizationHandler>();
    }

    // ── Proxy awareness ──────────────────────────────────────────────────

    [Fact]
    public async Task AddProxyAwareness_TrustsForwardedHeadersAndRewritesRedirectsRelative()
    {
        var services = new ServiceCollection().AddLogging();
        services.AddAuthentication().AddCookie();
        services.AddProxyAwareness();
        await using var provider = services.BuildServiceProvider();

        var forwarded = provider.GetRequiredService<IOptions<ForwardedHeadersOptions>>().Value;
        forwarded.ForwardedHeaders.Should().HaveFlag(ForwardedHeaders.XForwardedHost);
        forwarded.KnownProxies.Should().BeEmpty();

        var cookieOptions = provider.GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>().Get(CookieAuthenticationDefaults.AuthenticationScheme);
        var scheme = new AuthenticationScheme(CookieAuthenticationDefaults.AuthenticationScheme, null, typeof(CookieAuthenticationHandler));

        var loginHttp = new DefaultHttpContext();
        await cookieOptions.Events.OnRedirectToLogin(new RedirectContext<CookieAuthenticationOptions>(
            loginHttp, scheme, cookieOptions, new AuthenticationProperties(), "https://origin.azurewebsites.net/Login?ReturnUrl=%2Fx"));
        loginHttp.Response.Headers.Location.ToString().Should().Be("/Login?ReturnUrl=%2Fx");

        var deniedHttp = new DefaultHttpContext();
        await cookieOptions.Events.OnRedirectToAccessDenied(new RedirectContext<CookieAuthenticationOptions>(
            deniedHttp, scheme, cookieOptions, new AuthenticationProperties(), "/Denied"));
        deniedHttp.Response.Headers.Location.ToString().Should().Be("/Denied");
    }

    // ── Authentication registration ──────────────────────────────────────

    private static WebApplication BuildAuthApp(bool bypass, Dictionary<string, string?> configuration)
    {
        var builder = WebApplication.CreateBuilder();
        builder.Logging.ClearProviders();
        builder.Configuration.AddInMemoryCollection(configuration);
        builder.Services.AddDistributedMemoryCache();
        builder.AddBseAuthentication(bypass, TimeSpan.FromMinutes(5), CookieSecurePolicy.Always);
        return builder.Build();
    }

    [Fact]
    public async Task AddBseAuthentication_Bypass_RegistersDevelopmentScheme()
    {
        var keys = Path.Combine(Path.GetTempPath(), "bse-keys-" + Guid.NewGuid().ToString("N"));
        await using var app = BuildAuthApp(true, new()
        {
            ["Authentication:DevUserEmail"] = "dev@example.test",
            ["Authentication:UseWindowsIdentity"] = "false",
            ["DataProtection:KeysPath"] = keys
        });

        var schemes = app.Services.GetRequiredService<IAuthenticationSchemeProvider>();
        (await schemes.GetDefaultAuthenticateSchemeAsync())!.Name.Should().Be(DevelopmentAuthHandler.SchemeName);
        app.Services.GetRequiredService<IOptionsMonitor<DevelopmentAuthOptions>>()
            .Get(DevelopmentAuthHandler.SchemeName).Email.Should().Be("dev@example.test");
        app.Services.GetRequiredService<ISignInSessionRegistry>().Should().NotBeNull();
        app.Services.GetRequiredService<IOptions<AntiforgeryOptions>>().Value.HeaderName.Should().Be("RequestVerificationToken");
    }

    [Fact]
    public async Task AddBseAuthentication_Bypass_DefaultsWhenNothingConfigured()
    {
        await using var app = BuildAuthApp(true, new());

        app.Services.GetRequiredService<IOptionsMonitor<DevelopmentAuthOptions>>()
            .Get(DevelopmentAuthHandler.SchemeName).Email.Should().Be("dev-user@defra.gov.uk");
    }

    private static Dictionary<string, string?> EntraConfig(bool forceAuthn, string? publicOrigin = "https://public.example.test") => new()
    {
        ["AzureAd:TenantId"] = "11111111-1111-1111-1111-111111111111",
        ["AzureAd:ClientId"] = "22222222-2222-2222-2222-222222222222",
        ["AzureAd:ClientSecret"] = "not-a-secret",
        ["AzureAd:ForceAuthn"] = forceAuthn.ToString(),
        ["AzureAd:PublicOrigin"] = publicOrigin
    };

    [Fact]
    public async Task AddBseAuthentication_Entra_ConfiguresOidcFromAzureAdSection()
    {
        await using var app = BuildAuthApp(false, EntraConfig(true));

        var oidc = app.Services.GetRequiredService<IOptionsMonitor<OpenIdConnectOptions>>().Get(OpenIdConnectDefaults.AuthenticationScheme);

        oidc.ClientId.Should().Be("22222222-2222-2222-2222-222222222222");
        oidc.CallbackPath.ToString().Should().Be("/signin-oidc");
        var schemes = app.Services.GetRequiredService<IAuthenticationSchemeProvider>();
        (await schemes.GetDefaultChallengeSchemeAsync())!.Name.Should().Be(CookieAuthenticationDefaults.AuthenticationScheme);
    }

    [Theory]
    [InlineData(true, "https://public.example.test", "login", "public.example.test")]
    [InlineData(false, null, null, "internal.azurewebsites.net")]
    public async Task AddBseAuthentication_Entra_RedirectToIdentityProviderRewritesHostAndForcesLogin(
        bool forceAuthn, string? publicOrigin, string? expectedPrompt, string expectedHost)
    {
        await using var app = BuildAuthApp(false, EntraConfig(forceAuthn, publicOrigin));
        var oidc = app.Services.GetRequiredService<IOptionsMonitor<OpenIdConnectOptions>>().Get(OpenIdConnectDefaults.AuthenticationScheme);
        var http = new DefaultHttpContext { RequestServices = app.Services };
        var scheme = new AuthenticationScheme(OpenIdConnectDefaults.AuthenticationScheme, null, typeof(OpenIdConnectHandler));
        var context = new RedirectContext(http, scheme, oidc, new AuthenticationProperties())
        {
            ProtocolMessage = new OpenIdConnectMessage { RedirectUri = "https://internal.azurewebsites.net/signin-oidc" }
        };

        await oidc.Events.OnRedirectToIdentityProvider(context);

        context.ProtocolMessage.Prompt.Should().Be(expectedPrompt);
        new Uri(context.ProtocolMessage.RedirectUri).Host.Should().Be(expectedHost);
    }

    [Theory]
    [InlineData("preferred_username", "user@example.test", true)]
    [InlineData("other", "x", false)]
    public async Task AddBseAuthentication_Entra_TokenValidatedAddsEmailClaim(string claimType, string value, bool expectsEmail)
    {
        await using var app = BuildAuthApp(false, EntraConfig(false));
        var oidc = app.Services.GetRequiredService<IOptionsMonitor<OpenIdConnectOptions>>().Get(OpenIdConnectDefaults.AuthenticationScheme);
        var identity = new ClaimsIdentity([new Claim(claimType, value)], "t");
        var scheme = new AuthenticationScheme(OpenIdConnectDefaults.AuthenticationScheme, null, typeof(OpenIdConnectHandler));
        var context = new TokenValidatedContext(
            new DefaultHttpContext { RequestServices = app.Services }, scheme, oidc, new ClaimsPrincipal(identity), new AuthenticationProperties());

        await oidc.Events.OnTokenValidated(context);

        identity.FindFirst(ClaimsUserContext.EmailClaimType)?.Value.Should().Be(expectsEmail ? value : null);
    }

    [Fact]
    public async Task AddBseAuthentication_Entra_CookieEventsRedirectToTimeoutAndUnauthorizedPages()
    {
        await using var app = BuildAuthApp(false, EntraConfig(false));
        var cookies = app.Services.GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>().Get(CookieAuthenticationDefaults.AuthenticationScheme);
        var scheme = new AuthenticationScheme(CookieAuthenticationDefaults.AuthenticationScheme, null, typeof(CookieAuthenticationHandler));

        var stale = new DefaultHttpContext { RequestServices = app.Services };
        stale.Request.Headers.Cookie = $"{HostSetup.AuthCookieName}=expired";
        stale.Request.Headers.Accept = "text/html";
        await cookies.Events.OnRedirectToLogin(new RedirectContext<CookieAuthenticationOptions>(
            stale, scheme, cookies, new AuthenticationProperties(), "/login"));
        stale.Response.Headers.Location.ToString().Should().Be(HostSetup.SessionErrorPath);

        var denied = new DefaultHttpContext { RequestServices = app.Services };
        denied.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Name, "someone")], "t", ClaimTypes.Name, ClaimTypes.Role));
        await cookies.Events.OnRedirectToAccessDenied(new RedirectContext<CookieAuthenticationOptions>(
            denied, scheme, cookies, new AuthenticationProperties(), "/denied"));
        denied.Response.Headers.Location.ToString().Should().Be(HostSetup.UnauthorizedPath);

        var anonymousDenied = new DefaultHttpContext { RequestServices = app.Services };
        await cookies.Events.OnRedirectToAccessDenied(new RedirectContext<CookieAuthenticationOptions>(
            anonymousDenied, scheme, cookies, new AuthenticationProperties(), "/denied"));
        anonymousDenied.Response.Headers.Location.ToString().Should().Be(HostSetup.UnauthorizedPath);
    }

    // ── Middleware ───────────────────────────────────────────────────────

    [Theory]
    [InlineData("/x", true, false, HttpStatusCode302)]
    [InlineData("/x", true, true, HttpStatusCode200)]
    [InlineData("/x", false, false, HttpStatusCode200)]
    [InlineData("/Error", true, false, HttpStatusCode404)]
    [InlineData("/health", true, false, HttpStatusCode404)]
    [InlineData("/app.css", true, false, HttpStatusCode404)]
    public async Task UseGroupClaimGuard_RedirectsAuthenticatedUsersWithoutGroup(string path, bool authenticated, bool hasGroup, int expectedStatus)
    {
        var claims = new List<Claim>();
        if (authenticated) claims.Add(new Claim(ClaimTypes.Name, "u"));
        if (hasGroup) claims.Add(new Claim(ClaimsUserContext.BseGroupIdClaimType, "1"));
        await using var host = await StartAsync(null, app =>
        {
            app.UseGroupClaimGuard();
            app.MapGet("/x", () => "ok");
        }, [.. claims]);

        var response = await host.Client.GetAsync(path, Ct);

        ((int)response.StatusCode).Should().Be(expectedStatus);
        if (expectedStatus == HttpStatusCode302)
            response.Headers.Location!.ToString().Should().Be(HostSetup.UnauthorizedPath);
    }

    private const int HttpStatusCode200 = 200;
    private const int HttpStatusCode302 = 302;
    private const int HttpStatusCode404 = 404;

    private static Task<TestApp> StartSessionAppAsync(params Claim[] claims) =>
        StartAsync(
            b => { b.Services.AddDistributedMemoryCache(); b.Services.AddSession(o => o.Cookie.Name = HostSetup.SessionCookieName); },
            app =>
            {
                app.UseSession();
                app.UseSessionTimeoutGuard();
                app.MapGet("/x", () => "ok");
            },
            claims);

    [Fact]
    public async Task UseSessionTimeoutGuard_StaleSessionCookie_RedirectsToSessionError()
    {
        await using var host = await StartSessionAppAsync(new Claim(ClaimTypes.Name, "u"));
        using var request = Html("/x");
        request.Headers.Add("Cookie", $"{HostSetup.SessionCookieName}=stale");

        var response = await host.Client.SendAsync(request, Ct);

        ((int)response.StatusCode).Should().Be(302);
        response.Headers.Location!.ToString().Should().Be(HostSetup.SessionErrorPath);
    }

    [Fact]
    public async Task UseSessionTimeoutGuard_FreshVisitMarksSessionAndSecondVisitPasses()
    {
        await using var host = await StartSessionAppAsync(new Claim(ClaimTypes.Name, "u"));

        using var first = await host.Client.SendAsync(Html("/x"), Ct);
        ((int)first.StatusCode).Should().Be(200);

        var sessionCookie = first.Headers.GetValues("Set-Cookie").First(c => c.StartsWith(HostSetup.SessionCookieName)).Split(';')[0];
        using var secondRequest = Html("/x");
        secondRequest.Headers.Add("Cookie", sessionCookie);
        using var second = await host.Client.SendAsync(secondRequest, Ct);

        ((int)second.StatusCode).Should().Be(200);
    }

    [Theory]
    [InlineData("/x", "application/json")]
    [InlineData("/app.css", "text/html")]
    [InlineData("/Error", "text/html")]
    public async Task UseSessionTimeoutGuard_IgnoresNonHtmlStaticAndExcludedRequests(string path, string accept)
    {
        await using var host = await StartSessionAppAsync(new Claim(ClaimTypes.Name, "u"));
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Accept.ParseAdd(accept);
        request.Headers.Add("Cookie", $"{HostSetup.SessionCookieName}=stale");

        var response = await host.Client.SendAsync(request, Ct);

        response.Headers.Location.Should().BeNull();
    }

    [Fact]
    public async Task UseSessionTimeoutGuard_AnonymousUsersAreNotRedirected()
    {
        await using var host = await StartSessionAppAsync();
        using var request = Html("/x");
        request.Headers.Add("Cookie", $"{HostSetup.SessionCookieName}=stale");

        var response = await host.Client.SendAsync(request, Ct);

        ((int)response.StatusCode).Should().Be(200);
    }

    [Theory]
    [InlineData(100000, "/ok", LogEventLevel.Information)]
    [InlineData(0, "/ok", LogEventLevel.Warning)]
    [InlineData(100000, "/err", LogEventLevel.Error)]
    public async Task UseBseRequestLogging_ChoosesLevelAndEnrichesTheEvent(int thresholdMs, string path, LogEventLevel expected)
    {
        var sink = new CollectingSink();
        var logger = new LoggerConfiguration().WriteTo.Sink(sink).CreateLogger();
        var previous = Log.Logger;
        Log.Logger = logger;
        try
        {
            await using var host = await StartAsync(
                b => b.Host.UseSerilog(logger),
                app =>
                {
                    app.UseBseRequestLogging(thresholdMs);
                    app.MapGet("/ok", () => "ok");
                    app.MapGet("/err", () => Results.StatusCode(500));
                },
                new Claim(ClaimTypes.Name, "tester"));
            using var request = new HttpRequestMessage(HttpMethod.Get, path);
            request.Headers.Add("User-Agent", "unit-test");

            await host.Client.SendAsync(request, Ct);
        }
        finally
        {
            Log.Logger = previous;
        }

        var logged = sink.Events.Single(e => e.Properties.ContainsKey("Elapsed"));
        logged.Level.Should().Be(expected);
        logged.Properties["UserAgent"].ToString().Should().Contain("unit-test");
        logged.Properties["User"].ToString().Should().Contain("t");
        logged.Properties.Should().ContainKeys("RequestHost", "RequestScheme");
    }

    // ── Endpoints ────────────────────────────────────────────────────────

    private readonly ICaseEditSessionStateService _sessionState = Substitute.For<ICaseEditSessionStateService>();
    private readonly ICaseDraftDiscardService _discard = Substitute.For<ICaseDraftDiscardService>();

    private Task<TestApp> StartEndpointsAsync(bool bypass) =>
        StartAsync(
            b =>
            {
                b.Services.AddAuthentication(TestAuthHandler.SchemeName)
                    .AddScheme<TestAuthOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { })
                    .AddScheme<AuthenticationSchemeOptions, StubChallengeHandler>(OpenIdConnectDefaults.AuthenticationScheme, _ => { });
                b.Services.AddAuthorization();
                b.Services.AddAntiforgery(o => o.HeaderName = "RequestVerificationToken");
                b.Services.AddSingleton(_sessionState);
                b.Services.AddSingleton(_discard);
            },
            app =>
            {
                app.UseAuthentication();
                app.UseAuthorization();
                app.MapGet("/token", (HttpContext c, IAntiforgery a) => Results.Json(new { token = a.GetAndStoreTokens(c).RequestToken }));
                app.MapBseEndpoints(bypass);
            });

    [Fact]
    public async Task RootRedirectsToHome()
    {
        await using var host = await StartEndpointsAsync(true);

        var response = await host.Client.GetAsync("/", Ct);

        response.Headers.Location!.ToString().Should().Be(HostSetup.HomePath);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, true)]
    public async Task UnsavedStatus_ReportsPendingChanges(bool hasState, bool expected)
    {
        _sessionState.GetAsync("002600001", Arg.Any<CancellationToken>())
            .Returns(hasState ? new CaseEditSessionState { Rbse = "002600001", HasFeedsChanges = true } : null);
        await using var host = await StartEndpointsAsync(true);

        var json = await host.Client.GetStringAsync("/case/002600001/unsaved-status", Ct);

        JsonDocument.Parse(json).RootElement.GetProperty("hasUnsavedChanges").GetBoolean().Should().Be(expected);
    }

    [Fact]
    public async Task UnsavedStatus_StateWithNoFlags_IsNotUnsaved()
    {
        _sessionState.GetAsync("002600001", Arg.Any<CancellationToken>()).Returns(new CaseEditSessionState { Rbse = "002600001" });
        await using var host = await StartEndpointsAsync(true);

        var json = await host.Client.GetStringAsync("/case/002600001/unsaved-status", Ct);

        JsonDocument.Parse(json).RootElement.GetProperty("hasUnsavedChanges").GetBoolean().Should().BeFalse();
    }

    [Fact]
    public async Task DiscardUnsaved_WithoutAntiforgeryToken_IsRejected()
    {
        await using var host = await StartEndpointsAsync(true);

        var response = await host.Client.PostAsync("/case/002600001/discard-unsaved", null, Ct);

        ((int)response.StatusCode).Should().Be(400);
        await _discard.DidNotReceiveWithAnyArgs().DiscardAllAsync(default!);
    }

    [Fact]
    public async Task DiscardUnsaved_WithValidToken_ClearsDraft()
    {
        await using var host = await StartEndpointsAsync(true);
        using var tokenResponse = await host.Client.GetAsync("/token", Ct);
        var token = JsonDocument.Parse(await tokenResponse.Content.ReadAsStringAsync(Ct)).RootElement.GetProperty("token").GetString()!;
        var cookies = string.Join("; ", tokenResponse.Headers.GetValues("Set-Cookie").Select(c => c.Split(';')[0]));
        using var request = new HttpRequestMessage(HttpMethod.Post, "/case/002600001/discard-unsaved");
        request.Headers.Add("RequestVerificationToken", token);
        request.Headers.Add("Cookie", cookies);

        var response = await host.Client.SendAsync(request, Ct);

        ((int)response.StatusCode).Should().Be(200);
        await _discard.Received(1).DiscardAllAsync("002600001");
    }

    [Fact]
    public async Task SignIn_Bypass_ClearsCookiesAndGoesHome()
    {
        await using var host = await StartEndpointsAsync(true);

        var response = await host.Client.GetAsync("/signin", Ct);

        response.Headers.Location!.ToString().Should().Be(HostSetup.HomePath);
        var setCookies = string.Join("|", response.Headers.GetValues("Set-Cookie"));
        setCookies.Should().Contain(HostSetup.AuthCookieName).And.Contain(HostSetup.SessionCookieName);
    }

    [Fact]
    public async Task SignIn_WithoutBypass_ChallengesOpenIdConnect()
    {
        await using var host = await StartEndpointsAsync(false);

        var response = await host.Client.GetAsync("/signin", Ct);

        response.Headers.Location!.ToString().Should().StartWith("/stub-idp");
    }
}
