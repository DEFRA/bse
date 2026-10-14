using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace BSE.E2E.Tests;

/// <summary>
/// TSESS Check Menu (/Bsess/Menu, /Bsess/CheckByRbse, /Bsess/CheckByDate) regression suite,
/// driven through the real browser against the real database.
///
/// The local BSESSImport table has 1960 rows, mostly not matching [Case] by RBSE, so
/// GetCheckByDateAsync returns a large real discrepancy set for any wide date range.
/// Seed RBSE 000000001 does not appear in BSESSImport, so CheckByRbse for it is a one-sided
/// match: the BSE side (from [Case]) populates while the TSESS side stays blank.
/// Seed data: Case RBSE 000000001, EartagHerdmark B2668, Eartag 1644S, FormADate 03/01/2000.
/// </summary>
public sealed class BsessCheckMenuTests(BrowserFixture fixture) : E2ETestBase(fixture)
{
    private const string SeedRbse = "000000001";
    private const string UnknownRbse = "999999999";

    [Fact]
    public async Task Menu_Renders_WithLinksToBothCheckPages()
    {
        await GotoAsync("/Bsess/Menu");
        await Expect(Page.Locator("a[href='/Bsess/CheckByDate']")).ToHaveTextAsync("Compare cases by date");
        await Expect(Page.Locator("a[href='/Bsess/CheckByRbse']")).ToHaveTextAsync("Compare details by RBSE");
    }

    [Fact]
    public async Task Menu_CheckByRbseLink_Navigates()
    {
        await GotoAsync("/Bsess/Menu");
        await Page.Locator("a[href='/Bsess/CheckByRbse']").ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
        await Expect(Page.Locator("h1")).ToHaveTextAsync("TSESS Check : Compare Details By RBSE");
    }

    // ── Check By RBSE ────────────────────────────────────────────────────────

    [Fact]
    public async Task CheckByRbse_BlankRbse_ShowsValidationMessage()
    {
        await GotoAsync("/Bsess/CheckByRbse?Rbse=");
        await Expect(Page.Locator(".govuk-error-summary")).ToContainTextAsync("Enter an RBSE number");
    }

    [Fact]
    public async Task CheckByRbse_InvalidFormat_ShowsValidationMessage()
    {
        await GotoAsync("/Bsess/CheckByRbse?Rbse=not-a-number");
        await Expect(Page.Locator(".govuk-error-summary")).ToContainTextAsync("Enter the RBSE number in the format 00/00/00000");
    }

    [Fact]
    public async Task CheckByRbse_ValidExistingRbse_ShowsBseSideOfComparison()
    {
        await GotoAsync($"/Bsess/CheckByRbse?Rbse={SeedRbse}");
        await Expect(Page.Locator(".govuk-error-summary")).Not.ToBeVisibleAsync();
        await Expect(Page.Locator("table")).ToContainTextAsync("B2668 1644S");
        await Expect(Page.Locator("table")).ToContainTextAsync("03/01/2000");
    }

    [Fact]
    public async Task CheckByRbse_ValidButUnknownRbse_RendersWithoutErrorOrData()
    {
        await GotoAsync($"/Bsess/CheckByRbse?Rbse={UnknownRbse}");
        await Expect(Page.Locator(".govuk-error-summary")).Not.ToBeVisibleAsync();
        Assert.DoesNotContain("B2668", await Page.ContentAsync());
    }

    // ── Check By Date ────────────────────────────────────────────────────────

    [Fact]
    public async Task CheckByDate_MissingBothDates_ShowsValidationMessages()
    {
        await GotoAsync("/Bsess/CheckByDate?StartDate=&EndDate=");
        var errors = Page.Locator(".govuk-error-summary");
        await Expect(errors).ToContainTextAsync("Enter a start date");
        await Expect(errors).ToContainTextAsync("Enter an end date");
    }

    [Fact]
    public async Task CheckByDate_EndBeforeStart_ShowsValidationMessage()
    {
        await GotoAsync("/Bsess/CheckByDate?StartDate=31/12/2026&EndDate=01/01/2026");
        await Expect(Page.Locator(".govuk-error-summary")).ToContainTextAsync("The end date must be on or after the start date");
    }

    [Fact]
    public async Task CheckByDate_InvalidDateFormat_ShowsValidationMessage()
    {
        await GotoAsync("/Bsess/CheckByDate?StartDate=notadate&EndDate=01/01/2026");
        await Expect(Page.Locator("body")).ToContainTextAsync("Enter a date in the format 17/05/2024.");
    }

    [Fact]
    public async Task CheckByDate_ValidRange_ShowsDiscrepancies()
    {
        await GotoAsync("/Bsess/CheckByDate?StartDate=01/01/1900&EndDate=31/12/2026");
        await Expect(Page.Locator(".govuk-error-summary")).Not.ToBeVisibleAsync();
        await Expect(Page.GetByRole(AriaRole.Table, new() { Name = "TSESS discrepancies by date" })).ToBeVisibleAsync();
        await Expect(Page.Locator("nav[aria-label='Pagination']")).ToBeVisibleAsync();
    }

    [Fact]
    public async Task CheckByDate_NarrowFutureRange_ShowsNoDiscrepanciesBanner()
    {
        await GotoAsync("/Bsess/CheckByDate?StartDate=01/01/2999&EndDate=31/12/2999");
        await Expect(Page.Locator(".govuk-notification-banner--success")).ToContainTextAsync("No discrepancies found for this date range.");
    }
}
