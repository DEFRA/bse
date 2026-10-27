using System.Runtime.CompilerServices;
using BSE.Host.Pages.Case;
using BSE.Host.Services;
using BSE.Modules.Batch.Models;
using BSE.Modules.Batch.Services;
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

namespace BSE.Host.Tests;

public class NewModelTests
{
    private readonly ICaseService _cases = Substitute.For<ICaseService>();
    private readonly IBatchService _batch = Substitute.For<IBatchService>();
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly IFarmService _farms = Substitute.For<IFarmService>();
    private readonly ILookupDataService _lookups = Substitute.For<ILookupDataService>();

    private NewModel CreateModel()
    {
        _currentUser.GetUserIdAsync().Returns(Task.FromResult(7));
        _lookups.GetLookupAsync(Arg.Any<LookupTableId>())
            .Returns(Task.FromResult<IEnumerable<LookupItem>>([]));
        _lookups.GetADNSRegionsAsync()
            .Returns(Task.FromResult<IEnumerable<LuADNSRegion>>([]));
        _batch.GetOrCreateBatchNumberAsync().Returns(Task.FromResult(new BatchRecord(42, 2026, 1)));
        _cases.CreateCaseAsync(Arg.Any<UpdateCaseDetailsCommand>(), Arg.Any<int>())
            .Returns(Task.FromResult(AddCaseResult.Success));

        return new NewModel(_cases, _batch, _currentUser, _farms, _lookups)
        {
            TempData = new TempDataDictionary(new DefaultHttpContext(), Substitute.For<ITempDataProvider>())
        };
    }

    private static NewModel WithValidCaseFields(NewModel model)
    {
        model.Rbse = " 002600001 ";
        model.Cphh = "01/234/5678/90";
        model.Eartag = "AB123-234";
        model.FormADate = new DateTime(2026, 1, 15);
        return model;
    }

    private void FarmExists() =>
        _farms.GetByCphhAsync("01234567890")
            .Returns(Task.FromResult<FarmRecord?>((FarmRecord)RuntimeHelpers.GetUninitializedObject(typeof(FarmRecord))));

    private void FarmMissing() =>
        _farms.GetByCphhAsync(Arg.Any<string>()).Returns(Task.FromResult<FarmRecord?>(null));

    [Fact]
    public async Task OnPostAsync_MissingRequiredFields_ReturnsPageWithErrorsAndDoesNotSave()
    {
        var model = CreateModel();
        FarmMissing();

        var result = await model.OnPostAsync();

        result.Should().BeOfType<PageResult>();
        model.ModelState.Keys.Should().Contain(["Rbse", "Cphh", "Eartag", "FormADate"]);
        await _cases.DidNotReceive().CreateCaseAsync(Arg.Any<UpdateCaseDetailsCommand>(), Arg.Any<int>());
        await _lookups.Received().GetADNSRegionsAsync();
    }

    [Theory]
    [InlineData("0123")]
    [InlineData("01/234/5678/901")]
    public async Task OnPostAsync_CphhNotElevenDigits_AddsCphhError(string cphh)
    {
        var model = WithValidCaseFields(CreateModel());
        model.Cphh = cphh;
        FarmMissing();

        await model.OnPostAsync();

        model.ModelState["Cphh"]!.Errors.Should().ContainSingle(e => e.ErrorMessage.Contains("11 digits"));
    }

    [Fact]
    public async Task OnPostAsync_NormalisesCphhToDigitsOnly()
    {
        var model = WithValidCaseFields(CreateModel());
        FarmExists();

        await model.OnPostAsync();

        model.Cphh.Should().Be("01234567890");
    }

    [Fact]
    public async Task OnPostAsync_NewFarmWithoutFarmDetails_RequiresAllFarmFields()
    {
        var model = WithValidCaseFields(CreateModel());
        FarmMissing();

        var result = await model.OnPostAsync();

        result.Should().BeOfType<PageResult>();
        model.RequireFarmDetails.Should().BeTrue();
        model.ModelState.Keys.Should().Contain(["OwnerName", "Address1", "Parish", "County", "Aho", "AdnsRegionId"]);
        await _farms.DidNotReceive().AddAsync(Arg.Any<AddFarmCommand>(), Arg.Any<int>());
    }

    [Fact]
    public async Task OnPostAsync_NewFarmWithDetails_AddsFarmThenCase()
    {
        var model = WithValidCaseFields(CreateModel());
        model.OwnerName = "Owner";
        model.Address1 = "1 High St";
        model.Parish = "Parish";
        model.County = "Devon";
        model.Aho = "AHO1";
        model.AdnsRegionId = 5;
        FarmMissing();

        var result = await model.OnPostAsync();

        result.Should().BeOfType<RedirectToPageResult>();
        await _farms.Received(1).AddAsync(
            Arg.Is<AddFarmCommand>(f => f.CPHH == "01234567890" && f.OwnerName == "Owner" && f.ADNSRegionID == 5),
            7);
        await _cases.Received(1).CreateCaseAsync(Arg.Any<UpdateCaseDetailsCommand>(), 7);
    }

    [Fact]
    public async Task OnPostAsync_ExistingFarm_DoesNotAddFarmAndRedirectsToCaseFarmPage()
    {
        var model = WithValidCaseFields(CreateModel());
        FarmExists();

        var result = await model.OnPostAsync();

        var redirect = result.Should().BeOfType<RedirectToPageResult>().Subject;
        redirect.PageName.Should().Be("/Case/Farm");
        redirect.RouteValues!["rbse"].Should().Be("002600001");
        model.RequireFarmDetails.Should().BeFalse();
        await _farms.DidNotReceive().AddAsync(Arg.Any<AddFarmCommand>(), Arg.Any<int>());
    }

    [Fact]
    public async Task OnPostAsync_NoBatchSupplied_CreatesBatchAndUsesItsId()
    {
        var model = WithValidCaseFields(CreateModel());
        FarmExists();

        await model.OnPostAsync();

        await _cases.Received(1).CreateCaseAsync(
            Arg.Is<UpdateCaseDetailsCommand>(c => c.BatchId == 42 && c.Case.Rbse == "002600001"), 7);
    }

    [Fact]
    public async Task OnPostAsync_SuppliedBatchExists_UsesExistingBatchId()
    {
        var model = WithValidCaseFields(CreateModel());
        model.BatchYear = 2026;
        model.BatchNumber = 3;
        FarmExists();
        _batch.GetBatchIdAsync(2026, 3).Returns(Task.FromResult<int?>(99));

        await model.OnPostAsync();

        await _batch.DidNotReceive().GetOrCreateBatchNumberAsync();
        await _cases.Received(1).CreateCaseAsync(Arg.Is<UpdateCaseDetailsCommand>(c => c.BatchId == 99), 7);
    }

    [Fact]
    public async Task OnPostAsync_SuppliedBatchMissing_ReturnsPageWithErrorAndDoesNotSave()
    {
        var model = WithValidCaseFields(CreateModel());
        model.BatchYear = 2026;
        model.BatchNumber = 3;
        FarmExists();
        _batch.GetBatchIdAsync(2026, 3).Returns(Task.FromResult<int?>(null));

        var result = await model.OnPostAsync();

        result.Should().BeOfType<PageResult>();
        model.ModelState[string.Empty]!.Errors.Should().ContainSingle(e => e.ErrorMessage.Contains("was not found"));
        await _cases.DidNotReceive().CreateCaseAsync(Arg.Any<UpdateCaseDetailsCommand>(), Arg.Any<int>());
    }

    [Theory]
    [InlineData(AddCaseResult.DuplicateRbse, "Case '002600001' already exists.")]
    [InlineData(AddCaseResult.InsertError, "Database error during insert.")]
    [InlineData(AddCaseResult.AuditLogError, "Audit log error during create.")]
    public async Task OnPostAsync_CreateFails_ShowsMatchingMessage(AddCaseResult failure, string expected)
    {
        var model = WithValidCaseFields(CreateModel());
        model.Rbse = "002600001";
        FarmExists();
        _cases.CreateCaseAsync(Arg.Any<UpdateCaseDetailsCommand>(), Arg.Any<int>())
            .Returns(Task.FromResult(failure));

        var result = await model.OnPostAsync();

        result.Should().BeOfType<PageResult>();
        model.ModelState[string.Empty]!.Errors.Should().ContainSingle(e => e.ErrorMessage == expected);
    }

    [Fact]
    public async Task OnPostAsync_CreateSucceeds_SetsSuccessMessage()
    {
        var model = WithValidCaseFields(CreateModel());
        FarmExists();

        await model.OnPostAsync();

        model.TempData["SuccessMessage"].Should().Be("Case  002600001  created successfully.");
    }
}
