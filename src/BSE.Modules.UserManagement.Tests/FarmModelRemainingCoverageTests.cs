using System.Security.Claims;
using BSE.Host.Models.ViewModels;
using BSE.Host.Pages.Case;
using BSE.Host.Services;
using BSE.Modules.Batch.Models;
using BSE.Modules.Batch.Repositories;
using BSE.Modules.CaseManagement.Models;
using BSE.Modules.CaseManagement.Services;
using BSE.Modules.FarmManagement.Models;
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
using IGeoLookupService = BSE.Host.Services.IGeoLookupService;

namespace BSE.Modules.UserManagement.Tests;

/// <summary>Covers the remaining role guards, lookup-name resolution, grid helpers and
/// validation branches of <see cref="FarmModel"/> (Case/Farm).</summary>
public sealed class FarmModelRemainingCoverageTests
{
    private const string Rbse = "002600001";
    private const string Cphh = "01001000101";
    private const string Other = "01001000202";
    private const string Linked = "01001000303";

    private readonly ICaseService _cases = Substitute.For<ICaseService>();
    private readonly IFarmService _farms = Substitute.For<IFarmService>();
    private readonly ILookupDataService _lookups = Substitute.For<ILookupDataService>();
    private readonly IBatchRepository _batches = Substitute.For<IBatchRepository>();
    private readonly ICaseWizardStateService _wizard = Substitute.For<ICaseWizardStateService>();
    private readonly InMemorySessionState _session = new();
    private readonly ICaseEditOrchestrationService _orchestration = Substitute.For<ICaseEditOrchestrationService>();
    private readonly ICurrentUserService _user = Substitute.For<ICurrentUserService>();
    private readonly IGeoLookupService _geo = Substitute.For<IGeoLookupService>();

    public FarmModelRemainingCoverageTests()
    {
        _lookups.GetLookupAsync(Arg.Any<LookupTableId>()).Returns(Array.Empty<LookupItem>());
        _lookups.GetHerdTypesAsync().Returns(Array.Empty<LuHerdType>());
        _lookups.GetAuthoritiesByCountyAsync(Arg.Any<int>()).Returns(Array.Empty<LuAuthority>());
        _lookups.GetADNSRegionsByAuthorityAsync(Arg.Any<int>()).Returns(Array.Empty<LuAdnsRegion>());
        _batches.GetBatchNumbersByRbseAsync(Arg.Any<string>()).Returns(Array.Empty<BatchNumberEntry>());
        _wizard.GetAsync().Returns((CaseWizardState?)null);
        _farms.GetRelatedFarmsAsync(Arg.Any<string>()).Returns(Array.Empty<FarmRelationRecord>());
        _farms.GetHerdSizesAsync(Arg.Any<string>()).Returns(Array.Empty<HerdSizeRecord>());
        _cases.GetCaseAsync(Rbse).Returns((CaseRecord?)null);
        _orchestration.CommitAllAsync(Rbse, 4).Returns(CaseCommitOutcome.Success([]));
        _user.GetUserIdAsync().Returns(4);
    }

    private FarmModel Create(params string[] roles)
    {
        var model = new FarmModel(_cases, _farms, _lookups, _batches, _wizard, _session,
            Substitute.For<ICaseDraftDiscardService>(), _orchestration, _user,
            NullLogger<FarmModel>.Instance, new ConfigurationBuilder().Build(), _geo)
        { Rbse = Rbse };
        var http = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(roles.Select(r => new Claim(ClaimTypes.Role, r)), "TestAuth"))
        };
        model.PageContext = new PageContext { HttpContext = http };
        model.PageContext.ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), model.ModelState);
        model.TempData = new TempDataDictionary(http, Substitute.For<ITempDataProvider>());
        return model;
    }

    private static FarmRecord MakeFarm(string cphh = Cphh) => new()
    {
        CPHH = cphh, OwnerName = "Owner", Address1 = "1 Lane", Parish = "P", County = "TC", AHO = "TA", ADNSRegionID = 1, RowStamp = [1, 2, 3]
    };

    private void GivenExistingCase(FarmRecord? farm = null)
    {
        _cases.GetCaseAsync(Rbse).Returns(new CaseRecord { Rbse = Rbse, Cphh = Cphh });
        _farms.GetByCphhAsync(Cphh).Returns(farm ?? MakeFarm());
    }

    private static FarmEditViewModel Posted(string cphh = Cphh) => new() { CPHH = cphh, OwnerName = "Owner", Address1 = "1 Lane", Parish = "P", County = "TC", AHO = "TA", ADNSRegionID = 1 };

    [Fact]
    public void NewFarmFormFields_RoundTrip()
    {
        var model = Create("DataEntry");

        model.NewOwnerName = "Owner";
        model.NewAddress1 = "A1";
        model.NewAddress2 = "A2";
        model.NewAddress3 = "A3";
        model.NewPostcode = "PC";
        model.NewParish = "Parish";
        model.NewCounty = "County";
        model.NewAho = "AHO";
        model.NewAdnsRegionId = 5;

        (model.NewOwnerName, model.NewAddress1, model.NewAddress2, model.NewAddress3, model.NewPostcode,
            model.NewParish, model.NewCounty, model.NewAho, model.NewAdnsRegionId)
            .Should().Be(("Owner", "A1", "A2", "A3", "PC", "Parish", "County", "AHO", 5));
    }

    [Fact]
    public async Task OnGet_ExistingCase_ResolvesEveryLookupName()
    {
        GivenExistingCase(new FarmRecord
        {
            CPHH = Cphh, OwnerName = "Owner", Address1 = "1 Lane", Parish = "P", County = "TC", AHO = "TA", ADNSRegionID = 1,
            HerdType = "H", PedigreeType = "P", AuthorityCountyID = 3, AuthorityID = 4, RowStamp = [1]
        });
        _lookups.GetHerdTypesAsync().Returns(new[] { new LuHerdType { Code = "H", Description = "Beef herd" } });
        _lookups.GetLookupAsync(LookupTableId.PedigreeType).Returns(new[] { new LookupItem(1, "P", "Pedigree") });
        _lookups.GetLookupAsync(LookupTableId.AuthorityCounty).Returns(new[] { new LookupItem(3, "AC", "Auth county") });
        _lookups.GetAuthoritiesByCountyAsync(3).Returns(new[] { new LuAuthority { Id = 4, Name = "Local authority" } });
        var model = Create("DataEntry");

        await model.OnGetAsync();

        model.HerdTypeName.Should().Be("Beef herd");
        model.PedigreeTypeName.Should().Be("Pedigree");
        model.AuthorityCountyName.Should().Be("Auth county");
        model.LocalAuthorityName.Should().Be("Local authority");
    }

    [Fact]
    public void RoleFlags_ReflectTheUsersRoles()
    {
        Create().IsConfidentialViewOnly.Should().BeTrue();
        Create("DataEntry").IsConfidentialViewOnly.Should().BeFalse();
        Create("DataEntry").CanChangeCphh.Should().BeFalse();
        Create("DEFRAMaintenance").CanChangeCphh.Should().BeTrue();
        Create("VLAMaintenance").CanChangeCphh.Should().BeFalse();
    }

    [Fact]
    public async Task PendingBatch_IsOfferedToVlaUsersAndDetectedWhenAlreadyLinked()
    {
        GivenExistingCase();
        _wizard.GetAsync(Arg.Any<CancellationToken>()).Returns(new CaseWizardState(Rbse, "B9", 9));
        _batches.GetBatchNumbersByRbseAsync(Rbse).Returns(new[] { new BatchNumberEntry(9, "B9", Rbse, "BSE1") });

        var vla = Create("DataEntry", "VLAAccess");
        await vla.OnGetAsync();
        vla.ShowBatchAssignment.Should().BeTrue();
        vla.AlreadyInPendingBatch.Should().BeTrue();

        var defra = Create("DataEntry");
        await defra.OnGetAsync();
        defra.ShowBatchAssignment.Should().BeFalse();
    }

    [Fact]
    public async Task LinkedFarmsGrid_SortsByStatusAndBuildsPagingHelpers()
    {
        GivenExistingCase();
        _farms.GetRelatedFarmsAsync(Cphh).Returns(new[]
        {
            new FarmRelationRecord { ID = 1, CPHH = Cphh, RelatedCPHH = Linked, Status = "B", RowStamp = [1] },
            new FarmRelationRecord { ID = 2, CPHH = Cphh, RelatedCPHH = Other, Status = "A", RowStamp = [2] }
        });
        var model = Create("DataEntry");
        model.LSort = "status";

        await model.OnGetAsync();

        model.LinkedFarmsTotalCount.Should().Be(2);
        model.LinkedFarmsTotalPages.Should().Be(1);
        model.LinkedFarmsCurrentPage.Should().Be(1);
        model.PagedStagedLinkedFarms().Should().HaveCount(2);
        model.HerdSizesTotalCount.Should().Be(0);
        model.LinkedFarmsSortUrl("status").Should().Contain("LSort=status&LDir=desc");
        model.LinkedFarmsSortUrl("cphh").Should().Contain("LDir=asc");
        model.LinkedFarmsPageUrl(2).Should().StartWith("?LPage=2");
        model.HerdSizeSortUrl("year").Should().Contain("HSort=year&HDir=desc");
        model.HerdSizeSortUrl("total").Should().Contain("HDir=asc");
        model.HerdSizesPageUrl(3).Should().StartWith("?HPage=3");
    }

    [Fact]
    public void AriaSortHelpers_ReportTheSortStateOfEachColumn()
    {
        var model = Create();
        model.LSort = "cphh";
        model.HSort = "year";

        model.LinkedFarmAriaSort("status").Should().Be("none");
        model.LinkedFarmAriaSort("cphh").Should().Be("ascending");
        model.HerdSizeAriaSort("total").Should().Be("none");
        model.HerdSizeAriaSort("year").Should().Be("ascending");

        model.LDir = "desc";
        model.HDir = "desc";

        model.LinkedFarmAriaSort("cphh").Should().Be("descending");
        model.HerdSizeAriaSort("year").Should().Be("descending");
    }

    [Fact]
    public void StagedHerdSizeItem_WarnsOnLactationMismatchAndFlagsUnsavedRows()
    {
        var mismatch = new FarmModel.StagedHerdSizeItem { HerdYear = 2020, TotalSize = 10, Lactation1Size = 3, Id = null };
        var matching = new FarmModel.StagedHerdSizeItem { HerdYear = 2020, TotalSize = 3, Lactation1Size = 3, Id = 7 };

        mismatch.LactationMismatchWarning.Should().Contain("lactation total (3)");
        mismatch.IsUnsaved.Should().BeTrue();
        matching.LactationMismatchWarning.Should().BeNull();
        matching.IsUnsaved.Should().BeFalse();
    }

    [Fact]
    public async Task AjaxHandlers_ReturnTheNeutralAnswerForUnusableInput()
    {
        var model = Create("DataEntry");

        var tooLong = (JsonResult)await model.OnGetLinkedFarmStatusAsync("123456789012");
        tooLong.Value!.ToString().Should().Contain("status = ");

        var shortCphh = (JsonResult)await model.OnGetValidateMapReferenceAsync("0100", "SO12345678");
        shortCphh.Value!.ToString().Should().Contain("True");

        var shortMapRef = (JsonResult)await model.OnGetValidateMapReferenceAsync(Cphh, "SO12");
        shortMapRef.Value!.ToString().Should().Contain("True");
    }

    [Fact]
    public async Task HerdSizeHandlers_WithoutDataEntry_AreForbidden()
    {
        var model = Create("VLAAccess");

        (await model.OnPostBeginEditHerdRowAsync()).Should().BeOfType<ForbidResult>();
        (await model.OnPostAddHerdSizeRowAsync()).Should().BeOfType<ForbidResult>();
        (await model.OnPostUpdateHerdSizeRowAsync()).Should().BeOfType<ForbidResult>();
        (await model.OnPostDeleteHerdSizeAsync("k")).Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task HerdSizeHandlers_VlaDataEntry_CannotEditVlaControls_AreRedirected()
    {
        GivenExistingCase();
        var model = Create("DataEntry", "VLAAccess");

        (await model.OnPostBeginEditHerdRowAsync()).Should().BeOfType<RedirectToPageResult>();
        (await model.OnPostAddHerdSizeRowAsync()).Should().BeOfType<RedirectToPageResult>();
        (await model.OnPostUpdateHerdSizeRowAsync()).Should().BeOfType<RedirectToPageResult>();
        (await model.OnPostDeleteHerdSizeAsync("k")).Should().BeOfType<RedirectToPageResult>();
    }

    [Fact]
    public async Task UpdateHerdSizeRow_ReportsAMissingRowAndRejectsBlankValues()
    {
        GivenExistingCase();
        _farms.GetHerdSizesAsync(Cphh).Returns(new[] { new HerdSizeRecord { ID = 1, CPHH = Cphh, HerdYear = 2020, TotalSize = 10, RowStamp = [1] } });
        _batches.GetBatchNumbersByRbseAsync(Rbse).Returns(new[] { new BatchNumberEntry(1, "B1", Rbse, "BSE1") });
        await Create("DataEntry", "VLAAccess").OnGetAsync();
        var key = _session.Current!.HerdSizes.Single().ClientKey;

        var missing = Create("DataEntry", "VLAAccess");
        missing.EditingClientKey = "nope";
        (await missing.OnPostUpdateHerdSizeRowAsync()).Should().BeOfType<RedirectToPageResult>();
        missing.TempData["ErrorMessage"].Should().NotBeNull();

        var blank = Create("DataEntry", "VLAAccess");
        blank.EditingClientKey = key;
        blank.EditHerdRow = new FarmModel.HerdSizeRowInput { HerdYear = null, TotalSize = null };
        (await blank.OnPostUpdateHerdSizeRowAsync()).Should().BeOfType<PageResult>();
        blank.ReopenEditClientKey.Should().Be(key);
        blank.ModelState.Keys.Should().Contain([$"{nameof(FarmModel.EditHerdRow)}.HerdYear", $"{nameof(FarmModel.EditHerdRow)}.TotalSize"]);
    }

    [Fact]
    public async Task UpdateLinkedFarmRow_ToACphhAlreadyInTheList_IsRejected()
    {
        GivenExistingCase();
        _farms.GetRelatedFarmsAsync(Cphh).Returns(new[]
        {
            new FarmRelationRecord { ID = 1, CPHH = Cphh, RelatedCPHH = Linked, RowStamp = [1] },
            new FarmRelationRecord { ID = 2, CPHH = Cphh, RelatedCPHH = Other, RowStamp = [2] }
        });
        await Create("DataEntry").OnGetAsync();
        var key = _session.Current!.LinkedFarms.Single(l => l.RelatedCphh == Other).ClientKey;

        var model = Create("DataEntry");
        model.EditingLinkedClientKey = key;
        model.EditLinkedCphh = Linked;

        (await model.OnPostUpdateLinkedFarmRowAsync()).Should().BeOfType<PageResult>();

        model.ModelState[nameof(FarmModel.EditLinkedCphh)]!.Errors.Should().Contain(e => e.ErrorMessage.Contains("already exists"));
    }

    [Fact]
    public async Task SaveFarm_WithoutAnEditableFarm_ReturnsThePage()
    {
        GivenExistingCase();
        var model = Create("DataEntry");
        model.EditableFarm = null;

        (await model.OnPostSaveFarmAsync()).Should().BeOfType<PageResult>();
    }

    [Fact]
    public async Task SaveFarm_WithInvalidStagedCollections_ListsEveryProblem()
    {
        GivenExistingCase();
        var model = Create("DataEntry");
        model.EditableFarm = Posted();
        _session.Current = new CaseEditSessionState
        {
            Rbse = Rbse,
            Cphh = Cphh,
            HasFarmCollectionsChanges = true,
            LinkedFarms =
            [
                new() { RelatedCphh = "" },
                new() { RelatedCphh = "123" },
                new() { RelatedCphh = Cphh },
                new() { RelatedCphh = Linked },
                new() { RelatedCphh = Linked }
            ],
            HerdSizes = [new() { ClientKey = "h1", HerdYear = 1900, TotalSize = -5, Lactation1Size = 5000 }]
        };

        (await model.OnPostSaveFarmAsync()).Should().BeOfType<PageResult>();

        var messages = model.ModelState[string.Empty]!.Errors.Select(e => e.ErrorMessage).ToList();
        messages.Should().Contain(m => m.Contains("Enter a CPHH for each linked farm row"));
        messages.Should().Contain(m => m.Contains("must be 11 digits"));
        messages.Should().Contain(m => m.Contains("Cannot link a farm to itself"));
        messages.Should().Contain(m => m.Contains("is duplicated"));
        messages.Should().Contain(m => m.Contains("year must be"));
        messages.Should().Contain(m => m.Contains("total size must be between"));
        messages.Should().Contain(m => m.Contains("lactation values must be between"));
    }

    [Fact]
    public async Task SaveFarm_WithoutVlaBatchAccess_KeepsThePersistedHerdmarks()
    {
        GivenExistingCase(new FarmRecord
        {
            CPHH = Cphh, OwnerName = "Owner", Address1 = "1 Lane", Parish = "P", County = "TC", AHO = "TA", ADNSRegionID = 1,
            Herdmark1 = "AB", NumericHerdmark1 = "123456", RowStamp = [1, 2, 3]
        });
        var model = Create("DataEntry", "VLAAccess");
        model.EditableFarm = Posted();
        model.EditableFarm.Herdmark1 = "ZZ";
        model.EditableFarm.NumericHerdmark1 = "999999";

        await model.OnPostSaveFarmAsync();

        model.EditableFarm!.Herdmark1.Should().Be("AB");
        model.EditableFarm.NumericHerdmark1.Should().Be("123456");
    }

    [Fact]
    public async Task CreateCase_WithAnAuthorityButNoAdnsRegion_IsNotFlaggedAsIncompatible()
    {
        _farms.GetByCphhAsync(Cphh).Returns(MakeFarm());
        var model = Create("DataEntry");
        model.EditableFarm = Posted();
        model.EditableFarm.AuthorityID = 7;
        model.EditableFarm.ADNSRegionID = null;

        await model.OnPostCreateCaseAsync();

        model.ModelState.ContainsKey("EditableFarm.ADNSRegionID").Should().BeFalse();
    }

    [Fact]
    public async Task CreateCase_WhenNoRegionMatchesTheAuthority_IsRejected()
    {
        _farms.GetByCphhAsync(Cphh).Returns(MakeFarm());
        var model = Create("DataEntry");
        model.EditableFarm = Posted();
        model.EditableFarm.AuthorityID = 7;
        model.EditableFarm.ADNSRegionID = 1;

        (await model.OnPostCreateCaseAsync()).Should().BeOfType<PageResult>();

        model.ModelState["EditableFarm.ADNSRegionID"]!.Errors.Should().Contain(e => e.ErrorMessage.Contains("does not match"));
    }
}
