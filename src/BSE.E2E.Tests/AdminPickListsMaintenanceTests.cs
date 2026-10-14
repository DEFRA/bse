using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace BSE.E2E.Tests;

/// <summary>
/// ADMIN-02 — Pick List Maintenance (/Admin/PickLists, /PickListsAdd, /PickListsEdit)
/// regression suite, driven through the real UI against the real database.
///
/// Table under test is Horizontal Risk (TableId 3) — a generic Code/Description table with
/// no special-cased fields, so Add/Edit/Delete exercise the default code path. Every test
/// that adds a record deletes it again (the page supports a real delete, unlike User
/// Maintenance), leaving the database byte-for-byte as found.
/// </summary>
public sealed class AdminPickListsMaintenanceTests(BrowserFixture fixture) : E2ETestBase(fixture)
{
    private const int HorizontalRiskTableId = 3;
    private const string ExistingCode = "HIGH"; // seeded row, used for duplicate-code checks

    private static string UniqueCode() => $"E2{Guid.NewGuid():N}"[..8].ToUpperInvariant();

    private string PickListsUrl(int tableId) => $"/Admin/PickLists?tableId={tableId}";

    private ILocator AddLink => Page.GetByRole(AriaRole.Link, new() { Name = "Add a new record" });
    private ILocator SaveButton => Page.GetByRole(AriaRole.Button, new() { Name = "Save", Exact = true });
    private ILocator ErrorSummary => Page.Locator(".govuk-error-summary");

    private async Task DeleteRowAsync(string code)
    {
        var row = Page.Locator("tr", new() { HasText = code });
        void OnDialog(object? _, IDialog dialog) => dialog.AcceptAsync();
        Page.Dialog += OnDialog;
        try
        {
            await row.GetByRole(AriaRole.Button, new() { Name = "Delete", Exact = false }).ClickAsync();
            await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
        }
        finally
        {
            Page.Dialog -= OnDialog;
        }
    }

    [Fact]
    public async Task PickLists_PageRenders_WithTableSelectorAndGrid()
    {
        await GotoAsync("/Admin/PickLists");
        await Expect(Page.Locator("#tableId")).ToBeVisibleAsync();
        await Expect(Page.Locator("table.govuk-table")).ToBeVisibleAsync();
        await Expect(AddLink).ToBeVisibleAsync(); // current dev user is VLA Maintenance (CanEdit)
    }

    [Fact]
    public async Task PickLists_SwitchTable_ShowsSelectedTableRecords()
    {
        await GotoAsync("/Admin/PickLists");
        await Expect(Page.Locator("h2.govuk-heading-m")).ToHaveTextAsync("Valuation Age"); // default table (ID 11)

        await Page.Locator("#tableId").SelectOptionAsync(new SelectOptionValue { Label = "Breed" });
        await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);

        await Expect(Page.Locator("h2.govuk-heading-m")).ToHaveTextAsync("Breed");
    }

    [Fact]
    public async Task PickLists_SortByCode_TogglesAscendingThenDescending()
    {
        await GotoAsync(PickListsUrl(HorizontalRiskTableId));

        var codeHeader = Page.Locator("table.govuk-table thead th").First;
        await codeHeader.Locator("a").ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
        await Expect(Page.Locator("table.govuk-table thead th").First).ToHaveAttributeAsync("aria-sort", "ascending");

        await Page.Locator("table.govuk-table thead th").First.Locator("a").ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
        await Expect(Page.Locator("table.govuk-table thead th").First).ToHaveAttributeAsync("aria-sort", "descending");
    }

    // Full lifecycle: add -> edit -> delete, each step verified. Ends with zero residual rows.
    [Fact]
    public async Task PickLists_AddEditDeleteRecord_PersistsEachChangeAndCleansUp()
    {
        var code = UniqueCode();
        const string description = "E2E regression risk";
        const string updatedDescription = "E2E regression risk updated";

        await GotoAsync(PickListsUrl(HorizontalRiskTableId));
        await AddLink.ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);

        await Page.Locator("#field-Code").FillAsync(code);
        await Page.Locator("#field-Description").FillAsync(description);
        await SaveButton.ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);

        try
        {
            await Expect(Page.Locator("body")).ToContainTextAsync("Record added.");
            await Expect(Page.Locator("tr", new() { HasText = code })).ToContainTextAsync(description);

            // Edit: change the description.
            await Page.Locator("tr", new() { HasText = code }).GetByRole(AriaRole.Link, new() { Name = "Change", Exact = false }).ClickAsync();
            await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
            await Expect(Page.Locator("#field-Code")).ToHaveValueAsync(code);

            await Page.Locator("#field-Description").FillAsync(updatedDescription);
            await SaveButton.ClickAsync();
            await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);

            await Expect(Page.Locator("body")).ToContainTextAsync("Record updated.");
            await Expect(Page.Locator("tr", new() { HasText = code })).ToContainTextAsync(updatedDescription);
        }
        finally
        {
            // Delete: always clean up, even if the edit assertions above failed.
            await DeleteRowAsync(code);
        }

        await Expect(Page.Locator("body")).ToContainTextAsync("Record deleted.");
        await Expect(Page.Locator("tr", new() { HasText = code })).Not.ToBeVisibleAsync();
    }

    [Fact]
    public async Task PickLists_AddNewRecord_MissingDescription_ShowsValidationErrorAndDoesNotSave()
    {
        var code = UniqueCode();

        await GotoAsync(PickListsUrl(HorizontalRiskTableId));
        await AddLink.ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);

        await Page.Locator("#field-Code").FillAsync(code);
        await SaveButton.ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);

        await Expect(ErrorSummary).ToContainTextAsync("Enter a description");
        Assert.DoesNotContain("Record added.", await Page.ContentAsync());
    }

    [Fact]
    public async Task PickLists_AddNewRecord_DuplicateCode_ShowsValidationErrorAndDoesNotSave()
    {
        await GotoAsync(PickListsUrl(HorizontalRiskTableId));
        await AddLink.ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);

        await Page.Locator("#field-Code").FillAsync(ExistingCode);
        await Page.Locator("#field-Description").FillAsync("Duplicate attempt");
        await SaveButton.ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);

        await Expect(ErrorSummary).ToContainTextAsync(PickListsDuplicateMessage);
        Assert.DoesNotContain("Record added.", await Page.ContentAsync());
    }

    [Fact]
    public async Task PickLists_EditRecord_DuplicateCode_ShowsValidationErrorAndDoesNotSave()
    {
        var code = UniqueCode();

        await GotoAsync(PickListsUrl(HorizontalRiskTableId));
        await AddLink.ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
        await Page.Locator("#field-Code").FillAsync(code);
        await Page.Locator("#field-Description").FillAsync("E2E duplicate-edit source");
        await SaveButton.ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);

        try
        {
            await Page.Locator("tr", new() { HasText = code }).GetByRole(AriaRole.Link, new() { Name = "Change", Exact = false }).ClickAsync();
            await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);

            await Page.Locator("#field-Code").FillAsync(ExistingCode);
            await SaveButton.ClickAsync();
            await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);

            await Expect(ErrorSummary).ToContainTextAsync(PickListsDuplicateMessage);
        }
        finally
        {
            await GotoAsync(PickListsUrl(HorizontalRiskTableId));
            await DeleteRowAsync(code);
        }
    }

    [Fact]
    public async Task PickLists_CancelAdd_DoesNotPersist()
    {
        var code = UniqueCode();

        await GotoAsync(PickListsUrl(HorizontalRiskTableId));
        await AddLink.ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);

        await Page.Locator("#field-Code").FillAsync(code);
        await Page.Locator("#field-Description").FillAsync("Should not persist");
        await Page.GetByRole(AriaRole.Link, new() { Name = "Cancel" }).ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);

        Assert.DoesNotContain(code, await Page.ContentAsync());
    }

    // Matches PickListsModel.DuplicateCodeMessage (internal to BSE.Host, duplicated here for the UI assertion).
    private const string PickListsDuplicateMessage = "The Code you have selected is already used";
}
