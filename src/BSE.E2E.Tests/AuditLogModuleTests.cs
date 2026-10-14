using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace BSE.E2E.Tests;

/// <summary>
/// Audit log menu (/AuditLog/Menu) regression suite. ByCase and ByFarm are intentionally
/// excluded from the menu (legacy parity — reachable only via the Case/Farm tabs with an
/// RBSE/CPHH in context) so they are not navigable from here.
/// </summary>
public sealed class AuditLogModuleTests(BrowserFixture fixture) : E2ETestBase(fixture)
{
    [Fact]
    public async Task Menu_Renders_WithSixLinks()
    {
        await GotoAsync("/AuditLog/Menu");
        foreach (var href in new[]
        {
            "/AuditLog/ByDate", "/AuditLog/NewFarms", "/AuditLog/CphhChanges",
            "/AuditLog/RbseChanges", "/AuditLog/CaseMoves", "/AuditLog/ByUser",
        })
        {
            await Expect(Page.Locator($"a[href='{href}']")).ToBeVisibleAsync();
        }
    }

    [Fact]
    public async Task ByDate_MissingDate_ShowsValidationMessage()
    {
        await GotoAsync("/AuditLog/ByDate?LogDate=");
        await Expect(Page.Locator(".govuk-error-summary")).ToBeVisibleAsync();
    }

    [Fact]
    public async Task ByDate_ValidDate_RendersWithoutError()
    {
        await GotoAsync("/AuditLog/ByDate?LogDate=01/01/2020");
        await Expect(Page.Locator(".govuk-error-summary")).Not.ToBeVisibleAsync();
    }

    [Fact]
    public async Task ByUser_MissingUser_ShowsValidationMessage()
    {
        await GotoAsync("/AuditLog/ByUser?UserId=0");
        await Expect(Page.Locator("body")).ToContainTextAsync("Select a user.");
    }

    [Theory]
    [InlineData("/AuditLog/NewFarms")]
    [InlineData("/AuditLog/CphhChanges")]
    [InlineData("/AuditLog/RbseChanges")]
    [InlineData("/AuditLog/CaseMoves")]
    public async Task Page_Renders(string path)
    {
        await GotoAsync(path);
        await Expect(Page.Locator("h1")).ToBeVisibleAsync();
    }
}
