using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace BSE.E2E.Tests;

/// <summary>
/// Home page (/Home) regression suite — the landing menu for all authenticated users.
/// Scope is GET-only: the Batch/RBSE lookup POST handlers and the five case/farm
/// maintenance pages reachable from here (Final result entry, CPHH change, RBSE change,
/// Delete case, Move case) mutate or delete real case/farm data, so only page-render and
/// access-control checks are exercised — no form is submitted against them.
/// Current dev user is VLA Maintenance, which (via GetPoliciesForGroup) also satisfies
/// DEFRAMaintenance, so every link on the Home page is visible to this role.
/// </summary>
public sealed class HomeMenuTests(BrowserFixture fixture) : E2ETestBase(fixture)
{
    [Fact]
    public async Task Home_Renders_WithBatchAndRbsePanels()
    {
        await GotoAsync("/Home");
        await Expect(Page.Locator("#batch-panel-heading")).ToHaveTextAsync("Batch Number");
        await Expect(Page.Locator("#rbse-panel-heading")).ToHaveTextAsync("RBSE Number");
    }

    [Fact]
    public async Task Home_MenuLinks_IncludeAllExpectedItemsForVlaMaintenance()
    {
        await GotoAsync("/Home");

        var linksPanel = Page.Locator(".bse-home-links-panel");
        foreach (var href in new[]
        {
            "/Search/Farms", "/Search/Cases", "/Search/CasesByHerdmark",
            "/Search/CasesByHoldingHerdmark", "/Search/RelatedAnimals", "/Search/Outstanding",
            "/CaseWork/Menu", "/CaseWork/PrintBatch", "/Bsess/Menu",
            "/Case/FinalResultEntry", "/Farm/CphhChange", "/Case/RbseChange",
            "/Case/Delete", "/Case/MoveCase",
            "/AdnsExport/Menu", "/OssExport/Menu",
            "/Admin/Users", "/Admin/PickLists", "/AuditLog/Menu",
        })
        {
            await Expect(linksPanel.Locator($"a[href='{href}']")).ToBeVisibleAsync();
        }
    }

    [Theory]
    [InlineData("/Case/FinalResultEntry", "Final Result Entry")]
    [InlineData("/Farm/CphhChange", "CPHH Change")]
    [InlineData("/Case/RbseChange", "Change RBSE")]
    [InlineData("/Case/Delete", "Delete Case")]
    [InlineData("/Case/MoveCase", "Move Case")]
    public async Task MaintenancePage_Renders_ForAuthorisedRole(string path, string expectedHeading)
    {
        await GotoAsync(path);
        await Expect(Page.Locator("h1")).ToHaveTextAsync(expectedHeading);
    }
}
