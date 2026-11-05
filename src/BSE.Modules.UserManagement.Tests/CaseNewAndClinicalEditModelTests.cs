using System.Data;
using BSE.Host.Pages.Case;
using BSE.Host.Services;
using BSE.Infrastructure;
using BSE.Modules.Batch.Models;
using BSE.Modules.Batch.Repositories;
using BSE.Modules.Batch.Services;
using BSE.Modules.CaseManagement.Commands;
using BSE.Modules.CaseManagement.Enums;
using BSE.Modules.CaseManagement.Models;
using BSE.Modules.CaseManagement.Repositories;
using BSE.Modules.CaseManagement.Services;
using BSE.Modules.FarmManagement.Models;
using BSE.Modules.FarmManagement.Services;
using BSE.Modules.ReferenceData.Models;
using BSE.Modules.ReferenceData.Services;
using BSE.SharedKernel;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using NSubstitute;

namespace BSE.Modules.UserManagement.Tests;

public sealed class CaseNewModelTests
{
    private readonly ICaseService _cases = Substitute.For<ICaseService>();
    private readonly IBatchService _batch = Substitute.For<IBatchService>();
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly IFarmService _farms = Substitute.For<IFarmService>();
    private readonly ILookupDataService _lookups = Substitute.For<ILookupDataService>();

    private NewModel Create()
    {
        _currentUser.GetUserIdAsync().Returns(7);
        _batch.GetOrCreateBatchNumberAsync().Returns(new BatchRecord(42, 2026, 7));
        _lookups.GetLookupAsync(Arg.Any<LookupTableId>()).Returns(Array.Empty<LookupItem>());
        _lookups.GetADNSRegionsAsync().Returns(Array.Empty<LuAdnsRegion>());
        _farms.GetByCphhAsync(Arg.Any<string>()).Returns((FarmRecord?)null);

        var model = new NewModel(_cases, _batch, _currentUser, _farms, _lookups)
        {
            PageContext = new PageContext { HttpContext = new DefaultHttpContext() }
        };
        PageTestHelpers.Attach(model, "DataEntry", "FarmCreation");
        return model;
    }

    [Fact]
    public async Task OnPostAsync_WhenFarmIsNewAndUserLacksFarmCreation_Forbids()
    {
        var model = Create();
        PageTestHelpers.Attach(model, "DataEntry", "VLAAccess");
        model.Rbse = "002600001";
        model.Cphh = "01/001/0007/01";
        model.EartagCountry = "UK";
        model.EartagHerdmark = "H123";
        model.Eartag = "E456";
        model.FormADate = DateTime.Today.AddDays(-2);

        var result = await model.OnPostAsync();

        result.Should().BeOfType<ForbidResult>();
        await _cases.DidNotReceiveWithAnyArgs().CreateCaseAsync(default!, default);
    }

    [Fact]
    public async Task OnPostAsync_WhenBlank_ReportsRequiredFields()
    {
        var model = Create();

        var result = await model.OnPostAsync();

        result.Should().BeOfType<PageResult>();
        model.ModelState.Should().ContainKey(nameof(NewModel.Rbse));
        model.ModelState.Should().ContainKey(nameof(NewModel.Cphh));
        model.ModelState.Should().ContainKey(nameof(NewModel.Eartag));
        model.ModelState.Should().ContainKey(nameof(NewModel.FormADate));
    }

    [Fact]
    public async Task OnPostAsync_WhenCphhIsTooShort_ReportsFormatError()
    {
        var model = Create();
        model.Rbse = "002600001";
        model.Cphh = "123";
        model.EartagCountry = "UK";
        model.EartagHerdmark = "H";
        model.Eartag = "E";
        model.FormADate = DateTime.Today.AddDays(-1);

        var result = await model.OnPostAsync();

        result.Should().BeOfType<PageResult>();
        PageTestHelpers.ErrorMessages(model, nameof(NewModel.Cphh)).Should().Contain(m => m.Contains("Enter CPHH as 11 digits"));
    }

    [Fact]
    public async Task OnPostAsync_WhenValid_AddsFarmAndCreatesCase()
    {
        var model = Create();
        model.Rbse = "002600001";
        model.Cphh = "01/001/0007/01";
        model.EartagCountry = "UK";
        model.EartagHerdmark = "H123";
        model.Eartag = "E456";
        model.FormADate = DateTime.Today.AddDays(-2);
        model.OwnerName = "Owner";
        model.Address1 = "1 Main Street";
        model.Parish = "Test Parish";
        model.County = "BEDS";
        model.Aho = "AHO1";
        model.AdnsRegionId = 5;
        _cases.CreateCaseAsync(Arg.Any<UpdateCaseDetailsCommand>(), 7, Arg.Any<Func<IDbConnection, IDbTransaction, Task>?>()).Returns(AddCaseResult.Success);

        var result = await model.OnPostAsync();

        result.Should().BeOfType<RedirectToPageResult>();
        var enlist = (Func<IDbConnection, IDbTransaction, Task>)_cases.ReceivedCalls().Single().GetArguments()[2]!;
        var conn = Substitute.For<IDbConnection>();
        var tx = Substitute.For<IDbTransaction>();
        await enlist(conn, tx);
        await _farms.Received(1).AddAsync(Arg.Any<AddFarmCommand>(), 7, conn, tx);
        await _farms.DidNotReceive().AddAsync(Arg.Any<AddFarmCommand>(), Arg.Any<int>());
        model.RequireFarmDetails.Should().BeTrue();
        model.TempData["SuccessMessage"].Should().Be("Case 002600001 created successfully.");
    }
}

public sealed class ClinicalEditModelTests
{
    private readonly IClinicalRepository _clinicalRepository = Substitute.For<IClinicalRepository>();
    private readonly IBatchRepository _batchRepository = Substitute.For<IBatchRepository>();
    private readonly IDbConnectionFactory _connectionFactory = Substitute.For<IDbConnectionFactory>();
    private readonly IDbConnection _connection = Substitute.For<IDbConnection>();
    private readonly IDbTransaction _transaction = Substitute.For<IDbTransaction>();
    private readonly IConfiguration _configuration = new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?> { ["SpolSiteUrl"] = "https://spol.example" })
        .Build();

    private ClinicalEditModel Create()
    {
        _connectionFactory.CreateConnection().Returns(_connection);
        _connection.BeginTransaction().Returns(_transaction);
        _batchRepository.GetBatchNumbersByRbseAsync("002600001").Returns(Array.Empty<BatchNumberEntry>());

        var model = new ClinicalEditModel(_clinicalRepository, _batchRepository, _connectionFactory, _configuration)
        {
            Rbse = "002600001",
            PageContext = new PageContext { HttpContext = new DefaultHttpContext() }
        };
        PageTestHelpers.Attach(model, "DataEntry");
        return model;
    }

    [Fact]
    public async Task OnGetAsync_LoadsClinicalSignsAndBatchNumbers()
    {
        _clinicalRepository.GetByRbseAsync("002600001").Returns(new CaseClinicalRecord
        {
            Rbse = "002600001",
            Apprehension = true,
            WeightLoss = true,
            RowStamp = new byte[] { 9, 8, 7 }
        });

        var model = Create();

        var result = await model.OnGetAsync();

        result.Should().BeOfType<PageResult>();
        model.SpolSiteUrl.Should().Be("https://spol.example");
        model.Signs.Apprehension.Should().BeTrue();
        model.Signs.WeightLoss.Should().BeTrue();
        model.ClinicalRowStampBase64.Should().Be(Convert.ToBase64String(new byte[] { 9, 8, 7 }));
    }

    [Fact]
    public async Task OnPostSaveSignsAsync_WhenValid_AddsClinicalRecordAndRedirects()
    {
        var model = Create();
        model.PageContext.HttpContext.Request.Form = new FormCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
        {
            ["Signs.Apprehension"] = "true",
            ["Signs.HypersensitiveTouch"] = "true",
            ["Signs.WeightLoss"] = "true",
            ["Signs.MilkYield"] = "true"
        });

        var result = await model.OnPostSaveSignsAsync(null);

        result.Should().BeOfType<RedirectToPageResult>();
        await _clinicalRepository.Received(1).AddAsync(Arg.Any<AddCaseClinicalCommand>(), _connection, _transaction);
        model.TempData["Success"].Should().Be("Clinical signs saved.");
    }
}
