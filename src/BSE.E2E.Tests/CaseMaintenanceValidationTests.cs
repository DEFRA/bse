using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace BSE.E2E.Tests;

/// <summary>
/// Validation-level regression coverage for the five DEFRAMaintenance case/farm pages reachable
/// from the Home "Casework and reports" menu: Final Result Entry, RBSE Change, CPHH Change,
/// Delete Case, Move Case.
///
/// Scope boundary: every scenario here only ever submits the "Look Up" (and MoveCase's "Check")
/// handler, which performs a read-only lookup and returns before any Execute/Delete/Update call.
/// The actual commit handlers (Save/Change/Delete/Move) are never invoked — see
/// <see cref="HomeMenuTests"/> for the GET-only smoke coverage of these same pages.
/// </summary>
public sealed class CaseMaintenanceValidationTests(BrowserFixture fixture) : E2ETestBase(fixture)
{
    private const string UnknownRbse = "999999999";
    private const string UnknownCphh = "99999999901";

    [Fact]
    public async Task FinalResultEntry_BlankRbse_ShowsRequiredMessage()
    {
        await GotoAsync("/Case/FinalResultEntry");
        await Page.Locator("#rbse").FillAsync("");
        await Page.GetByRole(AriaRole.Button, new() { Name = "Look Up" }).ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);

        await Expect(Page.Locator(".govuk-error-summary")).ToContainTextAsync("Please enter an RBSE");
    }

    [Fact]
    public async Task FinalResultEntry_UnknownRbse_ShowsNotFoundMessage()
    {
        await GotoAsync("/Case/FinalResultEntry");
        await Page.Locator("#rbse").FillAsync(UnknownRbse);
        await Page.GetByRole(AriaRole.Button, new() { Name = "Look Up" }).ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);

        await Expect(Page.Locator(".govuk-error-summary")).ToContainTextAsync("This RBSE could not be found in the database");
    }

    [Fact]
    public async Task RbseChange_UnknownOldRbse_ShowsNotFoundMessage()
    {
        await GotoAsync("/Case/RbseChange");
        await Page.Locator("#old-rbse").FillAsync(UnknownRbse);
        await Page.GetByRole(AriaRole.Button, new() { Name = "Look Up" }).ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);

        await Expect(Page.Locator(".govuk-error-summary")).ToContainTextAsync("A case with this RBSE was not found");
    }

    [Fact]
    public async Task CphhChange_BlankOldCphh_ShowsRequiredMessage()
    {
        await GotoAsync("/Farm/CphhChange");
        await Page.Locator("#old-cphh").FillAsync("");
        await Page.GetByRole(AriaRole.Button, new() { Name = "Look Up" }).ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);

        await Expect(Page.Locator(".govuk-error-summary")).ToContainTextAsync("You must enter the old CPHH");
    }

    [Fact]
    public async Task CphhChange_UnknownOldCphh_ShowsNotFoundMessage()
    {
        await GotoAsync("/Farm/CphhChange");
        await Page.Locator("#old-cphh").FillAsync(UnknownCphh);
        await Page.GetByRole(AriaRole.Button, new() { Name = "Look Up" }).ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);

        await Expect(Page.Locator(".govuk-error-summary")).ToContainTextAsync("A farm with this CPHH was not found");
    }

    [Fact]
    public async Task DeleteCase_UnknownRbse_ShowsNotFoundMessage()
    {
        await GotoAsync("/Case/Delete");
        await Page.Locator("#rbse").FillAsync(UnknownRbse);
        await Page.GetByRole(AriaRole.Button, new() { Name = "Look Up" }).ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);

        await Expect(Page.Locator(".govuk-error-summary")).ToContainTextAsync("Cannot find a case with this RBSE");
    }

    [Fact]
    public async Task MoveCase_UnknownRbse_ShowsNotFoundMessage()
    {
        await GotoAsync("/Case/MoveCase");
        await Page.Locator("#rbse").FillAsync(UnknownRbse);
        await Page.GetByRole(AriaRole.Button, new() { Name = "Look Up" }).ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);

        await Expect(Page.Locator(".govuk-error-summary")).ToContainTextAsync("A case with this RBSE was not found");
    }
}
