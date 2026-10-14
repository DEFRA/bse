using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace BSE.E2E.Tests;

/// <summary>
/// Casework menu (/CaseWork/Menu) regression suite — open/closed case lists and batch printing.
/// </summary>
public sealed class CaseWorkModuleTests(BrowserFixture fixture) : E2ETestBase(fixture)
{
    [Fact]
    public async Task Menu_Renders_WithBothLinks()
    {
        await GotoAsync("/CaseWork/Menu");
        await Expect(Page.Locator("a[href='/CaseWork/OpenCases']")).ToHaveTextAsync("View all open cases");
        await Expect(Page.Locator("a[href='/CaseWork/ClosedCases']")).ToHaveTextAsync("View all closed cases");
    }

    [Fact]
    public async Task OpenCases_Renders_WithSortableGrid()
    {
        await GotoAsync("/CaseWork/OpenCases");
        var rbseHeader = Page.Locator("table thead th").First;
        await Expect(rbseHeader).ToBeVisibleAsync();
    }

    [Fact]
    public async Task OpenCases_SortByRbse_TogglesDescendingThenAscending()
    {
        await GotoAsync("/CaseWork/OpenCases");

        // Grid defaults to SortColumn=Rbse ascending (OpenCasesModel.SortColumn default), so the
        // first click on an already-active column toggles to descending, not ascending.
        var rbseHeader = Page.Locator("table thead th").First;
        await Expect(rbseHeader).ToHaveAttributeAsync("aria-sort", "ascending");

        await rbseHeader.Locator("a").ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
        await Expect(Page.Locator("table thead th").First).ToHaveAttributeAsync("aria-sort", "descending");

        await Page.Locator("table thead th").First.Locator("a").ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
        await Expect(Page.Locator("table thead th").First).ToHaveAttributeAsync("aria-sort", "ascending");
    }

    [Fact]
    public async Task ClosedCases_Renders()
    {
        await GotoAsync("/CaseWork/ClosedCases");
        await Expect(Page.Locator("table")).ToBeVisibleAsync();
    }

    [Fact]
    public async Task PrintBatch_Renders_WithForm()
    {
        await GotoAsync("/CaseWork/PrintBatch");
        await Expect(Page.Locator("#batch-year")).ToBeVisibleAsync();
        await Expect(Page.Locator("#batch-number")).ToBeVisibleAsync();
    }

    [Fact]
    public async Task PrintBatch_SubmitWithNothingEntered_ShowsValidationMessages()
    {
        await GotoAsync("/CaseWork/PrintBatch");
        await Page.GetByRole(AriaRole.Button, new() { Name = "Download" }).ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);

        var errors = Page.Locator("#print-batch-error-summary");
        await Expect(errors).ToContainTextAsync("Enter batch number.");
        await Expect(errors).ToContainTextAsync("Select report type.");
    }

    [Fact]
    public async Task PrintBatch_InvalidYearFormat_ShowsValidationMessage()
    {
        await GotoAsync("/CaseWork/PrintBatch");
        await Page.Locator("#batch-year").FillAsync("abc");
        await Page.Locator("#batch-number").FillAsync("1");
        await Page.GetByRole(AriaRole.Button, new() { Name = "Download" }).ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);

        await Expect(Page.Locator("#print-batch-error-summary")).ToContainTextAsync("Enter a four digit year");
    }
}
