using BSE.Host.Pages.Case;
using BSE.Host.Services;
using BSE.Modules.BsessIntegration.Models;
using BSE.Modules.BsessIntegration.Services;
using BSE.Modules.CaseManagement.Commands;
using BSE.Modules.CaseManagement.Enums;
using BSE.Modules.CaseManagement.Services;
using BSE.Modules.FarmManagement.Models;
using BSE.Modules.FarmManagement.Services;
using BSE.Modules.ReferenceData.Models;
using BSE.Modules.ReferenceData.Services;
using BSE.SharedKernel;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Logging;

namespace BSE.Host.Tests;

public class NewNonGbModelTests
{
    private readonly ICaseService _cases = Substitute.For<ICaseService>();
    private readonly ICurrentUserService _user = Substitute.For<ICurrentUserService>();
    private readonly IFarmService _farms = Substitute.For<IFarmService>();
    private readonly ILookupDataService _lookups = Substitute.For<ILookupDataService>();
    private readonly BSE.Modules.ReferenceData.Services.IGeoLookupService _geo = Substitute.For<BSE.Modules.ReferenceData.Services.IGeoLookupService>();
    private readonly IBsessCheckService _bsess = Substitute.For<IBsessCheckService>();
    private readonly ILogger<NewNonGbModel> _logger = Substitute.For<ILogger<NewNonGbModel>>();

    private NewNonGbModel CreateModel()
    {
        _user.GetUserIdAsync().Returns(Task.FromResult(3));
        _lookups.GetCaseFatesAsync().Returns(Task.FromResult<IEnumerable<LuCaseFate>>(
            [new LuCaseFate { Code = "SL", Description = "Slaughter" }, new LuCaseFate { Code = "DIED", Description = "Died" }]));
        _lookups.GetTestResultsAsync().Returns(Task.FromResult<IEnumerable<LuTestResult>>(
            [new LuTestResult { Code = "NE", Description = "Not examined" }]));
        _geo.GetNonGBCountyAsync().Returns(Task.FromResult<IEnumerable<LuBSECounty>>(
            [new LuBSECounty { Code = "DUB", Description = "Dublin" }]));
        _cases.CreateNonGbCaseAsync(Arg.Any<AddNonGbCaseCommand>(), Arg.Any<int>())
            .Returns(Task.FromResult(AddNonGbCaseResult.Success));

        var http = new DefaultHttpContext();
        return new NewNonGbModel(_cases, _user, _farms, _lookups, _geo, _bsess, _logger)
        {
            Rbse = " 002600001 ",
            PageContext = new PageContext { HttpContext = http },
            TempData = new TempDataDictionary(http, Substitute.For<ITempDataProvider>())
        };
    }

    private NewNonGbModel ValidModel()
    {
        var m = CreateModel();
        m.Cphh = "00/999/9001/23";
        m.EartagCountry = "UK"; m.EartagHerdmark = "MN123"; m.Eartag = "1";
        m.SlaughterDate = DateTime.Today.AddDays(-10);
        m.FinalResultDate = DateTime.Today.AddDays(-5);
        m.OwnerName = "Owner";
        m.Address1 = "1 Street";
        m.County = "DUB";
        return m;
    }

    [Fact]
    public async Task OnGet_BlankRbse_RedirectsHomeWithError()
    {
        var model = CreateModel();
        model.Rbse = "";

        var result = await model.OnGetAsync();

        result.Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/Home");
        model.TempData["ErrorMessage"].Should().NotBeNull();
    }

    [Fact]
    public async Task OnGet_LoadsLookupsAndDefaultsRbseDate()
    {
        var model = CreateModel();

        var result = await model.OnGetAsync();

        result.Should().BeOfType<PageResult>();
        model.RbseDate.Should().Be(DateTime.Today);
        model.FateOptions.Should().HaveCount(2);
        model.FinalResultOptions.Should().HaveCount(1);
        model.CountyOptions.Should().HaveCount(1);
    }

    [Fact]
    public async Task OnGet_UnknownFateAndResult_ResetToDefaults_AndCountyDescriptionMappedToCode()
    {
        var model = CreateModel();
        model.Fate = "ZZ";
        model.FinalResult = "ZZ";
        model.County = "dublin";

        await model.OnGetAsync();

        model.Fate.Should().Be("SL");
        model.FinalResult.Should().Be("NE");
        model.County.Should().Be("DUB");
    }

    [Fact]
    public void OnGetValidateEartag_ReturnsJson()
    {
        var model = CreateModel();

        var result = model.OnGetValidateEartag(null, null, null);

        result.Should().BeOfType<JsonResult>();
    }

    [Fact]
    public async Task OnPost_BlankRbse_RedirectsHome()
    {
        var model = CreateModel();
        model.Rbse = " ";

        (await model.OnPostAsync()).Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/Home");
    }

    [Fact]
    public async Task OnPost_EmptyForm_AddsAllRequiredErrors()
    {
        var model = CreateModel();
        model.Fate = null;
        model.FinalResult = "";

        var result = await model.OnPostAsync();

        result.Should().BeOfType<PageResult>();
        model.ModelState.Keys.Should().Contain(["Eartag", "Fate", "FinalResult", "FinalResultDate", "SlaughterDate",
            "Cphh", "OwnerName", "Address1", "County"]);
        await _cases.DidNotReceive().CreateNonGbCaseAsync(Arg.Any<AddNonGbCaseCommand>(), Arg.Any<int>());
        model.FateOptions.Should().NotBeEmpty();
    }

    [Fact]
    public async Task OnPost_InvalidEartag_AddsError()
    {
        var model = ValidModel();
        model.Eartag = "!!";
        model.EartagHerdmark = "!!";
        model.EartagCountry = "ZZ";

        (await model.OnPostAsync()).Should().BeOfType<PageResult>();
        model.ModelState.Should().ContainKey("Eartag");
    }

    [Fact]
    public async Task OnPost_DateRules_AddErrors()
    {
        var model = ValidModel();
        model.SlaughterDate = DateTime.Today.AddDays(1);
        model.FinalResultDate = DateTime.Today.AddDays(-3);

        await model.OnPostAsync();

        model.ModelState["SlaughterDate"]!.Errors[0].ErrorMessage.Should().Be("Slaughter Date is later than today.");
        model.ModelState["FinalResultDate"]!.Errors[0].ErrorMessage.Should().Contain("between the Slaughter Date and today");
    }

    [Theory]
    [InlineData("00/9999/0001", "Enter CPHH as 11 digits in the format NN/NNN/NNNN/NN.")]
    [InlineData("01/234/5678/90", "Please enter the CPHH of a non-GB farm.")]
    public async Task OnPost_BadCphh_AddsError(string cphh, string message)
    {
        var model = ValidModel();
        model.Cphh = cphh;

        await model.OnPostAsync();

        model.ModelState["Cphh"]!.Errors[0].ErrorMessage.Should().Be(message);
    }

    [Fact]
    public async Task OnPost_BadNumericHerdmark_AddsError()
    {
        var model = ValidModel();
        model.NumericHerdmark1 = "12ab";

        await model.OnPostAsync();

        model.ModelState.Should().ContainKey("NumericHerdmark1");
    }

    [Fact]
    public async Task OnPost_Valid_CreatesCaseAndRedirectsToFarm()
    {
        var model = ValidModel();
        model.Barcode = "BC";
        model.AhfReference = "AHF";

        var result = await model.OnPostAsync();

        var redirect = result.Should().BeOfType<RedirectToPageResult>().Subject;
        redirect.PageName.Should().Be("/Case/Farm");
        redirect.RouteValues!["rbse"].Should().Be("002600001");
        model.TempData["SuccessMessage"].Should().Be("Non-GB case  002600001  created successfully.");
        await _cases.Received(1).CreateNonGbCaseAsync(Arg.Is<AddNonGbCaseCommand>(c =>
            c.Rbse == "002600001" && c.Cphh == "00999900123" && c.Barcode == "BC" && c.County == "DUB"), 3);
        await _bsess.DidNotReceive().GetCheckByRbseAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task OnPost_NoBarcode_UsesBsessBarcode()
    {
        var bsess = ((BsessCheckByRbseResult)System.Runtime.CompilerServices.RuntimeHelpers
            .GetUninitializedObject(typeof(BsessCheckByRbseResult))) with { Barcode = "FROMBSESS" };
        var model = ValidModel();
        _bsess.GetCheckByRbseAsync("002600001", Arg.Any<CancellationToken>()).Returns(Task.FromResult<BsessCheckByRbseResult?>(bsess));

        await model.OnPostAsync();

        await _cases.Received(1).CreateNonGbCaseAsync(Arg.Is<AddNonGbCaseCommand>(c => c.Barcode == "FROMBSESS"), Arg.Any<int>());
    }

    [Fact]
    public async Task OnPost_BsessThrows_StillCreatesCase()
    {
        var model = ValidModel();
        _bsess.GetCheckByRbseAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns<Task<BsessCheckByRbseResult?>>(_ => throw new InvalidOperationException("down"));

        var result = await model.OnPostAsync();

        result.Should().BeOfType<RedirectToPageResult>();
        await _cases.Received(1).CreateNonGbCaseAsync(Arg.Is<AddNonGbCaseCommand>(c => c.Barcode == null), Arg.Any<int>());
    }

    [Fact]
    public async Task OnPost_ServiceThrows_AddsGenericError()
    {
        var model = ValidModel();
        _cases.CreateNonGbCaseAsync(Arg.Any<AddNonGbCaseCommand>(), Arg.Any<int>())
            .Returns<Task<AddNonGbCaseResult>>(_ => throw new InvalidOperationException("db"));

        var result = await model.OnPostAsync();

        result.Should().BeOfType<PageResult>();
        model.ModelState[string.Empty]!.Errors[0].ErrorMessage.Should().Be("Failed to save new case details.");
    }

    [Theory]
    [InlineData(AddNonGbCaseResult.AlreadyExists, "Case ' 002600001 ' already exists.")]
    [InlineData(AddNonGbCaseResult.FarmCreateError, "An error occurred saving the farm record.")]
    [InlineData(AddNonGbCaseResult.FarmUpdateAuditError, "An error occurred saving the farm record.")]
    [InlineData(AddNonGbCaseResult.CaseInsertError, "An error occurred saving the case record.")]
    [InlineData(AddNonGbCaseResult.CaseAuditError, "An error occurred saving the case record.")]
    [InlineData(AddNonGbCaseResult.CaseWorkInsertError, "An error occurred saving the casework record.")]
    [InlineData((AddNonGbCaseResult)99, "Failed to create case (error 99).")]
    public async Task OnPost_FailureResult_AddsMappedMessage(AddNonGbCaseResult code, string message)
    {
        var model = ValidModel();
        _cases.CreateNonGbCaseAsync(Arg.Any<AddNonGbCaseCommand>(), Arg.Any<int>()).Returns(Task.FromResult(code));

        var result = await model.OnPostAsync();

        result.Should().BeOfType<PageResult>();
        model.ModelState[string.Empty]!.Errors[0].ErrorMessage.Should().Be(message);
    }

    [Fact]
    public async Task Lookup_BlankRbse_RedirectsHome()
    {
        var model = CreateModel();
        model.Rbse = "";

        (await model.OnPostLookupAsync()).Should().BeOfType<RedirectToPageResult>();
    }

    [Theory]
    [InlineData("", "You need to enter a CPHH.")]
    [InlineData("00/9999/01", "Enter CPHH as 11 digits in the format NN/NNN/NNNN/NN.")]
    [InlineData("01/234/5678/90", "Please enter the CPHH of a non-GB farm.")]
    public async Task Lookup_InvalidCphh_AddsError(string cphh, string message)
    {
        var model = CreateModel();
        model.Cphh = cphh;

        var result = await model.OnPostLookupAsync();

        result.Should().BeOfType<PageResult>();
        model.ModelState["Cphh"]!.Errors[0].ErrorMessage.Should().Be(message);
        await _farms.DidNotReceive().GetByCphhAsync(Arg.Any<string>());
    }

    [Fact]
    public async Task Lookup_FarmFound_PrefillsFieldsWithCorrespondenceFallback()
    {
        _farms.GetByCphhAsync("00999900123").Returns(Task.FromResult<FarmRecord?>(new FarmRecord
        {
            OwnerName = " Farmer ",
            Address1 = " ",
            CorrespondenceAddress1 = " Corr1 ",
            Address2 = "A2",
            Postcode = "PC",
            County = " DUB ",
            Herdmark1 = " HM ",
            NumericHerdmark1 = " 123456 "
        }));
        var model = CreateModel();
        model.Cphh = "00/9999/0012/3";
        model.ModelState.AddModelError("x", "stale");

        var result = await model.OnPostLookupAsync();

        result.Should().BeOfType<PageResult>();
        model.FarmFoundOnLookup.Should().BeTrue();
        model.OwnerName.Should().Be("Farmer");
        model.Address1.Should().Be("Corr1");
        model.Address2.Should().Be("A2");
        model.County.Should().Be("DUB");
        model.NumericHerdmark1.Should().Be("123456");
        model.ModelState.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Lookup_FarmNotFound_FlagsFalse()
    {
        _farms.GetByCphhAsync(Arg.Any<string>()).Returns(Task.FromResult<FarmRecord?>(null));
        var model = CreateModel();
        model.Cphh = "00999900123";

        await model.OnPostLookupAsync();

        model.FarmFoundOnLookup.Should().BeFalse();
        model.FarmControlsEnabled.Should().BeTrue();
    }
}
