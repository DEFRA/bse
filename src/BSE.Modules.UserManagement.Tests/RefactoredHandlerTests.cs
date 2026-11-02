using System.Security.Claims;
using BSE.Host.Models.ViewModels;
using BSE.Host.Pages;
using BSE.Host.Pages.AuditLog;
using BSE.Host.Pages.Case;
using BSE.Host.Services;
using BSE.Modules.AuditLog.Models;
using BSE.Modules.AuditLog.Services;
using BSE.Modules.Batch.Models;
using BSE.Modules.Batch.Repositories;
using BSE.Modules.Batch.Services;
using BSE.Modules.BsessIntegration.Models;
using BSE.Modules.BsessIntegration.Services;
using BSE.Modules.CaseManagement.Commands;
using BSE.Modules.CaseManagement.Enums;
using BSE.Modules.CaseManagement.Models;
using BSE.Modules.CaseManagement.Repositories;
using BSE.Modules.CaseManagement.Services;
using BSE.Modules.CaseWork.Repositories;
using BSE.Modules.FarmManagement.Services;
using BSE.Modules.ReferenceData.Models;
using BSE.Modules.ReferenceData.Services;
using BSE.SharedKernel;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace BSE.Modules.UserManagement.Tests;

#pragma warning disable xUnit1051 // NSubstitute's Arg.Any<CancellationToken>() is intentional here for default-token matching.

internal static class PageTestHelpers
{
    public static ClaimsPrincipal Principal(params string[] roles) =>
        new(new ClaimsIdentity(roles.Select(r => new Claim(ClaimTypes.Role, r)), "Test"));

    public static void Attach(PageModel model, params string[] roles)
    {
        var httpContext = new DefaultHttpContext { User = Principal(roles) };
        httpContext.Request.ContentType = "application/x-www-form-urlencoded";
        httpContext.Request.Form = new FormCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>());
        model.PageContext = new PageContext { HttpContext = httpContext };
        var url = Substitute.For<IUrlHelper>();
        url.ActionContext.Returns(new ActionContext(httpContext, new Microsoft.AspNetCore.Routing.RouteData(),
            new Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor()));
        model.Url = url;
        model.PageContext.ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), model.ModelState);
        model.TempData = new TempDataDictionary(httpContext, Substitute.For<ITempDataProvider>());
    }

    public static IEnumerable<string> ErrorMessages(PageModel model, string key) =>
        model.ModelState.TryGetValue(key, out var entry) ? entry.Errors.Select(e => e.ErrorMessage) : [];
}

public sealed class ByCaseModelTests
{
    private readonly IAuditLogService _service = Substitute.For<IAuditLogService>();

    private ByCaseModel Create(string sortBy = "datetime", string sortDir = "asc", int page = 1)
    {
        var model = new ByCaseModel(_service) { Rbse = "002600001", SortBy = sortBy, SortDir = sortDir, PageNumber = page };
        PageTestHelpers.Attach(model);
        return model;
    }

    private static List<AuditLogEntry> Entries() =>
    [
        new() { TableName = "B", FieldName = "y", UserName = "u2", BeforeValue = "2", AfterValue = "b", Reason = "rb", Key = "k2", DateTime = new DateTime(2024, 1, 2) },
        new() { TableName = "A", FieldName = "z", UserName = "u3", BeforeValue = "3", AfterValue = "c", Reason = "rc", Key = "k3", DateTime = new DateTime(2024, 1, 3) },
        new() { TableName = "C", FieldName = "x", UserName = "u1", BeforeValue = "1", AfterValue = "a", Reason = "ra", Key = "k1", DateTime = new DateTime(2024, 1, 1) },
    ];

    [Theory]
    [InlineData("table", "A", "C")]
    [InlineData("field", "C", "A")]
    [InlineData("user", "C", "A")]
    [InlineData("before", "C", "A")]
    [InlineData("after", "C", "A")]
    [InlineData("reason", "C", "A")]
    [InlineData("key", "C", "A")]
    [InlineData("datetime", "C", "A")]
    [InlineData("unknown", "C", "A")]
    public async Task OnGetAsync_SortsByEachColumnInBothDirections(string sortBy, string firstAscTable, string firstDescTable)
    {
        _service.GetByCaseAsync("002600001").Returns(Entries());

        var asc = Create(sortBy, "asc");
        await asc.OnGetAsync();
        var desc = Create(sortBy, "desc");
        await desc.OnGetAsync();

        asc.Entries.First().TableName.Should().Be(firstAscTable);
        desc.Entries.First().TableName.Should().Be(firstDescTable);
    }

    [Fact]
    public async Task OnGetAsync_WithBlankRbse_RedirectsHome()
    {
        var model = Create();
        model.Rbse = " ";

        var result = await model.OnGetAsync();

        result.Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/Home");
    }

    [Fact]
    public async Task OnGetAsync_PageNumberBeyondLastPage_ClampsToLastPage()
    {
        _service.GetByCaseAsync("002600001").Returns(Enumerable.Range(0, 25)
            .Select(i => new AuditLogEntry { TableName = $"T{i:00}", DateTime = new DateTime(2024, 1, 1).AddDays(i) }).ToList());
        var model = Create(page: 99);

        await model.OnGetAsync();

        model.PageNumber.Should().Be(2);
        model.TotalPages.Should().Be(2);
        model.Entries.Should().HaveCount(5);
    }

    [Fact]
    public async Task OnGetExportAsync_WritesCsvWithEscapedValues()
    {
        _service.GetByCaseAsync("002600001").Returns(new List<AuditLogEntry>
        {
            new() { TableName = "Case", FieldName = "Notes", UserName = "me", BeforeValue = "a,b", AfterValue = "say \"hi\"", Reason = "line1\nline2", Key = "k", DateTime = new DateTime(2024, 5, 6, 7, 8, 0) }
        });
        var model = Create();

        var result = await model.OnGetExportAsync();

        var file = result.Should().BeOfType<FileContentResult>().Subject;
        file.ContentType.Should().Be("text/csv");
        var text = System.Text.Encoding.UTF8.GetString(file.FileContents);
        text.Should().Contain("\"a,b\"").And.Contain("\"say \"\"hi\"\"\"").And.Contain("06/05/2024 07:08");
    }

    [Fact]
    public async Task OnGetExportAsync_WithBlankRbse_RedirectsToPage()
    {
        var model = Create();
        model.Rbse = "";

        (await model.OnGetExportAsync()).Should().BeOfType<RedirectToPageResult>();
    }

    [Fact]
    public void SortUrl_TogglesDirectionForCurrentColumnAndKeepsReturnTo()
    {
        var model = Create("user", "asc");
        model.ReturnTo = "/Case/Edit";

        model.SortUrl("user").Should().Contain("sortDir=desc").And.Contain("returnTo=%2FCase%2FEdit");
        model.SortUrl("table").Should().Contain("sortDir=asc");
    }
}

public sealed class HomeRbseLookupTests
{
    private readonly IBatchService _batchService = Substitute.For<IBatchService>();
    private readonly ICaseRepository _caseRepository = Substitute.For<ICaseRepository>();
    private readonly ICaseWizardStateService _wizardState = Substitute.For<ICaseWizardStateService>();

    private HomeModel Create(params string[] roles)
    {
        _batchService.GetLatestBatchNumbersAsync().Returns(Array.Empty<LatestBatchRecord>());
        var model = new HomeModel(_batchService, _caseRepository, _wizardState);
        PageTestHelpers.Attach(model, roles);
        return model;
    }

    [Fact]
    public async Task BatchYearWithoutNumber_AddsBatchNumberError()
    {
        var model = Create("DataEntry");
        model.BatchYear = 2026;
        model.LookupRbse = "000500001";

        var result = await model.OnPostRbseLookupAsync();

        result.Should().BeOfType<PageResult>();
        PageTestHelpers.ErrorMessages(model, "BatchNumber").Should().Contain("Enter a batch number.");
    }

    [Fact]
    public async Task UnknownBatch_ShowsNotFoundError()
    {
        var model = Create("DataEntry");
        model.BatchYear = 2026;
        model.BatchNumber = 99;
        _batchService.GetBatchIdAsync(Arg.Any<short?>(), 99).Returns((int?)null);

        var result = await model.OnPostRbseLookupAsync();

        result.Should().BeOfType<PageResult>();
        model.BatchNotFound.Should().BeTrue();
        PageTestHelpers.ErrorMessages(model, "BatchYear").Should().ContainSingle(m => m.Contains("was not found"));
    }

    [Fact]
    public async Task BlankRbse_ShowsFormatError()
    {
        var model = Create("DataEntry");

        await model.OnPostRbseLookupAsync();

        PageTestHelpers.ErrorMessages(model, "LookupRbse").Should().ContainSingle(m => m.StartsWith("Enter an RBSE"));
    }

    [Fact]
    public async Task ValidBatchAndExistingCase_StoresBatchAndRedirectsToFarm()
    {
        var model = Create("DataEntry");
        model.BatchYear = 2026;
        model.BatchNumber = 5;
        model.LookupRbse = "00/05/00001";
        _batchService.GetBatchIdAsync(Arg.Any<short?>(), 5).Returns(42);
        _caseRepository.GetCaseByRbseAsync(Arg.Any<string>()).Returns(new CaseRecord { Rbse = "000500001", IsNonGbCase = false });

        var result = await model.OnPostRbseLookupAsync();

        result.Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/Case/Farm");
        await _wizardState.Received(1).SetAsync(Arg.Is<CaseWizardState>(s => s.BatchId == 42));
    }

    [Fact]
    public async Task NoBatch_ClearsWizardState()
    {
        var model = Create("DataEntry");
        model.LookupRbse = "000500001";
        _caseRepository.GetCaseByRbseAsync(Arg.Any<string>()).Returns(new CaseRecord { Rbse = "000500001" });

        await model.OnPostRbseLookupAsync();

        await _wizardState.Received(1).ClearAsync();
    }

    [Theory]
    [InlineData("VLAAccess", true)]
    [InlineData("DataEntry", false)]
    public async Task ExistingNonGbCase_OnlyOpenableByVlaGroups(string role, bool allowed)
    {
        var model = Create(role);
        model.LookupRbse = "630000001";
        _caseRepository.GetCaseByRbseAsync(Arg.Any<string>()).Returns(new CaseRecord { Rbse = "630000001", IsNonGbCase = true });

        var result = await model.OnPostRbseLookupAsync();

        if (allowed)
            result.Should().BeOfType<RedirectToPageResult>();
        else
            PageTestHelpers.ErrorMessages(model, "LookupRbse").Should().Contain("This is not a GB case");
    }

    [Theory]
    [InlineData("VLAMaintenance", "630000001", "/Case/NewNonGb", null)]
    [InlineData("VLAAccess", "630000001", null, "This case does not exist")]
    [InlineData("DataEntry", "230000001", null, "This is not a GB case")]
    [InlineData("DEFRAAccess", "000500001", null, "This case does not exist")]
    [InlineData("VLAAccess", "000500001", null, "This case does not exist")]
    [InlineData("DataEntry", "00XX00001", null, "This RBSE is not valid for a new case")]
    [InlineData("DataEntry", "009900001", "/Case/Farm", null)]
    public async Task NewCase_AppliesPerRoleRestrictions(string role, string rbse, string? expectedRedirect, string? expectedError)
    {
        var model = Create(role);
        model.LookupRbse = rbse;
        _caseRepository.GetCaseByRbseAsync(Arg.Any<string>()).Returns((CaseRecord?)null);

        var result = await model.OnPostRbseLookupAsync();

        if (expectedRedirect is not null)
            result.Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be(expectedRedirect);
        else
            PageTestHelpers.ErrorMessages(model, "LookupRbse").Should().Contain(expectedError!);
    }
}

public sealed class VlaEditValidationTests
{
    private readonly ICaseService _caseService = Substitute.For<ICaseService>();
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly ILookupDataService _lookups = Substitute.For<ILookupDataService>();
    private readonly IBatchRepository _batchRepository = Substitute.For<IBatchRepository>();

    private VlaEditModel Create(params string[] roles)
    {
        _lookups.GetLookupAsync(Arg.Any<LookupTableId>()).Returns(Array.Empty<LookupItem>());
        var model = new VlaEditModel(_caseService, _currentUser, _lookups, _batchRepository,
            new ConfigurationBuilder().AddInMemoryCollection().Build()) { Rbse = "002600001" };
        PageTestHelpers.Attach(model, roles);
        return model;
    }

    [Fact]
    public async Task OnPostAsync_WithoutDataEntryRole_IsForbidden()
    {
        var model = Create("VLAAccess");

        (await model.OnPostAsync()).Should().BeOfType<ForbidResult>();
    }

    public static IEnumerable<object[]> Scenarios()
    {
        var d = new DateTime(2020, 6, 1);
        yield return [(Action<VlaEditViewModel>)(c => c.BirthDate = new DateTime(1960, 1, 1)), "Case.BirthDate", "after 31/12/1969"];
        yield return [(Action<VlaEditViewModel>)(c => { c.FormADate = d; c.BirthDate = d; }), "Case.BirthDate", "before the Form A date"];
        yield return [(Action<VlaEditViewModel>)(c => c.BirthDate = DateTime.Today.AddDays(5)), "Case.BirthDate", "a past date"];
        yield return [(Action<VlaEditViewModel>)(c => { c.Origin = "P"; c.BirthDate = new DateTime(2010, 1, 1); c.PurchaseDate = new DateTime(2009, 1, 1); }), "Case.PurchaseDate", "after the birth date"];
        yield return [(Action<VlaEditViewModel>)(c => { c.Origin = "P"; c.FormADate = d; c.PurchaseDate = d; }), "Case.PurchaseDate", "before the Form A date"];
        yield return [(Action<VlaEditViewModel>)(c => { c.Origin = "P"; c.PurchaseDate = DateTime.Today.AddDays(1); }), "Case.PurchaseDate", "a past date"];
        yield return [(Action<VlaEditViewModel>)(c => { c.FormADate = d; c.HerdEntryDate = d.AddDays(1); }), "Case.HerdEntryDate", "before the Form A date"];
        yield return [(Action<VlaEditViewModel>)(c => c.HerdEntryDate = DateTime.Today.AddDays(1)), "Case.HerdEntryDate", "a past date"];
        yield return [(Action<VlaEditViewModel>)(c => { c.BirthDate = new DateTime(2010, 1, 1); c.OnsetDate = new DateTime(2009, 1, 1); }), "Case.OnsetDate", "after the date of birth"];
        yield return [(Action<VlaEditViewModel>)(c => { c.FormADate = d; c.OnsetDate = d.AddDays(1); }), "Case.OnsetDate", "before the Form A date"];
        yield return [(Action<VlaEditViewModel>)(c => c.OnsetDate = DateTime.Today.AddDays(1)), "Case.OnsetDate", "a past date"];
        yield return [(Action<VlaEditViewModel>)(c => { c.MonthsPregnant = 3; c.MonthsPostCalving = 1; }), "Case.MonthsPostCalving", "cannot enter a value"];
        yield return [(Action<VlaEditViewModel>)(c => c.MonthsPregnant = 12), "Case.MonthsPregnant", "between 1 and 9"];
        yield return [(Action<VlaEditViewModel>)(c => c.MonthsPostCalving = 5), "Case.MonthsPostCalving", "between 1 and 3"];
        yield return [(Action<VlaEditViewModel>)(c => c.SlaughterDate = DateTime.Today.AddDays(1)), "Case.SlaughterDate", "not be in the future"];
        yield return [(Action<VlaEditViewModel>)(c => { c.FormADate = d; c.SlaughterDate = d.AddDays(-30); }), "Case.SlaughterDate", "after the Form A date"];
        yield return [(Action<VlaEditViewModel>)(c => { c.BirthDate = new DateTime(2010, 1, 1); c.SlaughterDate = new DateTime(2000, 1, 1); }), "Case.SlaughterDate", "after the birth date"];
    }

    [Theory]
    [MemberData(nameof(Scenarios))]
    public async Task OnPostAsync_ReportsDomainRuleViolations(Action<VlaEditViewModel> arrange, string key, string expectedFragment)
    {
        var model = Create("DataEntry");
        arrange(model.Case);

        var result = await model.OnPostAsync();

        result.Should().BeOfType<PageResult>();
        PageTestHelpers.ErrorMessages(model, key).Should().Contain(m => m.Contains(expectedFragment));
    }

    [Fact]
    public async Task OnPostAsync_WhenOriginIsNotPurchased_ClearsPurchaseFields()
    {
        var model = Create("DataEntry");
        model.Case.Origin = "B";
        model.Case.PurchaseDate = new DateTime(2000, 1, 1);
        model.Case.PurchasedCounty = "X";
        model.Case.PurchaseAgeInMonths = 4;
        model.Case.MonthsPregnant = 3;
        model.Case.MonthsPostCalving = 1;

        await model.OnPostAsync();

        model.Case.PurchaseDate.Should().BeNull();
        model.Case.PurchasedCounty.Should().BeNull();
        model.Case.PurchaseAgeInMonths.Should().BeNull();
    }
}

public sealed class DefraEditValidationTests
{
    private const string Rbse = "002600001";
    private readonly ICaseService _caseService = Substitute.For<ICaseService>();
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly ILookupDataService _lookups = Substitute.For<ILookupDataService>();
    private readonly ICaseWorkRepository _caseWorkRepository = Substitute.For<ICaseWorkRepository>();
    private readonly IBatchRepository _batchRepository = Substitute.For<IBatchRepository>();

    private DefraEditModel Create()
    {
        _lookups.GetLookupAsync(Arg.Any<LookupTableId>()).Returns(Array.Empty<LookupItem>());
        _caseService.GetCaseAsync(Rbse).Returns(new CaseRecord { Rbse = Rbse, Cphh = "12345678901", IsNonGbCase = false, RowStamp = [1] });
        var model = new DefraEditModel(_caseService, _currentUser, _lookups, _caseWorkRepository, _batchRepository,
            new ConfigurationBuilder().AddInMemoryCollection().Build()) { Rbse = Rbse };
        PageTestHelpers.Attach(model);
        return model;
    }

    private static readonly DateTime FormA = new(2020, 6, 1);

    public static IEnumerable<object[]> Scenarios()
    {
        yield return [(Action<CaseEditViewModel>)(_ => { }), "Case.EartagCountry", "Enter an eartag."];
        yield return [(Action<CaseEditViewModel>)(c => { c.EartagCountry = "ZZ"; c.EartagHerdmark = "1"; c.Eartag = "2"; }), "Case.EartagCountry", ""];
        yield return [(Action<CaseEditViewModel>)(_ => { }), "Case.FormADate", "Enter a Form A date."];
        yield return [(Action<CaseEditViewModel>)(c => c.Bse1ReceivedDate = DateTime.Today.AddDays(2)), "Case.Bse1ReceivedDate", "Invalid Date"];
        yield return [(Action<CaseEditViewModel>)(c => { c.FormADate = DateTime.Today.AddDays(2); }), "Case.FormADate", "past date"];
        yield return [(Action<CaseEditViewModel>)(c => { c.FormADate = FormA; c.SlaughterDate = FormA.AddDays(-10); }), "Case.FormADate", "before the slaughter date"];
        yield return [(Action<CaseEditViewModel>)(c => { c.FormADate = FormA; c.FormAResubmittedDate = FormA.AddDays(-5); }), "Case.FormAResubmittedDate", "after the Form A date"];
        yield return [(Action<CaseEditViewModel>)(c => { c.FormADate = FormA; c.FormAResubmittedDate = DateTime.Today.AddDays(3); }), "Case.FormAResubmittedDate", "past date"];
        yield return [(Action<CaseEditViewModel>)(c => { c.FormADate = FormA; c.FormBDate = FormA.AddDays(-5); }), "Case.FormBDate", "after the Form A date"];
        yield return [(Action<CaseEditViewModel>)(c => { c.FormADate = FormA; c.FormBDate = DateTime.Today.AddDays(3); }), "Case.FormBDate", "past date"];
        yield return [(Action<CaseEditViewModel>)(c => { c.FormADate = FormA; c.FormBDate = FormA.AddDays(5); }), "Case.Fate", "required"];
        yield return [(Action<CaseEditViewModel>)(c => { c.FormADate = FormA; c.FormBDate = FormA.AddDays(5); c.Fate = "SL"; c.FormCDate = DateTime.Today.AddDays(2); }), "Case.FormCDate", "Invalid Date"];
        yield return [(Action<CaseEditViewModel>)(c => c.BirthDate = new DateTime(1960, 1, 1)), "Case.BirthDate", "1 January 1970"];
        yield return [(Action<CaseEditViewModel>)(c => { c.FormADate = FormA; c.BirthDate = FormA; }), "Case.BirthDate", "before the Form A date"];
        yield return [(Action<CaseEditViewModel>)(c => c.BirthDate = DateTime.Today.AddDays(2)), "Case.BirthDate", "past date"];
        yield return [(Action<CaseEditViewModel>)(c => { c.FormADate = FormA; c.BirthDate = new DateTime(2010, 1, 1); c.PurchaseDate = new DateTime(2000, 1, 1); }), "Case.BirthDate", "purchase date"];
        yield return [(Action<CaseEditViewModel>)(c => { c.FormADate = FormA; c.BirthDate = new DateTime(2010, 1, 1); c.OnsetDate = new DateTime(2000, 1, 1); }), "Case.BirthDate", "onset date"];
        yield return [(Action<CaseEditViewModel>)(c => { c.HasCaseWork = true; c.RbseDate = FormA; c.PurchaserBse1ReceivedDate = FormA; }), "Case.PurchaserBse1ReceivedDate", "after the RBSE date"];
        yield return [(Action<CaseEditViewModel>)(c => { c.HasCaseWork = true; c.RbseDate = FormA; c.BreederBse1ReceivedDate = DateTime.Today.AddDays(3); }), "Case.BreederBse1ReceivedDate", "past date"];
    }

    [Theory]
    [MemberData(nameof(Scenarios))]
    public async Task OnPostAsync_ReportsDomainRuleViolations(Action<CaseEditViewModel> arrange, string key, string expectedFragment)
    {
        var model = Create();
        arrange(model.Case);

        var result = await model.OnPostAsync();

        result.Should().BeOfType<PageResult>();
        PageTestHelpers.ErrorMessages(model, key).Should().Contain(m => m.Contains(expectedFragment));
    }
}

public sealed class NewNonGbModelTests
{
    private readonly ICaseService _cases = Substitute.For<ICaseService>();
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly IFarmService _farmService = Substitute.For<IFarmService>();
    private readonly ILookupDataService _lookups = Substitute.For<ILookupDataService>();
    private readonly BSE.Modules.ReferenceData.Services.IGeoLookupService _geo = Substitute.For<BSE.Modules.ReferenceData.Services.IGeoLookupService>();
    private readonly IBsessCheckService _bsess = Substitute.For<IBsessCheckService>();

    private NewNonGbModel Create()
    {
        _lookups.GetCaseFatesAsync().Returns(Array.Empty<LuCaseFate>());
        _lookups.GetTestResultsAsync().Returns(Array.Empty<LuTestResult>());
        _geo.GetNonGBCountyAsync().Returns(Array.Empty<LuBseCounty>());
        _currentUser.GetUserIdAsync().Returns(3);
        var model = new NewNonGbModel(_cases, _currentUser, _farmService, _lookups, _geo, _bsess, NullLogger<NewNonGbModel>.Instance)
        {
            Rbse = "630000001"
        };
        PageTestHelpers.Attach(model, "VLAMaintenance");
        return model;
    }

    private static void FillValid(NewNonGbModel m)
    {
        m.Cphh = "00999900101";
        m.EartagCountry = "UK";
        m.EartagHerdmark = "123456";
        m.Eartag = "789012";
        m.Fate = "SL";
        m.FinalResult = "NE";
        m.SlaughterDate = DateTime.Today.AddDays(-10);
        m.FinalResultDate = DateTime.Today.AddDays(-5);
        m.OwnerName = "Owner";
        m.Address1 = "1 Street";
        m.County = "JY";
    }

    [Fact]
    public async Task OnGetAsync_WithoutRbse_RedirectsHome()
    {
        var model = Create();
        model.Rbse = "";

        (await model.OnGetAsync()).Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/Home");
    }

    [Fact]
    public async Task OnGetAsync_DefaultsRbseDateToToday()
    {
        var model = Create();

        await model.OnGetAsync();

        model.RbseDate.Should().Be(DateTime.Today);
    }

    [Fact]
    public async Task OnPostAsync_WithBlankForm_ReportsEveryMandatoryField()
    {
        var model = Create();
        model.Fate = null;
        model.FinalResult = null;

        var result = await model.OnPostAsync();

        result.Should().BeOfType<PageResult>();
        foreach (var key in new[] { "Eartag", "Fate", "FinalResult", "FinalResultDate", "SlaughterDate", "Cphh", "OwnerName", "Address1", "County" })
            model.ModelState.Should().ContainKey(key);
    }

    [Fact]
    public async Task OnPostAsync_ValidatesDateRangesCphhAndNumericHerdmark()
    {
        var model = Create();
        FillValid(model);
        model.EartagCountry = "ZZ";
        model.SlaughterDate = DateTime.Today.AddDays(3);
        model.FinalResultDate = DateTime.Today.AddDays(-30);
        model.Cphh = "01001000701";
        model.NumericHerdmark1 = "12";

        await model.OnPostAsync();

        PageTestHelpers.ErrorMessages(model, "SlaughterDate").Should().Contain("Slaughter Date is later than today.");
        PageTestHelpers.ErrorMessages(model, "FinalResultDate").Should().Contain(m => m.Contains("between the Slaughter Date and today"));
        PageTestHelpers.ErrorMessages(model, "Cphh").Should().Contain("Please enter the CPHH of a non-GB farm.");
        PageTestHelpers.ErrorMessages(model, "NumericHerdmark1").Should().Contain("Numeric herdmark must be 6 digits.");
    }

    [Theory]
    [InlineData("123", "Enter CPHH as 11 digits")]
    [InlineData("", "You need to enter a CPHH.")]
    public async Task OnPostAsync_RejectsBadCphh(string cphh, string expected)
    {
        var model = Create();
        FillValid(model);
        model.Cphh = cphh;

        await model.OnPostAsync();

        PageTestHelpers.ErrorMessages(model, "Cphh").Should().Contain(m => m.Contains(expected));
    }

    [Fact]
    public async Task OnPostAsync_WhenValid_CreatesCaseAndRedirectsToFarm()
    {
        var model = Create();
        FillValid(model);
        _cases.CreateNonGbCaseAsync(Arg.Any<AddNonGbCaseCommand>(), 3).Returns(AddNonGbCaseResult.Success);
        _bsess.GetCheckByRbseAsync("630000001", Arg.Any<CancellationToken>())
            .Returns(new BsessCheckByRbseResult(null, null, null, null, null, "BAR1", null, null, null, null, null));

        var result = await model.OnPostAsync();

        result.Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/Case/Farm");
        await _cases.Received(1).CreateNonGbCaseAsync(Arg.Is<AddNonGbCaseCommand>(c => c.Barcode == "BAR1" && c.Cphh == "00999900101"), 3);
    }

    [Fact]
    public async Task OnPostAsync_WhenBsessLookupFails_StillCreatesCase()
    {
        var model = Create();
        FillValid(model);
        _cases.CreateNonGbCaseAsync(Arg.Any<AddNonGbCaseCommand>(), 3).Returns(AddNonGbCaseResult.Success);
        _bsess.GetCheckByRbseAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns<BsessCheckByRbseResult?>(_ => throw new InvalidOperationException("boom"));

        var result = await model.OnPostAsync();

        result.Should().BeOfType<RedirectToPageResult>();
    }

    [Theory]
    [InlineData(AddNonGbCaseResult.AlreadyExists, "already exists")]
    [InlineData(AddNonGbCaseResult.FarmCreateError, "saving the farm record")]
    [InlineData(AddNonGbCaseResult.FarmUpdateAuditError, "saving the farm record")]
    [InlineData(AddNonGbCaseResult.CaseInsertError, "saving the case record")]
    [InlineData(AddNonGbCaseResult.CaseAuditError, "saving the case record")]
    [InlineData(AddNonGbCaseResult.CaseWorkInsertError, "saving the casework record")]
    public async Task OnPostAsync_MapsFailureResultsToMessages(AddNonGbCaseResult result, string expected)
    {
        var model = Create();
        FillValid(model);
        _cases.CreateNonGbCaseAsync(Arg.Any<AddNonGbCaseCommand>(), 3).Returns(result);

        (await model.OnPostAsync()).Should().BeOfType<PageResult>();

        PageTestHelpers.ErrorMessages(model, string.Empty).Should().Contain(m => m.Contains(expected));
    }

    [Fact]
    public async Task OnPostAsync_WithoutRbse_RedirectsHome()
    {
        var model = Create();
        model.Rbse = "";

        (await model.OnPostAsync()).Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/Home");
    }

    [Fact]
    public async Task OnPostAsync_WithAnUnmappedFailureResult_ShowsTheGenericMessage()
    {
        var model = Create();
        FillValid(model);
        _cases.CreateNonGbCaseAsync(Arg.Any<AddNonGbCaseCommand>(), 3).Returns((AddNonGbCaseResult)999);

        (await model.OnPostAsync()).Should().BeOfType<PageResult>();

        PageTestHelpers.ErrorMessages(model, string.Empty).Should().Contain("Failed to create case (error 999).");
    }

    [Fact]
    public async Task OnGetAsync_ResolvesACountyDescriptionToItsCodeAndEnablesFarmControlsForACphh()
    {
        var model = Create();
        _geo.GetNonGBCountyAsync().Returns(new[] { new LuBseCounty { Code = "JY", Description = "Jersey" } });
        model.County = "Jersey";
        model.FarmControlsEnabled.Should().BeFalse();
        model.Cphh = "00999900101";

        await model.OnGetAsync();

        model.County.Should().Be("JY");
        model.FarmControlsEnabled.Should().BeTrue();
    }

    [Fact]
    public async Task OnPostAsync_WhenCreateThrows_ShowsGenericError()
    {
        var model = Create();
        FillValid(model);
        _cases.CreateNonGbCaseAsync(Arg.Any<AddNonGbCaseCommand>(), 3).Returns<AddNonGbCaseResult>(_ => throw new InvalidOperationException("db"));

        (await model.OnPostAsync()).Should().BeOfType<PageResult>();

        PageTestHelpers.ErrorMessages(model, string.Empty).Should().Contain("Failed to save new case details.");
    }

    [Fact]
    public async Task OnPostLookupAsync_WithExistingFarm_PrefillsFarmDetails()
    {
        var model = Create();
        model.Cphh = "00999900101";
        _farmService.GetByCphhAsync("00999900101").Returns(new BSE.Modules.FarmManagement.Models.FarmRecord
        {
            CPHH = "00999900101", OwnerName = " Owner ", Address1 = "", CorrespondenceAddress1 = " Corr ", Postcode = "AB1", County = "JY"
        });

        var result = await model.OnPostLookupAsync();

        result.Should().BeOfType<PageResult>();
        model.FarmFoundOnLookup.Should().BeTrue();
        model.OwnerName.Should().Be("Owner");
        model.Address1.Should().Be("Corr");
    }

    [Theory]
    [InlineData("", "You need to enter a CPHH.")]
    [InlineData("123", "Enter CPHH as 11 digits")]
    [InlineData("01001000701", "non-GB farm")]
    public async Task OnPostLookupAsync_RejectsInvalidCphh(string cphh, string expected)
    {
        var model = Create();
        model.Cphh = cphh;

        await model.OnPostLookupAsync();

        PageTestHelpers.ErrorMessages(model, "Cphh").Should().Contain(m => m.Contains(expected));
    }

    [Fact]
    public async Task OnPostLookupAsync_WithoutRbse_RedirectsHome()
    {
        var model = Create();
        model.Rbse = "";

        (await model.OnPostLookupAsync()).Should().BeOfType<RedirectToPageResult>();
    }

    [Fact]
    public void OnGetValidateEartag_ReturnsJson()
    {
        Create().OnGetValidateEartag("UK", "123456", "123456").Should().BeOfType<JsonResult>();
    }
}
