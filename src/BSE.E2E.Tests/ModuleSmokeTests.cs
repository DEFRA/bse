namespace BSE.E2E.Tests;

/// <summary>
/// SMOKE-01/02/03 — every page in each module is requested against the real database and
/// asserted not to fault. Pages returning 200 are additionally loaded in a real browser and
/// checked for an error page.
/// Test names carry the module prefix so the report groups cleanly by module.
/// </summary>
public sealed class ModuleSmokeTests(BrowserFixture fixture) : E2ETestBase(fixture)
{
    [Theory]
    [InlineData("Home", "/Home")]
    [InlineData("Help", "/Help")]
    [InlineData("MaintenanceConfirmation", "/MaintenanceConfirmation")]
    public async Task Module_Core_PageRenders(string name, string path)
    {
        _ = name;
        await AssertPageRendersAsync(path);
    }

    [Theory]
    [InlineData("Search menu", "/Search/Menu")]
    [InlineData("Search cases", "/Search/Cases")]
    [InlineData("Search farms", "/Search/Farms")]
    [InlineData("Search outstanding", "/Search/Outstanding")]
    [InlineData("Search related animals", "/Search/RelatedAnimals")]
    [InlineData("Search by herdmark", "/Search/CasesByHerdmark")]
    [InlineData("Search by holding herdmark", "/Search/CasesByHoldingHerdmark")]
    public async Task Module_Search_PageRenders(string name, string path)
    {
        _ = name;
        await AssertPageRendersAsync(path);
    }

    // rbse is a route segment (@page "{rbse}"), not a query parameter.
    [Theory]
    [InlineData("Case DEFRA tab", "/Case/Edit")]
    [InlineData("Case farm tab", "/Case/Farm")]
    [InlineData("Case VLA tab", "/Case/Vla")]
    [InlineData("Case BAB tab", "/Case/Bab")]
    [InlineData("Case clinical tab", "/Case/Clinical")]
    [InlineData("Case feeds tab", "/Case/Feeds")]
    [InlineData("Case relations tab", "/Case/Relations")]
    [InlineData("Case test results tab", "/Case/TestResults")]
    [InlineData("Case DEFRA edit panel", "/Case/DefraEdit")]
    [InlineData("Case relations view", "/Case/RelationsView")]
    [InlineData("Case pick farm", "/Case/PickFarm")]
    public async Task Module_CaseTabs_PageRenders(string name, string path)
    {
        _ = name;
        await AssertPageRendersAsync($"{path}/{E2EConfig.Rbse}");
    }

    [Theory]
    [InlineData("Audit menu", "/AuditLog/Menu")]
    [InlineData("Audit by case", "/AuditLog/ByCase")]
    [InlineData("Audit by date", "/AuditLog/ByDate")]
    [InlineData("Audit by farm", "/AuditLog/ByFarm")]
    [InlineData("Audit by user", "/AuditLog/ByUser")]
    [InlineData("Audit case moves", "/AuditLog/CaseMoves")]
    [InlineData("Audit CPHH changes", "/AuditLog/CphhChanges")]
    [InlineData("Audit new farms", "/AuditLog/NewFarms")]
    public async Task Module_AuditLog_PageRenders(string name, string path)
    {
        _ = name;
        await AssertPageRendersAsync(path);
    }

    [Theory]
    [InlineData("Casework menu", "/CaseWork/Menu")]
    [InlineData("Casework open cases", "/CaseWork/OpenCases")]
    [InlineData("Casework closed cases", "/CaseWork/ClosedCases")]
    public async Task Module_CaseWork_PageRenders(string name, string path)
    {
        _ = name;
        await AssertPageRendersAsync(path);
    }

    [Theory]
    [InlineData("Admin users", "/Admin/Users")]
    [InlineData("Admin pick lists", "/Admin/PickLists")]
    public async Task Module_Admin_PageRenders(string name, string path)
    {
        _ = name;
        await AssertPageRendersAsync(path);
    }

    [Theory]
    [InlineData("Farm lookup", "/Farm/Lookup")]
    [InlineData("Farm new", "/Farm/New")]
    public async Task Module_Farm_PageRenders(string name, string path)
    {
        _ = name;
        await AssertPageRendersAsync(path);
    }

    // cphh is a route segment (@page "{cphh}").
    [Theory]
    [InlineData("Farm details", "/Farm/Details")]
    [InlineData("Farm edit", "/Farm/Edit")]
    public async Task Module_Farm_CphhPageRenders(string name, string path)
    {
        _ = name;
        await AssertPageRendersAsync($"{path}/{E2EConfig.Cphh}");
    }

    [Theory]
    [InlineData("OSS export menu", "/OssExport/Menu")]
    [InlineData("BSESS check by RBSE", "/Bsess/CheckByRbse")]
    [InlineData("BSESS check by date", "/Bsess/CheckByDate")]
    public async Task Module_Exports_PageRenders(string name, string path)
    {
        _ = name;
        await AssertPageRendersAsync(path);
    }

    [Theory]
    [InlineData("Error", "/Error")]
    [InlineData("Session error", "/SessionError")]
    [InlineData("Unauthorized", "/Unauthorized")]
    public async Task Module_ErrorPages_PageRenders(string name, string path)
    {
        _ = name;
        await AssertPageRendersAsync(path);
    }

    // ERR-02: bad input must be handled gracefully (any non-5xx), never a server fault.
    [Theory]
    [InlineData("unknown rbse", "/Case/Edit/000000000")]
    [InlineData("malformed rbse", "/Case/Edit/%27%22abc")]
    [InlineData("unknown cphh", "/Farm/Details/00000000000")]
    [InlineData("non-numeric feed id", "/Case/FeedEdit/990100001/notanint")]
    public async Task Module_ErrorHandling_BadInputDoesNotFault(string name, string path)
    {
        _ = name;
        await AssertPageHealthyAsync(path);
    }
}
