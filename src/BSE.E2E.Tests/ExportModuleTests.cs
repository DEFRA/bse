using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace BSE.E2E.Tests;

/// <summary>
/// ADNS Export (/AdnsExport/Menu) and OSS Export (/OssExport/Menu) regression suite.
///
/// Scope boundary: ADNS "Dispatch" sends a real email via MS Graph and (when SaveAdnsData is
/// checked) permanently advances the ADNS reference sequence — a regulatory, zero-tolerance,
/// hard-to-reverse action. Only the read-only "Generate report" preview (no email, no DB write)
/// and validation paths are exercised here; Dispatch is never invoked.
/// </summary>
public sealed class ExportModuleTests(BrowserFixture fixture) : E2ETestBase(fixture)
{
    [Fact]
    public async Task AdnsExport_Menu_Renders_WithThreeLinks()
    {
        await GotoAsync("/AdnsExport/Menu");
        await Expect(Page.Locator("a[href='/AdnsExport/Gb']")).ToBeVisibleAsync();
        await Expect(Page.Locator("a[href='/AdnsExport/Ci']")).ToBeVisibleAsync();
        await Expect(Page.Locator("a[href='/AdnsExport/Ni']")).ToBeVisibleAsync();
    }

    [Theory]
    [InlineData("/AdnsExport/Gb", "ADNS Export : GB Cases")]
    [InlineData("/AdnsExport/Ci", "ADNS Export : Channel Island Cases")]
    [InlineData("/AdnsExport/Ni", "ADNS Export : Northern Ireland Cases")]
    public async Task AdnsExport_Page_Renders(string path, string expectedHeading)
    {
        await GotoAsync(path);
        await Expect(Page.Locator("h1")).ToHaveTextAsync(expectedHeading);
    }

    [Fact]
    public async Task AdnsExport_Gb_MissingEmailReference_ShowsValidationMessage()
    {
        await GotoAsync("/AdnsExport/Gb");
        await Page.Locator("input[name='EmailReference']").FillAsync("");
        await Page.GetByRole(AriaRole.Button, new() { Name = "Generate report", Exact = false }).ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);

        await Expect(Page.Locator(".govuk-error-summary")).ToContainTextAsync("Enter an email reference.");
    }

    [Fact]
    public async Task AdnsExport_Gb_GenerateReport_ReadOnlyPreviewRendersWithoutDispatch()
    {
        await GotoAsync("/AdnsExport/Gb");
        // Default-populated EmailReference/AdnsYear/StartAdnsNumber are valid as-is; only
        // "Generate report" (preview) is submitted — never "Dispatch".
        await Page.GetByRole(AriaRole.Button, new() { Name = "Generate report", Exact = false }).ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);

        await Expect(Page.Locator(".govuk-error-summary")).Not.ToBeVisibleAsync();
    }

    [Fact]
    public async Task OssExport_Menu_Renders_WithTwoLinks()
    {
        await GotoAsync("/OssExport/Menu");
        await Expect(Page.Locator("a[href='/OssExport/Bse1']")).ToBeVisibleAsync();
        await Expect(Page.Locator("a[href='/OssExport/Bse1b']")).ToBeVisibleAsync();
    }

    [Theory]
    [InlineData("/OssExport/Bse1", "OSS Export : BSE1")]
    [InlineData("/OssExport/Bse1b", "OSS Export : BSE1b")]
    public async Task OssExport_Page_Renders(string path, string expectedHeading)
    {
        await GotoAsync(path);
        await Expect(Page.Locator("h1")).ToHaveTextAsync(expectedHeading);
    }

    [Fact]
    public async Task OssExport_Bse1_MissingBatchNumber_ShowsValidationMessage()
    {
        await GotoAsync("/OssExport/Bse1");
        await Page.GetByRole(AriaRole.Button, new() { Name = "Download", Exact = false }).ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);

        await Expect(Page.Locator("body")).ToContainTextAsync("Enter a batch number.");
    }

    [Fact]
    public async Task OssExport_Bse1_UnknownBatch_ShowsNotFoundMessage()
    {
        await GotoAsync("/OssExport/Bse1");
        await Page.Locator("input[name='BatchYear']").FillAsync("1901");
        await Page.Locator("input[name='BatchNumber']").FillAsync("999999");
        await Page.GetByRole(AriaRole.Button, new() { Name = "Download", Exact = false }).ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);

        await Expect(Page.Locator("body")).ToContainTextAsync("was not found.");
    }
}
