using BSE.Host.Models.ViewModels;
using BSE.Host.Pages.Admin;
using BSE.Host.Pages.Case;
using BSE.Host.Services;
using BSE.Infrastructure;
using BSE.Modules.Batch.Repositories;
using BSE.Modules.Batch.Services;
using BSE.Modules.CaseManagement.Models;
using BSE.Modules.CaseManagement.Repositories;
using BSE.Modules.CaseManagement.Services;
using BSE.Modules.FarmManagement.Models;
using BSE.Modules.FarmManagement.Repositories;
using BSE.Modules.FarmManagement.Services;
using BSE.Modules.ReferenceData.Models;
using BSE.Modules.ReferenceData.Services;
using BSE.Modules.UserManagement.Models;
using BSE.Modules.UserManagement.Services;
using BSE.SharedKernel;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using System.Reflection;

namespace BSE.Modules.UserManagement.Tests;

public sealed class PageModelCoverageTests
{
    [Fact]
    public async Task UsersModel_OnGetAsync_LoadsUserGroupsAndEditValues()
    {
        var userService = Substitute.For<IUserManagementService>();
        var lookupData = Substitute.For<ILookupDataService>();
        var user = new User(3, "alice1", "alice@example.com", "Alice Example", "alice@example.com", true, 1, UserGroup.Admin);

        userService.GetAllUsersAsync().Returns(new[] { user });
        lookupData.GetUserGroupsAsync().Returns(new[] { new LuUserGroup { Id = 1, Name = "Admin" } });

        var model = new UsersModel(userService, lookupData)
        {
            PageContext = new PageContext { HttpContext = new DefaultHttpContext() },
            EditUserId = 3,
            SortColumn = "UserName",
            PageNumber = 1
        };

        var result = await model.OnGetAsync();

        result.Should().BeOfType<PageResult>();
        model.Users.Should().ContainSingle();
        model.EditUserName.Should().Be("Alice Example");
        model.EditEmail.Should().Be("alice@example.com");
        model.EditUserGroupId.Should().Be(1);
    }

    [Fact]
    public async Task UsersModel_OnPostStartNewAsync_RedirectsToLastPage()
    {
        var userService = Substitute.For<IUserManagementService>();
        var lookupData = Substitute.For<ILookupDataService>();
        userService.GetAllUsersAsync().Returns(new[]
        {
            new User(1, "a", "a@example.com", "A", "a@example.com", true, 2, UserGroup.DataEntry),
            new User(2, "b", "b@example.com", "B", "b@example.com", true, 2, UserGroup.DataEntry),
            new User(3, "c", "c@example.com", "C", "c@example.com", true, 2, UserGroup.DataEntry)
        });

        var model = new UsersModel(userService, lookupData)
        {
            PageContext = new PageContext { HttpContext = new DefaultHttpContext() },
            PageNumber = 1,
            SortColumn = "UserName",
            SortDesc = true
        };

        var result = await model.OnPostStartNewAsync();

        result.Should().BeOfType<RedirectToPageResult>();
        var redirect = (RedirectToPageResult)result;
        redirect.RouteValues.Should().ContainKey("IsAddingNew").WhoseValue.Should().Be(true);
        redirect.RouteValues.Should().ContainKey("PageNumber").WhoseValue.Should().Be(1);
    }

    [Fact]
    public async Task UsersModel_OnPostEditAsync_AddsNewUserAndRedirects()
    {
        var userService = Substitute.For<IUserManagementService>();
        var lookupData = Substitute.For<ILookupDataService>();
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Form = new FormCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
        {
            [nameof(UsersModel.EditIsActive)] = "true"
        });

        userService.GetAllUsersAsync().Returns(Array.Empty<User>());
        lookupData.GetUserGroupsAsync().Returns(new[] { new LuUserGroup { Id = 2, Name = "Data Entry" } });

        var model = new UsersModel(userService, lookupData)
        {
            PageContext = new PageContext { HttpContext = httpContext },
            TempData = new TempDataDictionary(httpContext, Substitute.For<ITempDataProvider>()),
            EditUserName = "  New Person  ",
            EditEmail = "newperson@example.com",
            EditUserGroupId = 2,
            EditUpn = "newperson@bse.local",
            EditNTLogin = string.Empty,
            SortColumn = "UserName",
            PageNumber = 1
        };

        var result = await model.OnPostEditAsync();

        result.Should().BeOfType<RedirectToPageResult>();
        await userService.Received(1).AddUserAsync(Arg.Is<User>(u =>
            u.UserName == "New Person" &&
            u.Email == "newperson@example.com" &&
            u.NTLogin == "newperson" &&
            u.UserGroupId == 2));
        model.TempData["Success"].Should().Be("User 'New Person' added.");
    }

    [Fact]
    public async Task FarmHerdSizeAddModel_OnPostAsync_RejectsInvalidModelState()
    {
        var caseService = Substitute.For<ICaseService>();
        var herdSizeRepo = Substitute.For<IHerdSizeRepository>();
        var httpContext = new DefaultHttpContext();

        caseService.GetCaseAsync("002600001").Returns(new CaseRecord { Cphh = "01001000101" });
        herdSizeRepo.GetByCphhAsync("01001000101").Returns(Array.Empty<HerdSizeRecord>());

        var model = new FarmHerdSizeAddModel(caseService, herdSizeRepo)
        {
            PageContext = new PageContext { HttpContext = httpContext },
            TempData = new TempDataDictionary(httpContext, Substitute.For<ITempDataProvider>()),
            Rbse = "002600001",
            HerdSize = new FarmModel.HerdSizeFormViewModel
            {
                HerdYear = 2030,
                TotalSize = 0,
                Lactation1Size = 7,
                Lactation2Size = 3
            }
        };

        var result = await model.OnPostAsync();

        result.Should().BeOfType<PageResult>();
        model.ModelState.Should().ContainKey("HerdSize.HerdYear");
        model.ModelState.Should().ContainKey("HerdSize.TotalSize");
    }

    [Fact]
    public async Task FarmHerdSizeAddModel_OnPostAsync_SavesValidEntryAndSetsSuccessMessage()
    {
        var caseService = Substitute.For<ICaseService>();
        var herdSizeRepo = Substitute.For<IHerdSizeRepository>();
        var httpContext = new DefaultHttpContext();

        caseService.GetCaseAsync("002600001").Returns(new CaseRecord { Cphh = "01001000101" });
        herdSizeRepo.GetByCphhAsync("01001000101").Returns(Array.Empty<HerdSizeRecord>());

        var model = new FarmHerdSizeAddModel(caseService, herdSizeRepo)
        {
            PageContext = new PageContext { HttpContext = httpContext },
            TempData = new TempDataDictionary(httpContext, Substitute.For<ITempDataProvider>()),
            Rbse = "002600001",
            HerdSize = new FarmModel.HerdSizeFormViewModel
            {
                HerdYear = 2024,
                TotalSize = 20,
                Lactation1Size = 10,
                Lactation2Size = 10
            }
        };

        var result = await model.OnPostAsync();

        result.Should().BeOfType<RedirectToPageResult>();
        await herdSizeRepo.Received(1).AddAsync(Arg.Is<AddHerdSizeCommand>(c =>
            c.CPHH == "01001000101" &&
            c.HerdYear == 2024 &&
            c.TotalSize == 20 &&
            c.Lactation1Size == 10 &&
            c.Lactation2Size == 10));
        model.TempData["Success"].Should().Be("Herd size for 2024 added.");
    }

    [Fact]
    public async Task FarmHerdSizeEditModel_OnGetAsync_LoadsExistingRowAndStamp()
    {
        var caseService = Substitute.For<ICaseService>();
        var farmService = Substitute.For<IFarmService>();
        var herdSizeRepo = Substitute.For<IHerdSizeRepository>();
        var stamp = new byte[] { 1, 2, 3, 4 };

        caseService.GetCaseAsync("002600001").Returns(new CaseRecord { Cphh = "01001000101" });
        farmService.GetHerdSizesAsync("01001000101").Returns(new[]
        {
            new HerdSizeRecord
            {
                ID = 5,
                CPHH = "01001000101",
                HerdYear = 2024,
                TotalSize = 55,
                Lactation1Size = 20,
                Lactation2Size = 30,
                Lactation10PlusSize = 5,
                RowStamp = stamp
            }
        });

        var model = new FarmHerdSizeEditModel(caseService, farmService, herdSizeRepo)
        {
            PageContext = new PageContext { HttpContext = new DefaultHttpContext() },
            Rbse = "002600001",
            Id = 5
        };

        var result = await model.OnGetAsync();

        result.Should().BeOfType<PageResult>();
        model.HerdSize.HerdYear.Should().Be(2024);
        model.HerdSize.TotalSize.Should().Be(55);
        model.RowStampBase64.Should().Be(Convert.ToBase64String(stamp));
    }

    [Fact]
    public async Task FarmHerdSizeEditModel_OnPostAsync_UpdatesRecordAndSetsSuccessMessage()
    {
        var caseService = Substitute.For<ICaseService>();
        var farmService = Substitute.For<IFarmService>();
        var herdSizeRepo = Substitute.For<IHerdSizeRepository>();
        var httpContext = new DefaultHttpContext();

        caseService.GetCaseAsync("002600001").Returns(new CaseRecord { Cphh = "01001000101" });

        var model = new FarmHerdSizeEditModel(caseService, farmService, herdSizeRepo)
        {
            PageContext = new PageContext { HttpContext = httpContext },
            TempData = new TempDataDictionary(httpContext, Substitute.For<ITempDataProvider>()),
            Rbse = "002600001",
            Id = 9,
            RowStampBase64 = Convert.ToBase64String(new byte[] { 7, 8 }),
            HerdSize = new FarmModel.HerdSizeFormViewModel
            {
                HerdYear = 2023,
                TotalSize = 18,
                Lactation1Size = 9,
                Lactation2Size = 9
            }
        };

        var result = await model.OnPostAsync();

        result.Should().BeOfType<RedirectToPageResult>();
        await herdSizeRepo.Received(1).UpdateAsync(Arg.Is<UpdateHerdSizeCommand>(c =>
            c.ID == 9 &&
            c.HerdYear == 2023 &&
            c.TotalSize == 18 &&
            c.Lactation1Size == 9 &&
            c.Lactation2Size == 9));
        model.TempData["Success"].Should().Be("Herd size for 2023 updated.");
    }

    [Fact]
    public void VlaModel_SelectedIfMatches_ReturnsSelected_WhenValueMatchesIgnoringCase()
    {
        VlaModel.SelectedIfMatches("Previous", "previous").Should().Be("selected");
        VlaModel.SelectedIfMatches("Buyer", "seller").Should().BeNull();
    }

    [Fact]
    public void VlaModel_ValidateOwnerRow_AddsRequiredFieldErrors()
    {
        var model = CreateVlaModel();

        InvokePrivateVoid(model, "ValidateOwnerRow", new object?[] { null, null, null, null });

        model.ModelState.Should().ContainKey(nameof(VlaModel.NewOwnerType));
        model.ModelState.Should().ContainKey(nameof(VlaModel.NewOwnerName));

        var ownerTypeState = model.ModelState[nameof(VlaModel.NewOwnerType)];
        var ownerNameState = model.ModelState[nameof(VlaModel.NewOwnerName)];

        ownerTypeState.Should().NotBeNull();
        ownerNameState.Should().NotBeNull();
        ownerTypeState!.Errors.Should().Contain(e => e.ErrorMessage == "Owner type is required.");
        ownerNameState!.Errors.Should().Contain(e => e.ErrorMessage == "You must enter either an owner name or a CPHH.");
    }

    [Fact]
    public void VlaModel_ValidatePreviousOwnerUniqueness_RejectsDuplicatePreviousType()
    {
        var model = CreateVlaModel();
        SetPrivateProperty(model, "StagedOtherOwners", new List<CaseEditDraftOtherOwnerItem>
        {
            new() { Id = 11, Type = "Previous", Name = "Old owner" }
        });

        InvokePrivateVoid(model, "ValidatePreviousOwnerUniqueness", new object?[] { "Previous", null });

        model.ModelState.Should().ContainKey(nameof(VlaModel.NewOwnerType));
        var ownerTypeState = model.ModelState[nameof(VlaModel.NewOwnerType)];
        ownerTypeState.Should().NotBeNull();
        ownerTypeState!.Errors.Should().Contain(e => e.ErrorMessage == "You can only have one owner of type Previous.");
    }

    [Fact]
    public void VlaModel_ValidateVlaDomainRules_AddsExpectedValidationErrors()
    {
        var model = CreateVlaModel();
        model.Case = new VlaEditViewModel
        {
            FormADate = new DateTime(2024, 7, 15),
            BirthDate = new DateTime(2024, 7, 10),
            PurchaseDate = new DateTime(2024, 7, 9),
            HerdEntryDate = new DateTime(2024, 7, 20),
            OnsetDate = new DateTime(2024, 7, 5),
            MonthsPregnant = 10,
            MonthsPostCalving = 4,
            SlaughterDate = new DateTime(2024, 7, 1)
        };

        InvokePrivateVoid(model, "ValidateVlaDomainRules", Array.Empty<object?>());

        model.ModelState.Should().ContainKey("Case.PurchaseDate");
        model.ModelState.Should().ContainKey("Case.HerdEntryDate");
        model.ModelState.Should().ContainKey("Case.OnsetDate");
        model.ModelState.Should().ContainKey("Case.MonthsPregnant");
        model.ModelState.Should().ContainKey("Case.MonthsPostCalving");
        model.ModelState.Should().ContainKey("Case.SlaughterDate");
        model.ModelState.Should().NotContainKey("Case.BirthDate");
    }

    [Fact]
    public void FarmModel_SortedStagedHerdSizes_OrdersAndPaginatesAsExpected()
    {
        var model = CreateFarmModel();
        model.HSort = "year";
        model.HDir = "desc";
        model.StagedHerdSizes = new List<FarmModel.StagedHerdSizeItem>
        {
            new() { HerdYear = 2023, TotalSize = 10 },
            new() { HerdYear = 2025, TotalSize = 40 },
            new() { HerdYear = 2024, TotalSize = 30 }
        };

        model.SortedStagedHerdSizes().Select(x => x.HerdYear).Should().Equal(2025, 2024, 2023);
        model.PagedSortedStagedHerdSizes().Select(x => x.HerdYear).Should().Equal(2025, 2024, 2023);
        model.HerdSizeSortUrl("total").Should().Contain("HSort=total").And.Contain("HDir=asc");
        model.HerdSizeAriaSort("year").Should().Be("descending");
        model.LinkedFarmAriaSort("status").Should().Be("none");
    }

    private static VlaModel CreateVlaModel()
    {
        var caseService = Substitute.For<ICaseService>();
        var babRepository = Substitute.For<IBabRepository>();
        var currentUserService = Substitute.For<ICurrentUserService>();
        var wizardState = Substitute.For<ICaseWizardStateService>();
        var caseEditDraftState = Substitute.For<ICaseEditDraftStateService>();
        var caseScalarDraftState = Substitute.For<ICaseScalarDraftStateService>();
        var caseEditOrchestration = Substitute.For<ICaseEditOrchestrationService>();
        var lookupService = Substitute.For<ILookupDataService>();
        var batchRepository = Substitute.For<IBatchRepository>();
        var ownerRepository = Substitute.For<IOtherOwnerRepository>();
        var connectionFactory = Substitute.For<IDbConnectionFactory>();
        var configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();

        var model = new VlaModel(
            caseService,
            babRepository,
            currentUserService,
            wizardState,
            caseEditDraftState,
            caseScalarDraftState,
            caseEditOrchestration,
            lookupService,
            batchRepository,
            ownerRepository,
            connectionFactory,
            configuration);

        SetPrivateProperty(model, "OwnerTypeOptions", new ILookupItem[]
        {
            new LookupItem { Code = "Previous", Description = "Previous owner" },
            new LookupItem { Code = "Buyer", Description = "Buyer" }
        });

        var httpContext = new DefaultHttpContext();
        httpContext.User = new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity(new[]
        {
            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, "DataEntry"),
            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, "VLAAccess")
        }, "TestAuth"));
        model.PageContext = new PageContext { HttpContext = httpContext };

        return model;
    }

    private static FarmModel CreateFarmModel()
    {
        var caseService = Substitute.For<ICaseService>();
        var farmService = Substitute.For<IFarmService>();
        var relationRepo = Substitute.For<IFarmRelationRepository>();
        var herdSizeRepo = Substitute.For<IHerdSizeRepository>();
        var lookups = Substitute.For<ILookupDataService>();
        var batchRepository = Substitute.For<IBatchRepository>();
        var wizardState = Substitute.For<ICaseWizardStateService>();
        var farmDraftState = Substitute.For<ICaseFarmDraftStateService>();
        var caseScalarDraftState = Substitute.For<ICaseScalarDraftStateService>();
        var caseEditOrchestration = Substitute.For<ICaseEditOrchestrationService>();
        var currentUser = Substitute.For<ICurrentUserService>();
        var geoLookup = Substitute.For<BSE.Host.Services.IGeoLookupService>();

        return new FarmModel(
            caseService,
            farmService,
            relationRepo,
            herdSizeRepo,
            lookups,
            batchRepository,
            wizardState,
            farmDraftState,
            caseScalarDraftState,
            caseEditOrchestration,
            currentUser,
            NullLogger<FarmModel>.Instance,
            new ConfigurationBuilder().AddInMemoryCollection().Build(),
            geoLookup);
    }

    private static void SetPrivateProperty<T>(object target, string propertyName, T value)
    {
        var property = target.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        property.Should().NotBeNull();
        property!.SetValue(target, value);
    }

    private static void InvokePrivateVoid(object target, string methodName, object?[]? args)
    {
        var method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        method.Should().NotBeNull();
        method!.Invoke(target, args ?? Array.Empty<object?>());
    }
}
