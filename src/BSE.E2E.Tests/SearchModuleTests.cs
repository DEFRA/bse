using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace BSE.E2E.Tests;

/// <summary>
/// SEARCH-01..04 — the entire Search section (/Search/Menu and its six sub-pages) regression
/// suite, driven through the real browser against the real database.
///
/// All searches are read-only (GET), so no cleanup is required. Test data below is real,
/// pre-existing seeded data in the local BSE database (not created by this suite):
/// Case RBSE 000000001 / CPHH 03043000301 / EartagHerdmark B2668, owner "S.R SMITH &amp; SONS",
/// which also has 3 related-animal (OFFSPRING) rows.
/// </summary>
public sealed class SearchModuleTests(BrowserFixture fixture) : E2ETestBase(fixture)
{
    private const string SeedRbse = "000000001";
    private const string SeedRbseFormatted = "00/00/00001"; // display format: CC/YY/NNNNN
    private const string SeedCphh = "03043000301";
    private const string SeedCphhFormatted = "03/043/0003/01"; // display format: NN/NNN/NNNN/NN
    private const string SeedHerdmark = "B2668";
    private const string SeedOwnerName = "S.R SMITH & SONS";
    private const string NoMatchRbse = "999999999";

    [Fact]
    public async Task Menu_Renders_WithLinksToAllSixSearchPages()
    {
        await GotoAsync("/Search/Menu");

        foreach (var (name, href) in new[]
        {
            ("Farm Search", "/Search/Farms"),
            ("Case Search", "/Search/Cases"),
            ("List Of Cases With A Given Herdmark (includes animals sold off)", "/Search/CasesByHerdmark"),
            ("List Of Cases For A Given Holding/Herdmark (for organic farmer checks)", "/Search/CasesByHoldingHerdmark"),
            ("Related Animals Search", "/Search/RelatedAnimals"),
            ("Outstanding Data List", "/Search/Outstanding"),
        })
        {
            await Expect(Page.Locator($"a[href='{href}']")).ToHaveTextAsync(name);
        }
    }

    [Fact]
    public async Task Menu_CaseSearchLink_NavigatesToCaseSearchPage()
    {
        await GotoAsync("/Search/Menu");
        await Page.Locator("a[href='/Search/Cases']").ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
        await Expect(Page.Locator("h1")).ToHaveTextAsync("Case Search");
    }

    // ── Case Search ──────────────────────────────────────────────────────────

    [Fact]
    public async Task CaseSearch_NoCriteria_ShowsValidationMessage()
    {
        await GotoAsync("/Search/Cases?Searched=true");
        await Expect(Page.Locator(".govuk-error-summary")).ToContainTextAsync("Please provide one or more search criteria");
    }

    [Fact]
    public async Task CaseSearch_InvalidRbseFormat_ShowsValidationMessage()
    {
        await GotoAsync("/Search/Cases?Searched=true&Filter.Rbse=not-a-number");
        await Expect(Page.Locator(".govuk-error-summary")).ToContainTextAsync("Enter RBSE as digits in the format NN/NN/NNNNN");
    }

    [Fact]
    public async Task CaseSearch_ByValidRbse_ReturnsSeededRecord()
    {
        await GotoAsync($"/Search/Cases?Searched=true&Filter.Rbse={SeedRbse}");
        await Expect(Page.Locator("#case-search-results")).ToContainTextAsync("1 result(s) found.");
        // Results grid, not the MOJ date-picker calendars (also rendered as <table>).
        await Expect(Page.GetByRole(AriaRole.Table, new() { Name = "Case search results" })).ToContainTextAsync(SeedCphhFormatted);
    }

    [Fact]
    public async Task CaseSearch_ByUnknownRbse_ReturnsZeroResults()
    {
        await GotoAsync($"/Search/Cases?Searched=true&Filter.Rbse={NoMatchRbse}");
        await Expect(Page.Locator("#case-search-results")).ToContainTextAsync("0 result(s) found.");
    }

    // ── Farm Search ──────────────────────────────────────────────────────────

    [Fact]
    public async Task FarmSearch_NoCriteria_ShowsValidationMessage()
    {
        await GotoAsync("/Search/Farms?Searched=true");
        await Expect(Page.Locator(".govuk-error-summary")).ToContainTextAsync("Please provide one or more search criteria");
    }

    [Fact]
    public async Task FarmSearch_ByValidCphh_ReturnsSeededRecord()
    {
        await GotoAsync($"/Search/Farms?Searched=true&Filter.Cphh={SeedCphh}");
        await Expect(Page.Locator("#farm-search-results")).ToContainTextAsync("1 result(s) found.");
        await Expect(Page.Locator("table")).ToContainTextAsync(SeedOwnerName);
    }

    [Fact]
    public async Task FarmSearch_ByUnknownCphh_ReturnsZeroResults()
    {
        await GotoAsync("/Search/Farms?Searched=true&Filter.Cphh=00000000000");
        await Expect(Page.Locator("#farm-search-results")).ToContainTextAsync("0 result(s) found.");
    }

    // ── Cases By Herdmark ────────────────────────────────────────────────────

    [Fact]
    public async Task CasesByHerdmark_ByValidHerdmark_ReturnsSeededRecord()
    {
        await GotoAsync($"/Search/CasesByHerdmark?Searched=true&Herdmark={SeedHerdmark}");
        await Expect(Page.Locator("#herdmark-search-results")).ToContainTextAsync("result(s) found.");
        await Expect(Page.Locator("table")).ToContainTextAsync(SeedRbseFormatted);
    }

    [Fact]
    public async Task CasesByHerdmark_HerdmarkTooLong_ShowsValidationMessage()
    {
        await GotoAsync($"/Search/CasesByHerdmark?Searched=true&Herdmark={new string('X', 40)}");
        await Expect(Page.Locator("body")).ToContainTextAsync("Herdmark must be 35 characters or fewer.");
    }

    [Fact]
    public async Task CasesByHerdmark_ByUnknownHerdmark_ReturnsZeroResults()
    {
        await GotoAsync("/Search/CasesByHerdmark?Searched=true&Herdmark=ZZNOPE999");
        await Expect(Page.Locator("#herdmark-search-results")).ToContainTextAsync("0 result(s) found.");
    }

    // ── Cases By Holding/Herdmark ────────────────────────────────────────────

    [Fact]
    public async Task CasesByHoldingHerdmark_NoCriteria_ShowsValidationMessage()
    {
        await GotoAsync("/Search/CasesByHoldingHerdmark?Searched=true");
        await Expect(Page.Locator(".govuk-error-summary")).ToContainTextAsync("Please provide one or more search criteria");
    }

    [Fact]
    public async Task CasesByHoldingHerdmark_ByValidCphh_ReturnsSeededRecord()
    {
        await GotoAsync($"/Search/CasesByHoldingHerdmark?Searched=true&Cphh={SeedCphh}");
        await Expect(Page.Locator("#holding-herdmark-search-results")).ToContainTextAsync("result(s) found.");
        await Expect(Page.Locator("table")).ToContainTextAsync(SeedRbseFormatted);
    }

    [Fact]
    public async Task CasesByHoldingHerdmark_InvalidNumericHerdmark_ShowsValidationMessage()
    {
        await GotoAsync("/Search/CasesByHoldingHerdmark?Searched=true&NumericHerdmark=123");
        await Expect(Page.Locator("body")).ToContainTextAsync("Numeric herdmark must be 6 digits.");
    }

    // ── Related Animals ──────────────────────────────────────────────────────

    [Fact]
    public async Task RelatedAnimals_NoCriteria_ShowsValidationMessage()
    {
        await GotoAsync("/Search/RelatedAnimals?Searched=true");
        await Expect(Page.Locator(".govuk-error-summary")).ToContainTextAsync("Please provide one or more search criteria");
    }

    [Fact]
    public async Task RelatedAnimals_InvalidRbseFormat_ShowsValidationMessage()
    {
        await GotoAsync("/Search/RelatedAnimals?Searched=true&Rbse=not-a-number");
        await Expect(Page.Locator("body")).ToContainTextAsync("Enter RBSE as digits in the format NN/NN/NNNNN");
    }

    [Fact]
    public async Task RelatedAnimals_ByValidRbse_ReturnsSeededRelations()
    {
        await GotoAsync($"/Search/RelatedAnimals?Searched=true&Rbse={SeedRbse}");
        await Expect(Page.Locator("#related-animals-search-results")).ToContainTextAsync("result(s) found.");
        await Expect(Page.Locator("table")).ToContainTextAsync("Offspring");
    }

    // ── Outstanding Data Search ──────────────────────────────────────────────

    [Fact]
    public async Task Outstanding_NoOptionSelected_ShowsValidationMessage()
    {
        await GotoAsync("/Search/Outstanding?Searched=true");
        await Expect(Page.Locator(".govuk-error-summary")).ToContainTextAsync("Please select one of these three options");
    }

    [Fact]
    public async Task Outstanding_ResultsOptionSelected_RendersResultsWithoutError()
    {
        await GotoAsync("/Search/Outstanding?Searched=true&Filter.SearchType=Results");
        await Expect(Page.Locator("#outstanding-search-results")).ToContainTextAsync("result(s) found.");
        await Expect(Page.Locator(".govuk-error-summary")).Not.ToBeVisibleAsync();
    }

    [Fact]
    public async Task Outstanding_FormADateFromAfterTo_ShowsValidationMessage()
    {
        await GotoAsync("/Search/Outstanding?Searched=true&Filter.SearchType=Results" +
            "&Filter.EarliestFormADate=31/12/2026&Filter.LatestFormADate=01/01/2026");
        // Outstanding uses a shorter, generic message (no field name) vs. Case Search's worded version.
        await Expect(Page.Locator(".govuk-error-summary")).ToContainTextAsync("Must be earlier than the latest date");
    }

    [Fact]
    public async Task Outstanding_InvalidDateFormat_ShowsValidationMessage()
    {
        await GotoAsync("/Search/Outstanding?Searched=true&Filter.SearchType=Results&Filter.EarliestFormADate=notadate");
        await Expect(Page.Locator(".govuk-error-summary")).ToContainTextAsync("Enter a valid date");
    }
}
