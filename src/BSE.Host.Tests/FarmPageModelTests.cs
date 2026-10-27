using System.Security.Claims;
using System.Text.Json;
using BSE.Host.Models.ViewModels;
using BSE.Host.Pages.Case;
using BSE.Host.Services;
using BSE.Modules.Batch.Models;
using BSE.Modules.Batch.Repositories;
using BSE.Modules.CaseManagement.Enums;
using BSE.Modules.CaseManagement.Models;
using BSE.Modules.CaseManagement.Services;
using BSE.Modules.FarmManagement.Models;
using BSE.Modules.FarmManagement.Repositories;
using BSE.Modules.FarmManagement.Services;
using BSE.Modules.ReferenceData.Models;
using BSE.Modules.ReferenceData.Services;
using BSE.SharedKernel;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using IGeoLookupService = BSE.Host.Services.IGeoLookupService;

namespace BSE.Host.Tests;

public sealed class FarmPageModelTests
{
    private const string Rbse = "002600001";
    private const string NewRbse = "002600099";
    private const string Cphh = "01001000101";
    private const string NewCphh = "01001000202";
    private static readonly string[] DataEntry = ["DataEntry"];
    private static readonly string[] VlaDataEntry = ["DataEntry", "VLAAccess"];
    private static readonly string[] VlaMaintenance = ["DataEntry", "VLAAccess", "VLAMaintenance"];
    private static readonly string[] DefraViewer = ["DEFRAAccess"];

    private readonly ICaseService _cases = Substitute.For<ICaseService>();
    private readonly IFarmService _farms = Substitute.For<IFarmService>();
    private readonly ILookupDataService _lookups = Substitute.For<ILookupDataService>();
    private readonly IBatchRepository _batches = Substitute.For<IBatchRepository>();
    private readonly ICaseWizardStateService _wizard = Substitute.For<ICaseWizardStateService>();
    private readonly ICaseEditSessionStateService _session = Substitute.For<ICaseEditSessionStateService>();
    private readonly ICaseDraftDiscardService _discard = Substitute.For<ICaseDraftDiscardService>();
    private readonly ICaseEditOrchestrationService _orchestration = Substitute.For<ICaseEditOrchestrationService>();
    private readonly ICurrentUserService _user = Substitute.For<ICurrentUserService>();
    private readonly IGeoLookupService _geo = Substitute.For<IGeoLookupService>();
    private CaseEditSessionState? _draft;
    private List<FarmRelationRecord> _relations = [];
    private List<HerdSizeRecord> _herds = [];

    public FarmPageModelTests()
    {
        _user.GetUserIdAsync().Returns(4);
        _cases.GetCaseAsync(Rbse).Returns(new CaseRecord { Rbse = Rbse, Cphh = Cphh });
        _cases.GetCaseAsync(NewRbse).Returns((CaseRecord?)null);
        _farms.GetByCphhAsync(Arg.Any<string>()).Returns((FarmRecord?)null);
        _farms.GetByCphhAsync(Cphh).Returns(MakeFarm());
        _farms.GetConfirmedCaseCountAsync(Arg.Any<string>()).Returns(2);
        _farms.GetRelatedFarmsAsync(Arg.Any<string>()).Returns(_ => (IEnumerable<FarmRelationRecord>)_relations);
        _farms.GetHerdSizesAsync(Arg.Any<string>()).Returns(_ => (IEnumerable<HerdSizeRecord>)_herds);
        _lookups.GetADNSRegionsAsync().Returns((IEnumerable<LuADNSRegion>)[new LuADNSRegion { Id = 5, Name = "Reg" }]);
        _lookups.GetLookupAsync(Arg.Any<LookupTableId>()).Returns((IEnumerable<LookupItem>)[]);
        _lookups.GetLookupAsync(LookupTableId.BSECounty).Returns((IEnumerable<LookupItem>)[new LookupItem(1, "KT", "Kent")]);
        _lookups.GetLookupAsync(LookupTableId.AHO).Returns((IEnumerable<LookupItem>)[new LookupItem(1, "A1", "Aho One")]);
        _lookups.GetLookupAsync(LookupTableId.PedigreeType).Returns((IEnumerable<LookupItem>)[new LookupItem(1, "PT", "Pedigree")]);
        _lookups.GetLookupAsync(LookupTableId.AuthorityCounty).Returns((IEnumerable<LookupItem>)[new LookupItem(2, "AC", "Auth County")]);
        _lookups.GetHerdTypesAsync().Returns((IEnumerable<LuHerdType>)[new LuHerdType { Id = 1, Code = "H", Description = "Dairy" }]);
        _lookups.GetAuthoritiesByCountyAsync(2).Returns((IEnumerable<LuAuthority>)[new LuAuthority { Id = 3, Name = "Auth" }]);
        _lookups.GetADNSRegionsByAuthorityAsync(3).Returns((IEnumerable<LuADNSRegion>)[new LuADNSRegion { Id = 5, Name = "Reg" }]);
        _batches.GetBatchNumbersByRbseAsync(Arg.Any<string>()).Returns((IReadOnlyList<BatchNumberEntry>)[]);
        _session.GetAsync(Arg.Any<string>()).Returns(_ => _draft);
        _session.SetAsync(Arg.Any<CaseEditSessionState>()).Returns(ci =>
        {
            _draft = ci.Arg<CaseEditSessionState>();
            return Task.CompletedTask;
        });
        _orchestration.CommitAllAsync(Arg.Any<string>(), 4).Returns(CaseCommitOutcome.Success([]));
    }

    private static FarmRecord MakeFarm(string cphh = Cphh) => new()
    {
        CPHH = cphh, OwnerName = "Owner", Address1 = "1 Road", Parish = "P", County = "KT", AHO = "A1",
        HerdType = "H", PedigreeType = "PT", IsDealer = true, Herdmark1 = "HM1", NumericHerdmark1 = "123456",
        ADNSRegionID = 5, AuthorityID = 3, AuthorityCountyID = 2, RowStamp = [1]
    };

    private static HerdSizeRecord Herd(int id, short year, short total, short lac) => new()
    {
        ID = id, CPHH = Cphh, HerdYear = year, TotalSize = total,
        Lactation1Size = lac, Lactation2Size = lac, Lactation3Size = lac, Lactation4Size = lac, Lactation5Size = lac,
        Lactation6Size = lac, Lactation7Size = lac, Lactation8Size = lac, Lactation9Size = lac, Lactation10Size = lac,
        Lactation10PlusSize = lac
    };

    private FarmModel CreateModel(string[]? roles = null, string rbse = Rbse)
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["SpolSiteUrl"] = "http://spol" }).Build();
        var model = new FarmModel(_cases, _farms, Substitute.For<IFarmRelationRepository>(), Substitute.For<IHerdSizeRepository>(),
            _lookups, _batches, _wizard, _session, _discard, _orchestration, _user, NullLogger<FarmModel>.Instance, config, _geo)
        { Rbse = rbse };
        var http = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity((roles ?? []).Select(r => new Claim(ClaimTypes.Role, r)), "Test"))
        };
        model.PageContext = new PageContext { HttpContext = http };
        model.PageContext.ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), model.ModelState);
        model.TempData = new TempDataDictionary(http, Substitute.For<ITempDataProvider>());
        return model;
    }

    private void GiveBatchHistory() =>
        _batches.GetBatchNumbersByRbseAsync(Rbse).Returns((IReadOnlyList<BatchNumberEntry>)[new BatchNumberEntry(1, "B1", Rbse, "BSE1")]);

    private static string Json(IActionResult r) => JsonSerializer.Serialize(((JsonResult)r).Value);

    private static string Stamp => Convert.ToBase64String(new byte[] { 1 });

    private static FarmEditViewModel Posted(Action<FarmEditViewModel>? tweak = null)
    {
        var vm = FarmEditViewModel.FromRecord(MakeFarm());
        tweak?.Invoke(vm);
        return vm;
    }

    private CaseEditSessionState SeedDraft(
        IEnumerable<CaseFarmDraftLinkedFarmItem>? links = null, IEnumerable<CaseFarmDraftHerdSizeItem>? herds = null)
    {
        _draft = new CaseEditSessionState
        {
            Rbse = Rbse, Cphh = Cphh, HasFarmCollectionsChanges = true,
            LinkedFarms = (links ?? []).ToList(), HerdSizes = (herds ?? []).ToList()
        };
        return _draft;
    }

    private static CaseFarmDraftLinkedFarmItem Link(string key, string cphh) => new() { ClientKey = key, RelatedCphh = cphh, Status = "s" };
    private static CaseFarmDraftHerdSizeItem HerdItem(string key, int year = 2020, int total = 50) => new() { ClientKey = key, HerdYear = year, TotalSize = total };

    // ── GET: existing case ──

    [Fact]
    public async Task OnGet_ExistingCase_LoadsFarmLookupsAndSeedsDraft()
    {
        _relations = [new FarmRelationRecord { ID = 1, CPHH = Cphh, RelatedCPHH = "01001000303", Status = "S", RowStamp = [2] }];
        _herds = [Herd(7, 2020, 40, 4)];
        var model = CreateModel(DataEntry);

        var result = await model.OnGetAsync();

        result.Should().BeOfType<PageResult>();
        model.SpolSiteUrl.Should().Be("http://spol");
        model.Farm!.CPHH.Should().Be(Cphh);
        model.ConfirmedCaseCount.Should().Be(2);
        model.ADNSRegionName.Should().Be("Reg");
        model.CountyName.Should().Be("Kent");
        model.AHOName.Should().Be("Aho One");
        model.HerdTypeName.Should().Be("Dairy");
        model.PedigreeTypeName.Should().Be("Pedigree");
        model.AuthorityCountyName.Should().Be("Auth County");
        model.LocalAuthorityName.Should().Be("Auth");
        model.LinkedFarms.Should().ContainSingle();
        model.HerdSizes.Should().ContainSingle();
        model.EditableFarm!.OwnerName.Should().Be("Owner");
        model.EditableFarmRowStampBase64.Should().Be(Stamp);
        model.StagedLinkedFarms.Should().ContainSingle().Which.RelatedCphh.Should().Be("01001000303");
        model.StagedHerdSizes.Should().ContainSingle().Which.HerdYear.Should().Be(2020);
        model.HasUnsavedChanges.Should().BeFalse();
        _draft!.Cphh.Should().Be(Cphh);
        model.ViewData["AuthorityOptions"].Should().BeAssignableTo<IEnumerable<LuAuthority>>().Which.Should().ContainSingle();
    }

    [Fact]
    public async Task OnGet_StagedFarmForSameCphh_OverlaysEditableFarm()
    {
        _draft = new CaseEditSessionState { Rbse = Rbse, Farm = Posted(f => f.OwnerName = "Staged").ToUpdateCommand([1]) };
        var model = CreateModel(DataEntry);

        await model.OnGetAsync();

        model.EditableFarm!.OwnerName.Should().Be("Staged");
    }

    [Fact]
    public async Task OnGet_StagedFarmForDifferentCphh_IsIgnored()
    {
        _draft = new CaseEditSessionState { Rbse = Rbse, Farm = Posted(f => { f.CPHH = NewCphh; f.OwnerName = "Staged"; }).ToUpdateCommand([1]) };
        var model = CreateModel(DataEntry);

        await model.OnGetAsync();

        model.EditableFarm!.OwnerName.Should().Be("Owner");
    }

    [Fact]
    public async Task OnGet_DraftForOtherCphh_IsReseededFromPersisted()
    {
        _relations = [new FarmRelationRecord { ID = 1, RelatedCPHH = "01001000303" }];
        _draft = new CaseEditSessionState
        {
            Rbse = Rbse, Cphh = NewCphh, HasFarmCollectionsChanges = true,
            LinkedFarms = [Link("x", "01001000999")]
        };
        var model = CreateModel(DataEntry);

        await model.OnGetAsync();

        model.StagedLinkedFarms.Should().ContainSingle().Which.RelatedCphh.Should().Be("01001000303");
    }

    [Fact]
    public async Task OnGet_ExistingDraftForSameCphh_IsKept()
    {
        SeedDraft(links: [Link("k1", "01001000555")]);
        var model = CreateModel(DataEntry);

        await model.OnGetAsync();

        model.StagedLinkedFarms.Should().ContainSingle().Which.ClientKey.Should().Be("k1");
        model.HasUnsavedChanges.Should().BeTrue();
    }

    [Theory]
    [InlineData("DataEntry", true, false, true)]
    [InlineData("DEFRAAccess", false, false, false)]
    public async Task OnGet_DefraRolePermissions(string role, bool joint, bool vla, bool defra)
    {
        var model = CreateModel([role]);

        await model.OnGetAsync();

        model.CanEditJointControls.Should().Be(joint);
        model.CanEditVlaControls.Should().Be(vla);
        model.CanEditDefraControls.Should().Be(defra);
        model.IsConfidentialViewOnly.Should().Be(role != "DataEntry");
    }

    [Fact]
    public async Task OnGet_VlaDataEntryWithoutBatch_ReadOnly()
    {
        var model = CreateModel(VlaDataEntry);

        await model.OnGetAsync();

        model.CanEditJointControls.Should().BeFalse();
        model.CanEditVlaControls.Should().BeFalse();
        model.CanEditDefraControls.Should().BeFalse();
    }

    [Fact]
    public async Task OnGet_VlaDataEntryWithBatchHistory_CanEditJointAndVlaButNotDefra()
    {
        GiveBatchHistory();
        var model = CreateModel(VlaDataEntry);

        await model.OnGetAsync();

        model.CanEditJointControls.Should().BeTrue();
        model.CanEditVlaControls.Should().BeTrue();
        model.CanEditDefraControls.Should().BeFalse();
        model.BatchNumbers.Should().ContainSingle();
    }

    [Fact]
    public async Task OnGet_VlaMaintenanceWithPendingBatch_CanEditAllAndShowsBatchAssignment()
    {
        _wizard.GetAsync().Returns(new CaseWizardState(Rbse, "2026/5", 9));
        var model = CreateModel(VlaMaintenance);

        await model.OnGetAsync();

        model.CanEditDefraControls.Should().BeTrue();
        model.CanEditVlaControls.Should().BeTrue();
        model.ShowBatchAssignment.Should().BeTrue();
        model.AlreadyInPendingBatch.Should().BeFalse();
        model.CanChangeCphh.Should().BeTrue();
    }

    [Fact]
    public async Task OnGet_PendingBatchAlreadyLinked_FlagsAlreadyInBatch()
    {
        _wizard.GetAsync().Returns(new CaseWizardState(Rbse, "2026/5", 1));
        GiveBatchHistory();
        var model = CreateModel(VlaMaintenance);

        await model.OnGetAsync();

        model.AlreadyInPendingBatch.Should().BeTrue();
    }

    [Fact]
    public async Task OnGet_PendingBatchForOtherCase_NoBatchAssignment()
    {
        _wizard.GetAsync().Returns(new CaseWizardState("002600777", "2026/5", 9));
        var model = CreateModel(VlaMaintenance);

        await model.OnGetAsync();

        model.ShowBatchAssignment.Should().BeFalse();
    }

    // ── Sorting / paging ──

    [Theory]
    [InlineData("cphh", "asc", "01001000101")]
    [InlineData("cphh", "desc", "01001000303")]
    [InlineData("status", "asc", "01001000303")]
    [InlineData("status", "desc", "01001000101")]
    public async Task LinkedFarms_SortByColumn(string sort, string dir, string expectedFirst)
    {
        _relations =
        [
            new FarmRelationRecord { ID = 1, RelatedCPHH = "01001000303", Status = "A" },
            new FarmRelationRecord { ID = 2, RelatedCPHH = "01001000101", Status = "Z" }
        ];
        var model = CreateModel(DataEntry);
        model.LSort = sort;
        model.LDir = dir;

        await model.OnGetAsync();

        model.LinkedFarms.First().RelatedCPHH.Should().Be(expectedFirst);
        model.SortedStagedLinkedFarms().First().RelatedCphh.Should().Be(expectedFirst);
    }

    [Fact]
    public async Task LinkedFarms_PageBeyondEnd_IsClampedAndUrlsReflectState()
    {
        _relations = Enumerable.Range(1, 12).Select(i => new FarmRelationRecord { ID = i, RelatedCPHH = $"0100100{i:0000}" }).ToList();
        var model = CreateModel(DataEntry);
        model.LPage = 9;

        await model.OnGetAsync();

        model.LinkedFarmsTotalCount.Should().Be(12);
        model.LinkedFarmsTotalPages.Should().Be(2);
        model.LPage.Should().Be(2);
        model.LinkedFarmsCurrentPage.Should().Be(2);
        model.LinkedFarms.Should().HaveCount(2);
        model.PagedStagedLinkedFarms().Should().HaveCount(2);
        model.LinkedFarmsPageUrl(1).Should().StartWith("?LPage=1&LSort=cphh&LDir=asc");
        model.LinkedFarmsSortUrl("cphh").Should().Contain("LDir=desc");
        model.LinkedFarmsSortUrl("status").Should().Contain("LSort=status&LDir=asc");
        model.LinkedFarmAriaSort("cphh").Should().Be("ascending");
        model.LinkedFarmAriaSort("status").Should().Be("none");
    }

    [Theory]
    [InlineData("year", "asc", 2001, 2001)]
    [InlineData("year", "desc", 2003, 2003)]
    [InlineData("zzz", "asc", 2003, 2001)]
    [InlineData("total", "asc", 2002, 2002)]
    [InlineData("total", "desc", 2001, 2001)]
    public async Task HerdSizes_SortByColumn(string sort, string dir, int expectedStagedFirst, int expectedPersistedFirst)
    {
        _herds = [Herd(1, 2001, 30, 3), Herd(2, 2002, 10, 1), Herd(3, 2003, 20, 2)];
        var model = CreateModel(DataEntry);
        model.HSort = sort;
        model.HDir = dir;

        await model.OnGetAsync();

        model.SortedStagedHerdSizes().First().HerdYear.Should().Be(expectedStagedFirst);
        model.HerdSizes.First().HerdYear.Should().Be((short)expectedPersistedFirst);
        model.PagedSortedStagedHerdSizes().First().HerdYear.Should().Be(expectedStagedFirst);
    }

    [Fact]
    public async Task HerdSizes_EveryLactationColumnSortsAscendingAndDescending()
    {
        _herds = [Herd(1, 2001, 30, 3), Herd(2, 2002, 10, 1), Herd(3, 2003, 20, 2)];
        foreach (var col in new[] { "lac1", "lac2", "lac3", "lac4", "lac5", "lac6", "lac7", "lac8", "lac9", "lac10", "lac10p" })
        {
            foreach (var (dir, expected) in new[] { ("asc", 2002), ("desc", 2001) })
            {
                var model = CreateModel(DataEntry);
                model.HSort = col;
                model.HDir = dir;

                await model.OnGetAsync();

                model.SortedStagedHerdSizes().First().HerdYear.Should().Be(expected, $"{col} {dir} staged");
                model.HerdSizes.First().HerdYear.Should().Be((short)expected, $"{col} {dir} persisted");
            }
        }
    }

    [Fact]
    public async Task HerdSizes_PagingUrlsAndAriaSort()
    {
        _herds = Enumerable.Range(0, 12).Select(i => Herd(i + 1, (short)(1990 + i), 10, 1)).ToList();
        var model = CreateModel(DataEntry);
        model.HPage = 7;

        await model.OnGetAsync();

        model.HerdSizesTotalCount.Should().Be(12);
        model.HerdSizesTotalPages.Should().Be(2);
        model.HerdSizesCurrentPage.Should().Be(2);
        model.HerdSizeSortUrl("year").Should().Contain("HSort=year&HDir=desc");
        model.HerdSizeSortUrl("total").Should().Contain("HSort=total&HDir=asc");
        model.HerdSizesPageUrl(2).Should().StartWith("?HPage=2&HSort=year&HDir=asc");
        model.HerdSizeAriaSort("year").Should().Be("ascending");
        model.HerdSizeAriaSort("total").Should().Be("none");
        model.HDir = "desc";
        model.HerdSizeAriaSort("year").Should().Be("descending");
    }

    [Fact]
    public void StagedHerdSizeItem_ReportsMismatchAndUnsavedState()
    {
        var mismatch = new FarmModel.StagedHerdSizeItem { TotalSize = 10, Lactation1Size = 4 };
        mismatch.LactationMismatchWarning.Should().Contain("(4)").And.Contain("(10)");
        mismatch.IsUnsaved.Should().BeTrue();

        var match = new FarmModel.StagedHerdSizeItem { Id = 3, TotalSize = 4, Lactation1Size = 4 };
        match.LactationMismatchWarning.Should().BeNull();
        match.IsUnsaved.Should().BeFalse();
    }

    // ── GET: create mode ──

    [Fact]
    public async Task OnGet_NoCaseNoCphh_ShowsEmptyCreateForm()
    {
        var model = CreateModel(DataEntry, NewRbse);

        var result = await model.OnGetAsync();

        result.Should().BeOfType<PageResult>();
        model.Case.Should().BeNull();
        model.CanEditCreateMode.Should().BeTrue();
        model.RequireFarmDetails.Should().BeFalse();
        model.HasResolvedCphh.Should().BeFalse();
    }

    [Fact]
    public async Task OnGet_NoCaseDefraViewer_CannotCreate()
    {
        var model = CreateModel(DefraViewer, NewRbse);

        await model.OnGetAsync();

        model.CanEditCreateMode.Should().BeFalse();
    }

    [Fact]
    public async Task OnGet_NoCaseWithExistingFarmCphh_LoadsFarm()
    {
        _farms.GetByCphhAsync(NewCphh).Returns(MakeFarm(NewCphh));
        var model = CreateModel(DataEntry, NewRbse);
        model.NewCphh = "01/001/0002/02";

        await model.OnGetAsync();

        model.Farm!.CPHH.Should().Be(NewCphh);
        model.EditableFarm!.CPHH.Should().Be(NewCphh);
        model.RequireFarmDetails.Should().BeFalse();
        model.HasResolvedCphh.Should().BeTrue();
    }

    [Fact]
    public async Task OnGet_NoCaseWithUnknownFarmCphh_RequiresFarmDetails()
    {
        var model = CreateModel(DataEntry, NewRbse);
        model.NewCphh = NewCphh;

        await model.OnGetAsync();

        model.Farm.Should().BeNull();
        model.RequireFarmDetails.Should().BeTrue();
        model.EditableFarm!.CPHH.Should().Be(NewCphh);
    }

    [Fact]
    public async Task OnGet_ForceNewFarmDetails_AppliesSeeds()
    {
        var model = CreateModel(DataEntry, NewRbse);
        model.NewCphh = NewCphh;
        model.ForceNewFarmDetails = true;
        model.SeedParish = "SeedP";
        model.SeedCounty = "SeedC";
        model.SeedAdnsRegionId = 5;
        model.SeedAuthorityId = 3;
        model.SeedAuthorityCountyId = 2;
        model.SeedHerdmark1 = "HM";
        model.SeedNumericHerdmark1 = "654321";

        await model.OnGetAsync();

        model.RequireFarmDetails.Should().BeTrue();
        var farm = model.EditableFarm!;
        farm.CPHH.Should().Be(NewCphh);
        farm.Parish.Should().Be("SeedP");
        farm.County.Should().Be("SeedC");
        farm.ADNSRegionID.Should().Be(5);
        farm.AuthorityID.Should().Be(3);
        farm.AuthorityCountyID.Should().Be(2);
        farm.Herdmark1.Should().Be("HM");
        farm.NumericHerdmark1.Should().Be("654321");
        model.ViewData["AdnsOptions"].Should().BeAssignableTo<IEnumerable<LuADNSRegion>>().Which.Should().ContainSingle();
    }

    [Fact]
    public async Task OnGet_SelectedCphhOfExistingFarm_UsesThatFarm()
    {
        _farms.GetByCphhAsync(NewCphh).Returns(MakeFarm(NewCphh));
        var model = CreateModel(DataEntry, NewRbse);
        model.SelectedCphh = "01/001/0002/02";

        await model.OnGetAsync();

        model.EditableFarm!.CPHH.Should().Be(NewCphh);
        model.RequireFarmDetails.Should().BeFalse();
    }

    [Fact]
    public async Task OnGet_NoCaseWithDraftCphh_UsesDraftCphh()
    {
        _farms.GetByCphhAsync(NewCphh).Returns(MakeFarm(NewCphh));
        _draft = new CaseEditSessionState { Rbse = NewRbse, Cphh = NewCphh };
        var model = CreateModel(DataEntry, NewRbse);

        await model.OnGetAsync();

        model.EditableFarm!.CPHH.Should().Be(NewCphh);
    }

    [Fact]
    public async Task OnGet_NoCaseStagedFarmMatchingRequestedCphh_IsOverlaid()
    {
        _farms.GetByCphhAsync(NewCphh).Returns(MakeFarm(NewCphh));
        _draft = new CaseEditSessionState { Rbse = NewRbse, Farm = Posted(f => { f.CPHH = NewCphh; f.OwnerName = "Staged"; }).ToUpdateCommand([1]) };
        var model = CreateModel(DataEntry, NewRbse);
        model.NewCphh = NewCphh;

        await model.OnGetAsync();

        model.EditableFarm!.OwnerName.Should().Be("Staged");
    }

    // ── Cancel ──

    [Fact]
    public async Task Cancel_PostAndGet_DiscardDraftAndRedirectHome()
    {
        var post = await CreateModel(DataEntry).OnPostCancelFarmEditAsync();
        var get = await CreateModel(DataEntry).OnGetCancelFarmEditAsync();

        post.Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/Home");
        get.Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/Home");
        await _discard.Received(2).DiscardAllAsync(Rbse);
    }

    // ── Save farm ──

    [Fact]
    public async Task SaveFarm_WithoutDataEntry_Forbids()
    {
        (await CreateModel().OnPostSaveFarmAsync()).Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task SaveFarm_RowStillBeingEdited_ReturnsPageWithError()
    {
        var model = CreateModel(DataEntry);
        model.EditableFarm = Posted();
        model.EditingClientKey = "k";

        var result = await model.OnPostSaveFarmAsync();

        result.Should().BeOfType<PageResult>();
        model.ModelState[string.Empty]!.Errors.Single().ErrorMessage.Should().Contain("Finish editing");
        await _orchestration.DidNotReceiveWithAnyArgs().CommitAllAsync(default!, default);
    }

    [Fact]
    public async Task SaveFarm_InvalidStagedCollections_ReturnsPage()
    {
        SeedDraft(
            links: [Link("a", Cphh), Link("b", "123"), Link("c", ""), Link("d", "01001000303"), Link("e", "01001000303")],
            herds: [HerdItem("h", year: 1900, total: 5000)]);
        var model = CreateModel(DataEntry);
        model.EditableFarm = Posted();
        model.EditableFarmRowStampBase64 = Stamp;

        var result = await model.OnPostSaveFarmAsync();

        result.Should().BeOfType<PageResult>();
        var messages = model.ModelState[string.Empty]!.Errors.Select(e => e.ErrorMessage).ToList();
        messages.Should().Contain(m => m.Contains("Cannot link a farm to itself"));
        messages.Should().Contain(m => m.Contains("must be 11 digits"));
        messages.Should().Contain(m => m.Contains("Enter a CPHH for each linked farm row"));
        messages.Should().Contain(m => m.Contains("is duplicated"));
        messages.Should().Contain(m => m.Contains("year must be"));
        messages.Should().Contain(m => m.Contains("total size must be"));
        await _orchestration.DidNotReceiveWithAnyArgs().CommitAllAsync(default!, default);
    }

    [Fact]
    public async Task SaveFarm_OutOfRangeLactation_ReturnsPage()
    {
        var herd = HerdItem("h");
        herd.Lactation1Size = 5000;
        SeedDraft(herds: [herd]);
        var model = CreateModel(DataEntry);
        model.EditableFarm = Posted();

        var result = await model.OnPostSaveFarmAsync();

        result.Should().BeOfType<PageResult>();
        model.ModelState[string.Empty]!.Errors.Should().Contain(e => e.ErrorMessage.Contains("lactation values"));
    }

    [Fact]
    public async Task SaveFarm_NoPostedFarm_ReturnsPage()
    {
        var model = CreateModel(DataEntry);
        model.EditableFarm = null;

        var result = await model.OnPostSaveFarmAsync();

        result.Should().BeOfType<PageResult>();
        await _orchestration.DidNotReceiveWithAnyArgs().CommitAllAsync(default!, default);
    }

    [Fact]
    public async Task SaveFarm_BlankCphhAndBadHerdmarks_ReportsFieldErrors()
    {
        var model = CreateModel(DataEntry);
        model.EditableFarm = Posted(f => { f.CPHH = ""; f.NumericHerdmark1 = "12"; f.NumericHerdmark2 = "abcdef"; });

        var result = await model.OnPostSaveFarmAsync();

        result.Should().BeOfType<PageResult>();
        model.ModelState.Keys.Should().Contain([
            "EditableFarm.CPHH", "EditableFarm.NumericHerdmark1", "EditableFarm.NumericHerdmark2"]);
    }

    [Fact]
    public async Task SaveFarm_AdnsRegionNotInAuthority_ReportsError()
    {
        var model = CreateModel(DataEntry);
        model.EditableFarm = Posted(f => f.ADNSRegionID = 99);

        var result = await model.OnPostSaveFarmAsync();

        result.Should().BeOfType<PageResult>();
        model.ModelState["EditableFarm.ADNSRegionID"]!.Errors.Single().ErrorMessage.Should().Contain("does not match");
    }

    [Fact]
    public async Task SaveFarm_Valid_StagesFarmCommitsAndRedirectsHome()
    {
        var model = CreateModel(DataEntry);
        model.EditableFarm = Posted(f => { f.OwnerName = "New Owner"; f.PedigreeType = "HACK"; f.IsDealer = false; f.Herdmark1 = "NEWHM"; });
        model.EditableFarmRowStampBase64 = Stamp;

        var result = await model.OnPostSaveFarmAsync();

        result.Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/Home");
        _draft!.Farm!.OwnerName.Should().Be("New Owner");
        _draft.Farm.Herdmark1.Should().Be("NEWHM");
        _draft.Farm.PedigreeType.Should().Be("PT");
        _draft.Farm.IsDealer.Should().BeTrue();
        _draft.Farm.RowStamp.Should().Equal(1);
        _draft.HasPendingChanges.Should().BeTrue();
        await _orchestration.Received(1).CommitAllAsync(Rbse, 4);
    }

    [Fact]
    public async Task SaveFarm_VlaDataEntry_RevertsJointFieldsWhenNotEditable()
    {
        var model = CreateModel(VlaDataEntry);
        model.EditableFarm = Posted(f => { f.Herdmark1 = "HACK"; f.NumericHerdmark1 = "999999"; });

        await model.OnPostSaveFarmAsync();

        _draft!.Farm!.Herdmark1.Should().Be("HM1");
        _draft.Farm.NumericHerdmark1.Should().Be("123456");
    }

    [Fact]
    public async Task SaveFarm_MandatoryFieldsMissing_RedirectsToSaveResult()
    {
        _orchestration.CommitAllAsync(Rbse, 4).Returns<Task<CaseCommitOutcome>>(_ => throw new MandatoryCaseFieldsMissingException(["Owner"]));
        var model = CreateModel(DataEntry);
        model.EditableFarm = Posted();

        var result = await model.OnPostSaveFarmAsync();

        result.Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/Case/SaveResult");
    }

    [Fact]
    public async Task SaveFarm_CommitFailure_RedirectsHomeWithError()
    {
        _orchestration.CommitAllAsync(Rbse, 4).Returns(CaseCommitOutcome.Failure(EditCaseResult.PostUpdateError));
        var model = CreateModel(DataEntry);
        model.EditableFarm = Posted();

        var result = await model.OnPostSaveFarmAsync();

        result.Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/Home");
        model.TempData["ErrorMessage"]!.ToString().Should().StartWith("Unable to save farm changes");
    }

    [Fact]
    public async Task SaveFarm_Warnings_RedirectsToSaveResult()
    {
        _orchestration.CommitAllAsync(Rbse, 4).Returns(CaseCommitOutcome.Success(["w"]));
        var model = CreateModel(DataEntry);
        model.EditableFarm = Posted();

        var result = await model.OnPostSaveFarmAsync();

        result.Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/Case/SaveResult");
    }

    // ── Stage and goto ──

    [Fact]
    public async Task StageAndGoto_WithoutDataEntry_RedirectsWithoutStaging()
    {
        var result = await CreateModel(DefraViewer).OnPostStageAndGotoAsync("/Case/Vla");

        var redirect = result.Should().BeOfType<RedirectToPageResult>().Subject;
        redirect.PageName.Should().Be("/Case/Vla");
        redirect.RouteValues!["rbse"].Should().Be(Rbse);
        _draft.Should().BeNull();
    }

    [Fact]
    public async Task StageAndGoto_NoPostedFarm_RedirectsWithoutStaging()
    {
        var model = CreateModel(DataEntry);
        model.EditableFarm = null;

        var result = await model.OnPostStageAndGotoAsync("/Case/Vla");

        result.Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/Case/Vla");
        await _session.DidNotReceiveWithAnyArgs().SetAsync(default!);
    }

    [Fact]
    public async Task StageAndGoto_NewCaseWithoutCphh_NavigatesWithoutStaging()
    {
        var model = CreateModel(DataEntry, NewRbse);
        model.EditableFarm = new FarmEditViewModel();

        var result = await model.OnPostStageAndGotoAsync("/Case/Vla");

        result.Should().BeOfType<RedirectToPageResult>();
        await _session.DidNotReceiveWithAnyArgs().SetAsync(default!);
    }

    [Fact]
    public async Task StageAndGoto_InvalidFarm_ReturnsPage()
    {
        var model = CreateModel(DataEntry);
        model.EditableFarm = Posted(f => f.CPHH = "");
        model.EditableFarm.NumericHerdmark1 = "1";

        var result = await model.OnPostStageAndGotoAsync("/Case/Vla");

        result.Should().BeOfType<PageResult>();
        model.ModelState.Should().ContainKey("EditableFarm.CPHH");
    }

    [Fact]
    public async Task StageAndGoto_UnchangedFarm_StagesWithoutPendingFlag()
    {
        var model = CreateModel(DataEntry);
        model.EditableFarm = Posted();
        model.EditableFarmRowStampBase64 = Stamp;

        var result = await model.OnPostStageAndGotoAsync("/Case/Vla");

        result.Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/Case/Vla");
        _draft!.Farm.Should().NotBeNull();
        _draft.HasPendingChanges.Should().BeFalse();
    }

    [Fact]
    public async Task StageAndGoto_EditedFarm_FlagsPendingChanges()
    {
        var model = CreateModel(DataEntry);
        model.EditableFarm = Posted(f => f.OwnerName = "Changed");
        model.EditableFarmRowStampBase64 = Stamp;

        await model.OnPostStageAndGotoAsync("/Case/Vla");

        _draft!.Farm!.OwnerName.Should().Be("Changed");
        _draft.HasPendingChanges.Should().BeTrue();
    }

    [Fact]
    public async Task StageAndGoToAuditLog_WithoutDataEntry_Forbids()
    {
        (await CreateModel(DefraViewer).OnPostStageAndGoToAuditLogAsync()).Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task StageAndGoToAuditLog_Valid_RedirectsToFarmAuditLog()
    {
        var model = CreateModel(DataEntry);
        model.EditableFarm = Posted();

        var result = await model.OnPostStageAndGoToAuditLogAsync();

        var redirect = result.Should().BeOfType<RedirectToPageResult>().Subject;
        redirect.PageName.Should().Be("/AuditLog/ByFarm");
        redirect.RouteValues!["cphh"].Should().Be(Cphh);
        _draft!.Farm.Should().NotBeNull();
    }

    [Fact]
    public async Task StageAndGoToAuditLog_InvalidFarm_ReturnsPage()
    {
        var model = CreateModel(DataEntry);
        model.EditableFarm = Posted(f => f.CPHH = "");

        var result = await model.OnPostStageAndGoToAuditLogAsync();

        result.Should().BeOfType<PageResult>();
    }

    // ── Linked farms ──

    [Fact]
    public async Task AddLinkedFarm_WithoutDataEntry_Forbids()
    {
        (await CreateModel().OnPostAddLinkedFarmRowAsync()).Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task AddLinkedFarm_NotAllowedToEditDefra_RedirectsToPage()
    {
        var model = CreateModel(VlaDataEntry);
        model.NewLinkedCphh = "01001000303";

        var result = await model.OnPostAddLinkedFarmRowAsync();

        result.Should().BeOfType<RedirectToPageResult>();
        _draft?.LinkedFarms.Should().NotContain(l => l.RelatedCphh == "01001000303");
    }

    [Fact]
    public async Task AddLinkedFarm_Valid_StagesRowWithFarmStatus()
    {
        _farms.GetByCphhAsync("01001000404").Returns(new FarmRecord { CPHH = "01001000404", OwnerName = "Bob", Address1 = "Rd" });
        SeedDraft(links: [Link("a", "01001000303")]);
        var model = CreateModel(DataEntry);
        model.EditableFarm = Posted(f => f.OwnerName = "Unsaved");
        model.NewLinkedCphh = "01/001/0004/04";

        var result = await model.OnPostAddLinkedFarmRowAsync();

        result.Should().BeOfType<RedirectToPageResult>();
        _draft!.LinkedFarms.Should().HaveCount(2);
        _draft.LinkedFarms.Last().RelatedCphh.Should().Be("01001000404");
        _draft.LinkedFarms.Last().Status.Should().Be("Bob, Rd");
        _draft.HasFarmCollectionsChanges.Should().BeTrue();
        _draft.Farm!.OwnerName.Should().Be("Unsaved");
    }

    [Fact]
    public async Task AddLinkedFarm_UnknownFarm_StatusIsBseFree()
    {
        SeedDraft();
        var model = CreateModel(DataEntry);
        model.NewLinkedCphh = "01001000404";

        await model.OnPostAddLinkedFarmRowAsync();

        _draft!.LinkedFarms.Single().Status.Should().Be("BSE Free");
    }

    [Theory]
    [InlineData("", "Enter a CPHH")]
    [InlineData("123", "11 digits")]
    [InlineData(Cphh, "Cannot link a farm to itself")]
    [InlineData("01001000303", "already in the Linked Farms list")]
    public async Task AddLinkedFarm_Invalid_ReopensAddRow(string cphh, string expected)
    {
        SeedDraft(links: [Link("a", "01001000303")]);
        var model = CreateModel(DataEntry);
        model.NewLinkedCphh = cphh;

        var result = await model.OnPostAddLinkedFarmRowAsync();

        result.Should().BeOfType<PageResult>();
        model.ShowAddLinkedRow.Should().BeTrue();
        model.NewLinkedCphh.Should().Be(cphh);
        model.ModelState[nameof(model.NewLinkedCphh)]!.Errors.Should().Contain(e => e.ErrorMessage.Contains(expected));
        _draft!.LinkedFarms.Should().HaveCount(1);
    }

    [Fact]
    public async Task BeginEditLinkedFarm_ExistingRow_ReopensWithCphh()
    {
        SeedDraft(links: [Link("a", "01001000303")]);
        var model = CreateModel(DataEntry);
        model.EditingLinkedClientKey = "a";

        var result = await model.OnPostBeginEditLinkedFarmRowAsync();

        result.Should().BeOfType<PageResult>();
        model.ReopenLinkedEditClientKey.Should().Be("a");
        model.EditLinkedCphh.Should().Be("01001000303");
    }

    [Fact]
    public async Task BeginEditLinkedFarm_WithoutDataEntry_Forbids()
    {
        (await CreateModel().OnPostBeginEditLinkedFarmRowAsync()).Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task BeginEditLinkedFarm_NotAllowed_Redirects()
    {
        var result = await CreateModel(VlaDataEntry).OnPostBeginEditLinkedFarmRowAsync();

        result.Should().BeOfType<RedirectToPageResult>();
    }

    [Fact]
    public async Task UpdateLinkedFarm_Valid_UpdatesRowAndStatus()
    {
        SeedDraft(links: [Link("a", "01001000303")]);
        var model = CreateModel(DataEntry);
        model.EditingLinkedClientKey = "a";
        model.EditLinkedCphh = "01001000404";

        var result = await model.OnPostUpdateLinkedFarmRowAsync();

        result.Should().BeOfType<RedirectToPageResult>();
        _draft!.LinkedFarms.Single().RelatedCphh.Should().Be("01001000404");
        _draft.LinkedFarms.Single().Status.Should().Be("BSE Free");
    }

    [Fact]
    public async Task UpdateLinkedFarm_MissingRow_SetsErrorMessage()
    {
        SeedDraft();
        var model = CreateModel(DataEntry);
        model.EditingLinkedClientKey = "gone";
        model.EditLinkedCphh = "01001000404";

        var result = await model.OnPostUpdateLinkedFarmRowAsync();

        result.Should().BeOfType<RedirectToPageResult>();
        model.TempData["ErrorMessage"]!.ToString().Should().Contain("no longer exists");
    }

    [Theory]
    [InlineData("", "Enter a CPHH")]
    [InlineData("123", "11 digits")]
    [InlineData(Cphh, "Cannot link a farm to itself")]
    [InlineData("01001000505", "already exists")]
    public async Task UpdateLinkedFarm_Invalid_ReopensEditRow(string cphh, string expected)
    {
        SeedDraft(links: [Link("a", "01001000303"), Link("b", "01001000505")]);
        var model = CreateModel(DataEntry);
        model.EditingLinkedClientKey = "a";
        model.EditLinkedCphh = cphh;

        var result = await model.OnPostUpdateLinkedFarmRowAsync();

        result.Should().BeOfType<PageResult>();
        model.ReopenLinkedEditClientKey.Should().Be("a");
        model.ModelState[nameof(model.EditLinkedCphh)]!.Errors.Should().Contain(e => e.ErrorMessage.Contains(expected));
        _draft!.LinkedFarms.First().RelatedCphh.Should().Be("01001000303");
    }

    [Fact]
    public async Task UpdateLinkedFarm_WithoutDataEntry_Forbids()
    {
        (await CreateModel().OnPostUpdateLinkedFarmRowAsync()).Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task DeleteLinkedFarm_RemovesRow()
    {
        SeedDraft(links: [Link("a", "01001000303"), Link("b", "01001000505")]);
        var model = CreateModel(DataEntry);

        var result = await model.OnPostDeleteLinkedFarmAsync("a");

        result.Should().BeOfType<RedirectToPageResult>();
        _draft!.LinkedFarms.Should().ContainSingle().Which.ClientKey.Should().Be("b");
    }

    [Fact]
    public async Task DeleteLinkedFarm_UnknownKey_LeavesRows()
    {
        SeedDraft(links: [Link("a", "01001000303")]);

        await CreateModel(DataEntry).OnPostDeleteLinkedFarmAsync("zzz");

        _draft!.LinkedFarms.Should().HaveCount(1);
    }

    [Fact]
    public async Task DeleteLinkedFarm_WithoutDataEntry_Forbids()
    {
        (await CreateModel().OnPostDeleteLinkedFarmAsync("a")).Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task DeleteLinkedFarm_NotAllowed_Redirects()
    {
        var result = await CreateModel(VlaDataEntry).OnPostDeleteLinkedFarmAsync("a");

        result.Should().BeOfType<RedirectToPageResult>();
    }

    [Fact]
    public async Task ResortGrids_RestagesPostedFarmAndRendersPage()
    {
        var model = CreateModel(DataEntry);
        model.EditableFarm = Posted(f => f.OwnerName = "Typed");
        model.EditableFarmRowStampBase64 = Stamp;
        model.HSort = "total";

        var result = await model.OnPostResortGridsAsync();

        result.Should().BeOfType<PageResult>();
        _draft!.Farm!.OwnerName.Should().Be("Typed");
        model.EditableFarm!.OwnerName.Should().Be("Typed");
    }

    // ── Herd sizes ──

    [Fact]
    public async Task HerdHandlers_WithoutDataEntry_Forbid()
    {
        (await CreateModel().OnPostBeginEditHerdRowAsync()).Should().BeOfType<ForbidResult>();
        (await CreateModel().OnPostAddHerdSizeRowAsync()).Should().BeOfType<ForbidResult>();
        (await CreateModel().OnPostUpdateHerdSizeRowAsync()).Should().BeOfType<ForbidResult>();
        (await CreateModel().OnPostDeleteHerdSizeAsync("x")).Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task HerdHandlers_WithoutVlaEditRights_Redirect()
    {
        (await CreateModel(DataEntry).OnPostBeginEditHerdRowAsync()).Should().BeOfType<RedirectToPageResult>();
        (await CreateModel(DataEntry).OnPostAddHerdSizeRowAsync()).Should().BeOfType<RedirectToPageResult>();
        (await CreateModel(DataEntry).OnPostUpdateHerdSizeRowAsync()).Should().BeOfType<RedirectToPageResult>();
        (await CreateModel(DataEntry).OnPostDeleteHerdSizeAsync("x")).Should().BeOfType<RedirectToPageResult>();
    }

    [Fact]
    public async Task BeginEditHerdRow_CopiesRowIntoEditInput()
    {
        GiveBatchHistory();
        var item = HerdItem("h1", 2019, 60);
        item.Lactation1Size = 5;
        item.Lactation10PlusSize = 7;
        SeedDraft(herds: [item]);
        var model = CreateModel(VlaMaintenance);
        model.EditingClientKey = "h1";

        var result = await model.OnPostBeginEditHerdRowAsync();

        result.Should().BeOfType<PageResult>();
        model.ReopenEditClientKey.Should().Be("h1");
        model.EditHerdRow.HerdYear.Should().Be(2019);
        model.EditHerdRow.TotalSize.Should().Be(60);
        model.EditHerdRow.Lactation1Size.Should().Be(5);
        model.EditHerdRow.Lactation10PlusSize.Should().Be(7);
    }

    [Fact]
    public async Task AddHerdRow_Valid_StagesNormalisedRow()
    {
        GiveBatchHistory();
        SeedDraft();
        var model = CreateModel(VlaMaintenance);
        model.NewHerdRow = new FarmModel.HerdSizeRowInput { HerdYear = 2021, TotalSize = 80, Lactation2Size = 9 };

        var result = await model.OnPostAddHerdSizeRowAsync();

        result.Should().BeOfType<RedirectToPageResult>();
        var row = _draft!.HerdSizes.Should().ContainSingle().Subject;
        row.HerdYear.Should().Be(2021);
        row.TotalSize.Should().Be(80);
        row.Lactation2Size.Should().Be(9);
        row.Lactation1Size.Should().Be(0);
        row.Id.Should().BeNull();
        row.ClientKey.Should().NotBeNullOrEmpty();
        _draft.HasFarmCollectionsChanges.Should().BeTrue();
    }

    [Fact]
    public async Task AddHerdRow_BlankLactations_StayNull()
    {
        GiveBatchHistory();
        SeedDraft();
        var model = CreateModel(VlaMaintenance);
        model.NewHerdRow = new FarmModel.HerdSizeRowInput { HerdYear = 2021, TotalSize = 80 };

        await model.OnPostAddHerdSizeRowAsync();

        _draft!.HerdSizes.Single().Lactation1Size.Should().BeNull();
    }

    [Fact]
    public async Task AddHerdRow_Invalid_ReopensAddRowWithErrors()
    {
        GiveBatchHistory();
        SeedDraft();
        var model = CreateModel(VlaMaintenance);
        model.NewHerdRow = new FarmModel.HerdSizeRowInput { HerdYear = 1800, TotalSize = 5000, Lactation3Size = 2000, Lactation10PlusSize = -1 };

        var result = await model.OnPostAddHerdSizeRowAsync();

        result.Should().BeOfType<PageResult>();
        model.ShowAddHerdRow.Should().BeTrue();
        model.ModelState.Keys.Should().Contain([
            "NewHerdRow.HerdYear", "NewHerdRow.TotalSize", "NewHerdRow.Lactation3Size", "NewHerdRow.Lactation10PlusSize"]);
        _draft!.HerdSizes.Should().BeEmpty();
    }

    [Fact]
    public async Task AddHerdRow_MissingYearAndTotal_ReportsRequired()
    {
        GiveBatchHistory();
        SeedDraft();
        var model = CreateModel(VlaMaintenance);
        model.NewHerdRow = new FarmModel.HerdSizeRowInput();

        await model.OnPostAddHerdSizeRowAsync();

        model.ModelState["NewHerdRow.HerdYear"]!.Errors.Single().ErrorMessage.Should().Be("Enter a year");
        model.ModelState["NewHerdRow.TotalSize"]!.Errors.Single().ErrorMessage.Should().Be("Enter a total herd size");
    }

    [Fact]
    public async Task UpdateHerdRow_Valid_UpdatesStagedRow()
    {
        GiveBatchHistory();
        SeedDraft(herds: [HerdItem("h1", 2019, 60)]);
        var model = CreateModel(VlaMaintenance);
        model.EditingClientKey = "h1";
        model.EditHerdRow = new FarmModel.HerdSizeRowInput { HerdYear = 2020, TotalSize = 70, Lactation1Size = 3 };

        var result = await model.OnPostUpdateHerdSizeRowAsync();

        result.Should().BeOfType<RedirectToPageResult>();
        var row = _draft!.HerdSizes.Single();
        row.HerdYear.Should().Be(2020);
        row.TotalSize.Should().Be(70);
        row.Lactation1Size.Should().Be(3);
        row.Lactation2Size.Should().Be(0);
    }

    [Fact]
    public async Task UpdateHerdRow_MissingRow_SetsErrorMessage()
    {
        GiveBatchHistory();
        SeedDraft();
        var model = CreateModel(VlaMaintenance);
        model.EditingClientKey = "gone";

        var result = await model.OnPostUpdateHerdSizeRowAsync();

        result.Should().BeOfType<RedirectToPageResult>();
        model.TempData["ErrorMessage"]!.ToString().Should().Contain("no longer exists");
    }

    [Fact]
    public async Task UpdateHerdRow_Invalid_ReopensEditRow()
    {
        GiveBatchHistory();
        SeedDraft(herds: [HerdItem("h1", 2019, 60)]);
        var model = CreateModel(VlaMaintenance);
        model.EditingClientKey = "h1";
        model.EditHerdRow = new FarmModel.HerdSizeRowInput { HerdYear = 1900, TotalSize = 70 };

        var result = await model.OnPostUpdateHerdSizeRowAsync();

        result.Should().BeOfType<PageResult>();
        model.ReopenEditClientKey.Should().Be("h1");
        model.ModelState.Should().ContainKey("EditHerdRow.HerdYear");
        _draft!.HerdSizes.Single().HerdYear.Should().Be(2019);
    }

    [Fact]
    public async Task DeleteHerdSize_RemovesRowOnlyForKnownKey()
    {
        GiveBatchHistory();
        SeedDraft(herds: [HerdItem("h1"), HerdItem("h2", 2021)]);

        await CreateModel(VlaMaintenance).OnPostDeleteHerdSizeAsync("zzz");
        _draft!.HerdSizes.Should().HaveCount(2);

        var result = await CreateModel(VlaMaintenance).OnPostDeleteHerdSizeAsync("h1");

        result.Should().BeOfType<RedirectToPageResult>();
        _draft.HerdSizes.Should().ContainSingle().Which.ClientKey.Should().Be("h2");
        _draft.HasFarmCollectionsChanges.Should().BeTrue();
    }

    // ── Lookup in create mode ──

    [Fact]
    public async Task LookupNewCase_NotAllowed_Forbids()
    {
        (await CreateModel(DefraViewer, NewRbse).OnPostLookupNewCaseAsync()).Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task LookupNewCase_NonGbCphh_RejectedWithError()
    {
        var model = CreateModel(DataEntry, NewRbse);
        model.EditableFarm = new FarmEditViewModel { CPHH = "00/001/0002/02" };

        var result = await model.OnPostLookupNewCaseAsync();

        result.Should().BeOfType<PageResult>();
        model.ModelState["EditableFarm.CPHH"]!.Errors.Single().ErrorMessage.Should().Contain("non-GB");
        model.EditableFarm.CPHH.Should().Be("00001000202");
    }

    [Fact]
    public async Task LookupNewCase_ExistingFarm_LoadsItAndStagesIt()
    {
        _farms.GetByCphhAsync(NewCphh).Returns(MakeFarm(NewCphh));
        var model = CreateModel(DataEntry, NewRbse);
        model.EditableFarm = new FarmEditViewModel { CPHH = "01/001/0002/02" };

        var result = await model.OnPostLookupNewCaseAsync();

        result.Should().BeOfType<PageResult>();
        model.RequireFarmDetails.Should().BeFalse();
        model.EditableFarm!.CPHH.Should().Be(NewCphh);
        model.EditableFarm.OwnerName.Should().Be("Owner");
        model.EditableFarmRowStampBase64.Should().Be(Stamp);
        model.Farm!.CPHH.Should().Be(NewCphh);
        _draft!.Farm!.CPHH.Should().Be(NewCphh);
    }

    [Fact]
    public async Task LookupNewCase_UnknownFarm_RedirectsToPickFarm()
    {
        var model = CreateModel(DataEntry, NewRbse);
        model.EditableFarm = new FarmEditViewModel { CPHH = NewCphh };

        var result = await model.OnPostLookupNewCaseAsync();

        var redirect = result.Should().BeOfType<RedirectToPageResult>().Subject;
        redirect.PageName.Should().Be("/Case/PickFarm");
        redirect.RouteValues!["cphh"].Should().Be(NewCphh);
        redirect.RouteValues["rbse"].Should().Be(NewRbse);
    }

    [Fact]
    public async Task LookupNewCase_BlankCphh_RedirectsToPickFarm()
    {
        var model = CreateModel(DataEntry, NewRbse);
        model.EditableFarm = new FarmEditViewModel();

        var result = await model.OnPostLookupNewCaseAsync();

        result.Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/Case/PickFarm");
    }

    // ── Create case ──

    private FarmModel CreateCaseModel(Action<FarmEditViewModel>? tweak = null)
    {
        var model = CreateModel(DataEntry, NewRbse);
        model.EditableFarm = new FarmEditViewModel { CPHH = NewCphh };
        tweak?.Invoke(model.EditableFarm);
        model.NewEartagCountry = "UK";
        model.NewEartagHerdmark = "HM";
        model.NewEartag = "123";
        model.NewSex = "F";
        model.NewBirthDate = new DateTime(2020, 5, 1);
        return model;
    }

    [Fact]
    public async Task CreateCase_NotAllowed_Forbids()
    {
        (await CreateModel(DefraViewer, NewRbse).OnPostCreateCaseAsync()).Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task CreateCase_BlankCphh_ReportsRequiredErrors()
    {
        var model = CreateModel(DataEntry, NewRbse);
        model.EditableFarm = new FarmEditViewModel();

        var result = await model.OnPostCreateCaseAsync();

        result.Should().BeOfType<PageResult>();
        model.ModelState.Keys.Should().Contain([
            "EditableFarm.CPHH", "EditableFarm.OwnerName", "EditableFarm.Address1", "EditableFarm.Parish",
            "EditableFarm.County", "EditableFarm.AHO", "EditableFarm.ADNSRegionID"]);
        await _orchestration.DidNotReceiveWithAnyArgs().CommitAllAsync(default!, default);
    }

    [Fact]
    public async Task CreateCase_NewFarmWithAllDetails_StagesFarmAndCaseThenCommits()
    {
        var model = CreateCaseModel(f =>
        {
            f.OwnerName = "O"; f.Address1 = "A"; f.Parish = "P"; f.County = "KT"; f.AHO = "A1"; f.ADNSRegionID = 5;
        });

        var result = await model.OnPostCreateCaseAsync();

        result.Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/Home");
        _draft!.Farm!.CPHH.Should().Be(NewCphh);
        _draft.Farm.OwnerName.Should().Be("O");
        _draft.Case!.Rbse.Should().Be(NewRbse);
        _draft.Case.Eartag.Should().Be("123");
        _draft.Case.EartagCountry.Should().Be("UK");
        _draft.Case.Sex.Should().Be("F");
        _draft.Case.BirthDate.Should().Be(new DateTime(2020, 5, 1));
        _draft.Case.IsBirthDateEst.Should().BeFalse();
        _draft.HasPendingChanges.Should().BeTrue();
        await _orchestration.Received(1).CommitAllAsync(NewRbse, 4);
    }

    [Fact]
    public async Task CreateCase_ExistingFarm_SkipsFarmDetailValidation()
    {
        _farms.GetByCphhAsync(NewCphh).Returns(MakeFarm(NewCphh));
        var model = CreateCaseModel();

        var result = await model.OnPostCreateCaseAsync();

        result.Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/Home");
        model.RequireFarmDetails.Should().BeFalse();
        _draft!.Farm!.CPHH.Should().Be(NewCphh);
    }

    [Fact]
    public async Task CreateCase_NoBirthDate_LeavesEstimateFlagNull()
    {
        _farms.GetByCphhAsync(NewCphh).Returns(MakeFarm(NewCphh));
        var model = CreateCaseModel();
        model.NewBirthDate = null;

        await model.OnPostCreateCaseAsync();

        _draft!.Case!.IsBirthDateEst.Should().BeNull();
    }

    [Fact]
    public async Task CreateCase_CaseAlreadyStaged_IsNotOverwritten()
    {
        _farms.GetByCphhAsync(NewCphh).Returns(MakeFarm(NewCphh));
        var staged = new CaseEditViewModel { Rbse = NewRbse, Eartag = "STAGED" }.ToEditCommand([]);
        _draft = new CaseEditSessionState { Rbse = NewRbse, Case = staged };
        var model = CreateCaseModel();

        await model.OnPostCreateCaseAsync();

        _draft!.Case!.Eartag.Should().Be("STAGED");
    }

    [Fact]
    public async Task CreateCase_NonGbCphh_DoesNotLookUpFarmOrRequireDetails()
    {
        var model = CreateCaseModel(f => f.CPHH = "00001000202");

        var result = await model.OnPostCreateCaseAsync();

        result.Should().BeOfType<RedirectToPageResult>();
        model.RequireFarmDetails.Should().BeFalse();
        _draft!.Farm!.CPHH.Should().Be("00001000202");
    }

    [Fact]
    public async Task CreateCase_BlankFormCphh_FallsBackToNewCphhButStillFlagsRequiredCphh()
    {
        _farms.GetByCphhAsync(NewCphh).Returns(MakeFarm(NewCphh));
        var model = CreateCaseModel(f => f.CPHH = "");
        model.NewCphh = NewCphh;

        var result = await model.OnPostCreateCaseAsync();

        result.Should().BeOfType<PageResult>();
        model.ModelState.Should().ContainKey("EditableFarm.CPHH");
        model.ModelState.Should().NotContainKey("EditableFarm.OwnerName");
        model.EditableFarm!.CPHH.Should().Be(NewCphh);
    }

    [Fact]
    public async Task CreateCase_InsertFails_ShowsInlineError()
    {
        _farms.GetByCphhAsync(NewCphh).Returns(MakeFarm(NewCphh));
        _orchestration.CommitAllAsync(NewRbse, 4).Returns<Task<CaseCommitOutcome>>(_ => throw new InvalidOperationException("duplicate rbse"));
        var model = CreateCaseModel();

        var result = await model.OnPostCreateCaseAsync();

        result.Should().BeOfType<PageResult>();
        model.ModelState[string.Empty]!.Errors.Single().ErrorMessage.Should().Be("duplicate rbse");
    }

    [Fact]
    public async Task CreateCase_CommitFailure_RedirectsHomeWithError()
    {
        _farms.GetByCphhAsync(NewCphh).Returns(MakeFarm(NewCphh));
        _orchestration.CommitAllAsync(NewRbse, 4).Returns(CaseCommitOutcome.Failure(EditCaseResult.PostUpdateError));
        var model = CreateCaseModel();

        var result = await model.OnPostCreateCaseAsync();

        result.Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/Home");
        model.TempData["ErrorMessage"]!.ToString().Should().StartWith("Unable to create case");
    }

    [Fact]
    public async Task CreateCase_MandatoryMissing_RedirectsToSaveResult()
    {
        _farms.GetByCphhAsync(NewCphh).Returns(MakeFarm(NewCphh));
        _orchestration.CommitAllAsync(NewRbse, 4).Returns<Task<CaseCommitOutcome>>(_ => throw new MandatoryCaseFieldsMissingException(["x"]));
        var model = CreateCaseModel();

        var result = await model.OnPostCreateCaseAsync();

        result.Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/Case/SaveResult");
    }

    [Fact]
    public async Task CreateCase_Warnings_RedirectsToSaveResult()
    {
        _farms.GetByCphhAsync(NewCphh).Returns(MakeFarm(NewCphh));
        _orchestration.CommitAllAsync(NewRbse, 4).Returns(CaseCommitOutcome.Success(["w"]));
        var model = CreateCaseModel();

        var result = await model.OnPostCreateCaseAsync();

        result.Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/Case/SaveResult");
    }

    [Fact]
    public async Task CreateCase_InvalidStagedHerdSize_ReturnsPage()
    {
        _farms.GetByCphhAsync(NewCphh).Returns(MakeFarm(NewCphh));
        _draft = new CaseEditSessionState
        {
            Rbse = NewRbse, Cphh = NewCphh, HasFarmCollectionsChanges = true,
            HerdSizes = [HerdItem("h", year: 1900)]
        };
        var model = CreateCaseModel();

        var result = await model.OnPostCreateCaseAsync();

        result.Should().BeOfType<PageResult>();
        await _orchestration.DidNotReceiveWithAnyArgs().CommitAllAsync(default!, default);
    }

    // ── AJAX handlers ──

    [Theory]
    [InlineData(null)]
    [InlineData("  ")]
    [InlineData("0100100020201")]
    public async Task LinkedFarmStatus_BlankOrTooLong_ReturnsNull(string? cphh)
    {
        var result = await CreateModel(DataEntry).OnGetLinkedFarmStatusAsync(cphh);

        Json(result).Should().Be("{\"status\":null}");
    }

    [Fact]
    public async Task LinkedFarmStatus_KnownFarm_ReturnsOwnerAndAddress()
    {
        var result = await CreateModel(DataEntry).OnGetLinkedFarmStatusAsync("01/001/0001/01");

        Json(result).Should().Be("{\"status\":\"Owner, 1 Road\"}");
    }

    [Fact]
    public async Task LinkedFarmStatus_UnknownFarm_ReturnsBseFree()
    {
        var result = await CreateModel(DataEntry).OnGetLinkedFarmStatusAsync(NewCphh);

        Json(result).Should().Be("{\"status\":\"BSE Free\"}");
    }

    [Theory]
    [InlineData(null)]
    [InlineData(0)]
    public async Task Authorities_NoCounty_ReturnsEmptyArray(int? id)
    {
        Json(await CreateModel(DataEntry).OnGetAuthoritiesAsync(id)).Should().Be("[]");
        Json(await CreateModel(DataEntry).OnGetAdnsRegionsAsync(id)).Should().Be("[]");
    }

    [Fact]
    public async Task Authorities_And_AdnsRegions_ReturnIdAndName()
    {
        Json(await CreateModel(DataEntry).OnGetAuthoritiesAsync(2)).Should().Be("[{\"id\":3,\"name\":\"Auth\"}]");
        Json(await CreateModel(DataEntry).OnGetAdnsRegionsAsync(3)).Should().Be("[{\"id\":5,\"name\":\"Reg\"}]");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("01")]
    public async Task EstimateMapReference_ShortCphh_ReturnsError(string? cphh)
    {
        Json(await CreateModel(DataEntry).OnGetEstimateMapReferenceAsync(cphh)).Should().Contain("at least 5 characters");
        await _geo.DidNotReceiveWithAnyArgs().EstimateMapReferenceAsync(default!);
    }

    [Fact]
    public async Task EstimateMapReference_ServiceErrorAndSuccess()
    {
        _geo.EstimateMapReferenceAsync(NewCphh).Returns(new MapReferenceEstimate(null, null, null, "no data"));
        Json(await CreateModel(DataEntry).OnGetEstimateMapReferenceAsync(NewCphh)).Should().Contain("no data");

        _geo.EstimateMapReferenceAsync(NewCphh).Returns(new MapReferenceEstimate("SU", "123", "456", null));
        Json(await CreateModel(DataEntry).OnGetEstimateMapReferenceAsync(NewCphh))
            .Should().Be("{\"mapRef1\":\"SU\",\"mapRef2\":\"123\",\"mapRef3\":\"456\"}");
    }

    [Theory]
    [InlineData(null, "SU123456")]
    [InlineData(NewCphh, null)]
    [InlineData("01", "SU123456")]
    [InlineData(NewCphh, "SU12")]
    public async Task ValidateMapReference_MissingOrShort_IsValidWithoutLookup(string? cphh, string? mapRef)
    {
        Json(await CreateModel(DataEntry).OnGetValidateMapReferenceAsync(cphh, mapRef)).Should().Be("{\"valid\":true}");
        await _geo.DidNotReceiveWithAnyArgs().IsMapReferenceWithinParishAsync(default!, default!);
    }

    [Fact]
    public async Task ValidateMapReference_InsideAndOutsideParish()
    {
        _geo.IsMapReferenceWithinParishAsync(NewCphh, "SU123456").Returns(true);
        Json(await CreateModel(DataEntry).OnGetValidateMapReferenceAsync(NewCphh, " su123456 ")).Should().Be("{\"valid\":true}");

        _geo.IsMapReferenceWithinParishAsync(NewCphh, "SU123456").Returns(false);
        Json(await CreateModel(DataEntry).OnGetValidateMapReferenceAsync(NewCphh, "SU123456"))
            .Should().Contain("\"valid\":false").And.Contain("outside the parish");
    }
}
