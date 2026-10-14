using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace BSE.E2E.Tests;

/// <summary>
/// ADMIN-01 — User Maintenance (/Admin/Users) regression suite, driven through the real UI
/// against the real database.
///
/// Destructive-test policy: the grid has no delete action (only deactivate), so every test
/// that adds a user ends by deactivating it — there is no permanent, active test clutter left
/// behind. Edit-only tests restore the original value in a finally block.
/// </summary>
public sealed class AdminUsersMaintenanceTests(BrowserFixture fixture) : E2ETestBase(fixture)
{
    // Guaranteed to exist locally (the DevUserEmail configured for the bypass handler).
    private const string ExistingUserEmail = "gunjan.arya@defra.gov.uk";

    private ILocator NewButton => Page.Locator("button[data-action='start-new']");
    private ILocator EditButton => Page.Locator("button[data-action='start-edit']");
    private ILocator SaveButton => Page.Locator("button[data-action='save']");
    private ILocator CancelButton => Page.Locator("button[data-action='cancel']");
    private ILocator SuccessBanner => Page.Locator(".govuk-notification-banner--success");
    private ILocator ErrorSummary => Page.Locator(".govuk-error-summary");

    private static string UniqueEmail(string tag) => $"e2e.{tag}.{Guid.NewGuid():N}@example.test";

    [Fact]
    public async Task AdminUsers_PageRenders_WithGridAndActionButtons()
    {
        await GotoAsync("/Admin/Users");
        await Expect(Page.Locator("table.bse-user-maintenance-table")).ToBeVisibleAsync();
        await Expect(NewButton).ToBeVisibleAsync();
        await Expect(EditButton).ToBeVisibleAsync();
        await Expect(SaveButton).ToBeVisibleAsync();
        await Expect(CancelButton).ToBeVisibleAsync();
    }

    // Full lifecycle: add -> edit -> deactivate, each step verified. Ends deactivated so no
    // active test record is left behind.
    [Fact]
    public async Task AdminUsers_AddEditDeactivateUser_PersistsEachChange()
    {
        var email = UniqueEmail("lifecycle");
        const string name = "E2E Lifecycle User";
        const string updatedName = "E2E Lifecycle User Updated";

        await GotoAsync("/Admin/Users");
        await NewButton.ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);

        await Expect(Page.Locator("#edit-user-name-new")).ToBeVisibleAsync();
        await Page.Locator("#edit-user-name-new").FillAsync(name);
        await Page.Locator("#edit-email-new").FillAsync(email);
        await Page.Locator("#edit-user-group-id-new").SelectOptionAsync(new SelectOptionValue { Label = "DEFRA Viewer" });
        await SaveButton.ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);

        await Expect(SuccessBanner).ToContainTextAsync($"User '{name}' added.");

        var selectedRow = Page.Locator("tr.bse-row-selected");
        await Expect(selectedRow).ToBeVisibleAsync();
        var userId = await selectedRow.GetAttributeAsync("data-user-id");
        Assert.False(string.IsNullOrEmpty(userId), "Newly added row has no data-user-id");

        // Edit: change display name and group.
        await EditButton.ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
        await Page.Locator($"#edit-user-name-{userId}").FillAsync(updatedName);
        await Page.Locator($"#edit-user-group-id-{userId}").SelectOptionAsync(new SelectOptionValue { Label = "VLA Data Entry" });
        await SaveButton.ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);

        await Expect(SuccessBanner).ToContainTextAsync($"User '{updatedName}' updated.");
        await Expect(Page.Locator($"tr[data-user-id='{userId}']")).ToContainTextAsync(updatedName);
        await Expect(Page.Locator($"tr[data-user-id='{userId}']")).ToContainTextAsync("VLA Data Entry");

        // Deactivate.
        await EditButton.ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
        await Page.Locator($"#edit-is-active-{userId}").UncheckAsync();
        await SaveButton.ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);

        var activeCheckbox = Page.Locator($"#IsActive_{userId}");
        await Expect(activeCheckbox).ToBeDisabledAsync();
        await Expect(activeCheckbox).Not.ToBeCheckedAsync();
    }

    [Fact]
    public async Task AdminUsers_AddNewUser_MissingDisplayName_ShowsValidationErrorAndDoesNotSave()
    {
        var email = UniqueEmail("missingname");

        await GotoAsync("/Admin/Users");
        await NewButton.ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);

        await Page.Locator("#edit-email-new").FillAsync(email);
        await Page.Locator("#edit-user-group-id-new").SelectOptionAsync(new SelectOptionValue { Label = "DEFRA Viewer" });
        await SaveButton.ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);

        await Expect(ErrorSummary).ToContainTextAsync("Enter a display name");
        Assert.DoesNotContain("added.", await Page.ContentAsync());
    }

    [Fact]
    public async Task AdminUsers_AddNewUser_InvalidEmailFormat_ShowsValidationErrorAndDoesNotSave()
    {
        await GotoAsync("/Admin/Users");
        await NewButton.ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);

        await Page.Locator("#edit-user-name-new").FillAsync("E2E Invalid Email User");
        await Page.Locator("#edit-email-new").FillAsync("not-an-email");
        await Page.Locator("#edit-user-group-id-new").SelectOptionAsync(new SelectOptionValue { Label = "DEFRA Viewer" });
        await SaveButton.ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);

        await Expect(ErrorSummary).ToContainTextAsync("Enter an email address in the correct format");
        Assert.DoesNotContain("added.", await Page.ContentAsync());
    }

    [Fact]
    public async Task AdminUsers_AddNewUser_DuplicateEmail_ShowsValidationErrorAndDoesNotSave()
    {
        await GotoAsync("/Admin/Users");
        await NewButton.ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);

        await Page.Locator("#edit-user-name-new").FillAsync("E2E Duplicate Email User");
        await Page.Locator("#edit-email-new").FillAsync(ExistingUserEmail);
        await Page.Locator("#edit-user-group-id-new").SelectOptionAsync(new SelectOptionValue { Label = "DEFRA Viewer" });
        await SaveButton.ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);

        await Expect(ErrorSummary).ToContainTextAsync("Unable to add the selected user");
        Assert.DoesNotContain("added.", await Page.ContentAsync());
    }

    [Fact]
    public async Task AdminUsers_CancelAddNewUser_DiscardsChanges()
    {
        const string transientName = "E2E Should Not Persist";

        await GotoAsync("/Admin/Users");
        await NewButton.ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);

        await Page.Locator("#edit-user-name-new").FillAsync(transientName);
        await CancelButton.ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);

        Assert.DoesNotContain(transientName, await Page.ContentAsync());
        await Expect(Page.Locator("#edit-user-name-new")).Not.ToBeVisibleAsync();
    }

    [Fact]
    public async Task AdminUsers_SortByName_TogglesDescendingThenAscending()
    {
        await GotoAsync("/Admin/Users");

        // Grid defaults to SortColumn=UserName ascending (UsersModel.SortColumn default), so the
        // first click on an already-active column toggles to descending, not ascending.
        var nameHeader = Page.Locator("table.bse-user-maintenance-table thead th").Nth(1);
        await Expect(nameHeader).ToHaveAttributeAsync("aria-sort", "ascending");

        await nameHeader.Locator("a").ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
        await Expect(Page.Locator("table.bse-user-maintenance-table thead th").Nth(1)).ToHaveAttributeAsync("aria-sort", "descending");

        await Page.Locator("table.bse-user-maintenance-table thead th").Nth(1).Locator("a").ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
        await Expect(Page.Locator("table.bse-user-maintenance-table thead th").Nth(1)).ToHaveAttributeAsync("aria-sort", "ascending");
    }
}
