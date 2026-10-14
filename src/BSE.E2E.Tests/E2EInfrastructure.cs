using Microsoft.Playwright;

namespace BSE.E2E.Tests;

/// <summary>
/// Shared configuration for the E2E suite.
/// The app must already be running at <see cref="BaseUrl"/> with
/// Authentication:BypassEnabled=true and a DevUserEmail that exists in [User].
/// </summary>
public static class E2EConfig
{
    public static string BaseUrl =>
        Environment.GetEnvironmentVariable("BSE_E2E_BASEURL") ?? "http://localhost:5199";

    /// <summary>
    /// Playwright's own browser download is blocked by TLS interception on this network,
    /// so the suite drives the locally installed Edge instead.
    /// </summary>
    public const string BrowserChannel = "msedge";

    // Real records seeded in the local BSE database.
    public const string Rbse = "990100001";
    public const string SecondRbse = "990100002";
    public const string Cphh = "12345678901";
}

/// <summary>Launches one browser for the whole run; each test gets an isolated context.</summary>
public sealed class BrowserFixture : IAsyncLifetime
{
    private IPlaywright? _playwright;
    public IBrowser Browser { get; private set; } = default!;
    public IAPIRequestContext Api { get; private set; } = default!;

    public async Task InitializeAsync()
    {
        _playwright = await Playwright.CreateAsync();
        Browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Channel = E2EConfig.BrowserChannel,
            Headless = Environment.GetEnvironmentVariable("BSE_E2E_HEADED") != "true",
            SlowMo = Environment.GetEnvironmentVariable("BSE_E2E_HEADED") == "true" ? 250 : 0
        });
        Api = await _playwright.APIRequest.NewContextAsync(new APIRequestNewContextOptions
        {
            BaseURL = E2EConfig.BaseUrl,
            IgnoreHTTPSErrors = true
        });
    }

    public async Task DisposeAsync()
    {
        if (Api is not null) await Api.DisposeAsync();
        if (Browser is not null) await Browser.CloseAsync();
        _playwright?.Dispose();
    }
}

[CollectionDefinition(Name)]
public sealed class BrowserCollection : ICollectionFixture<BrowserFixture>
{
    public const string Name = "browser";
}

/// <summary>
/// Base class providing a fresh page per test plus assertion helpers that
/// distinguish "the page worked" from "the server blew up".
/// </summary>
[Collection(BrowserCollection.Name)]
public abstract class E2ETestBase : IAsyncLifetime
{
    private readonly BrowserFixture _fixture;
    private IBrowserContext _context = default!;
    protected IPage Page { get; private set; } = default!;
    protected IAPIRequestContext Api => _fixture.Api;

    protected E2ETestBase(BrowserFixture fixture) => _fixture = fixture;

    public async Task InitializeAsync()
    {
        _context = await _fixture.Browser.NewContextAsync(new BrowserNewContextOptions
        {
            IgnoreHTTPSErrors = true,
            ViewportSize = new ViewportSize { Width = 1440, Height = 900 }
        });
        Page = await _context.NewPageAsync();
    }

    public async Task DisposeAsync() => await _context.DisposeAsync();

    protected async Task<IResponse?> GotoAsync(string path) =>
        await Page.GotoAsync($"{E2EConfig.BaseUrl}{path}", new PageGotoOptions
        {
            WaitUntil = WaitUntilState.DOMContentLoaded,
            Timeout = 30_000
        });

    /// <summary>
    /// Asserts a page is reachable and does not fault. Status is taken from an API request
    /// because Chromium aborts navigation to a bodyless 4xx before any assertion could run.
    /// A 404 is treated as healthy — this proves the server handled the request gracefully.
    /// </summary>
    protected async Task<int> AssertPageHealthyAsync(string path)
    {
        var api = await Api.GetAsync(path);
        var status = api.Status;

        Assert.True(status < 500, $"{path}: server error HTTP {status}");

        if (status == 200)
        {
            await GotoAsync(path);
            var body = await Page.ContentAsync();
            Assert.False(body.Contains("An unhandled exception occurred", StringComparison.OrdinalIgnoreCase),
                $"{path}: developer exception page rendered");
            Assert.False(body.Contains("<title>Internal Server Error", StringComparison.OrdinalIgnoreCase),
                $"{path}: error page rendered");
        }

        return status;
    }

    /// <summary>Asserts the page is reachable AND actually served content (HTTP 200).</summary>
    protected async Task AssertPageRendersAsync(string path)
    {
        var status = await AssertPageHealthyAsync(path);
        Assert.True(status == 200, $"{path}: expected HTTP 200 but got {status}");
    }
}
