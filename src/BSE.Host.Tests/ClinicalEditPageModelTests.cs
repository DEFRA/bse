using BSE.Host.Pages.Case;
using BSE.Infrastructure;
using BSE.Modules.Batch.Models;
using BSE.Modules.Batch.Repositories;
using BSE.Modules.CaseManagement.Commands;
using BSE.Modules.CaseManagement.Models;
using BSE.Modules.CaseManagement.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;
using System.Data;

namespace BSE.Host.Tests;

public sealed class ClinicalEditPageModelTests
{
    private const string Rbse = "002600001";

    private readonly IClinicalRepository _clinical = Substitute.For<IClinicalRepository>();
    private readonly IBatchRepository _batch = Substitute.For<IBatchRepository>();
    private readonly IDbConnectionFactory _factory = Substitute.For<IDbConnectionFactory>();
    private readonly IDbConnection _connection = Substitute.For<IDbConnection>();
    private readonly IDbTransaction _transaction = Substitute.For<IDbTransaction>();

    public ClinicalEditPageModelTests()
    {
        _batch.GetBatchNumbersByRbseAsync(Rbse).Returns((IReadOnlyList<BatchNumberEntry>)[new BatchNumberEntry(1, "B1", Rbse, "BSE1")]);
        _connection.BeginTransaction().Returns(_transaction);
        _factory.CreateConnection().Returns(_connection);
    }

    private ClinicalEditModel CreateModel(params string[] checkedSigns)
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["SpolSiteUrl"] = "http://spol" }).Build();
        var model = new ClinicalEditModel(_clinical, _batch, _factory, config) { Rbse = Rbse };
        var http = new DefaultHttpContext();
        http.Request.Form = new FormCollection(
            checkedSigns.ToDictionary(s => $"Signs.{s}", _ => new StringValues("true")));
        model.PageContext = new PageContext { HttpContext = http };
        model.TempData = new TempDataDictionary(http, Substitute.For<ITempDataProvider>());
        return model;
    }

    [Fact]
    public async Task OnGet_WithClinicalRecord_PopulatesSignsAndRowStamp()
    {
        _clinical.GetByRbseAsync(Rbse).Returns(new CaseClinicalRecord { Rbse = Rbse, Apprehension = true, MilkYield = true, Tremor = true, RowStamp = [5, 6] });
        var model = CreateModel();

        var result = await model.OnGetAsync();

        result.Should().BeOfType<PageResult>();
        model.Signs.Apprehension.Should().BeTrue();
        model.Signs.MilkYield.Should().BeTrue();
        model.Signs.Tremor.Should().BeTrue();
        model.Signs.Kicking.Should().BeFalse();
        model.ClinicalRowStampBase64.Should().Be(Convert.ToBase64String(new byte[] { 5, 6 }));
        model.BatchNumbers.Should().ContainSingle();
        model.SpolSiteUrl.Should().Be("http://spol");
    }

    [Fact]
    public async Task OnGet_RecordWithoutRowStamp_LeavesRowStampNull()
    {
        _clinical.GetByRbseAsync(Rbse).Returns(new CaseClinicalRecord { Rbse = Rbse });

        var model = CreateModel();
        await model.OnGetAsync();

        model.ClinicalRowStampBase64.Should().BeNull();
    }

    [Fact]
    public async Task OnGet_NoClinicalRecord_UsesEmptySigns()
    {
        _clinical.GetByRbseAsync(Rbse).Returns((CaseClinicalRecord?)null);
        var model = CreateModel();

        await model.OnGetAsync();

        model.Signs.Apprehension.Should().BeFalse();
        model.ClinicalRowStampBase64.Should().BeNull();
        model.BatchNumbers.Should().ContainSingle();
    }

    [Fact]
    public async Task SaveSigns_WithRowStamp_EditsExistingRecordInTransaction()
    {
        var model = CreateModel("Apprehension", "Kicking", "ConditionLoss");

        var result = await model.OnPostSaveSignsAsync(Convert.ToBase64String(new byte[] { 5, 6 }));

        var redirect = result.Should().BeOfType<RedirectToPageResult>().Subject;
        redirect.PageName.Should().Be("/Case/Clinical");
        redirect.RouteValues!["rbse"].Should().Be(Rbse);
        model.TempData["Success"].Should().Be("Clinical signs saved.");
        await _clinical.Received(1).EditAsync(
            Arg.Is<EditCaseClinicalCommand>(c => c.Rbse == Rbse && c.Apprehension && c.Kicking && c.ConditionLoss
                && !c.Tremor && c.RowStamp.SequenceEqual(new byte[] { 5, 6 })),
            _connection, _transaction);
        await _clinical.DidNotReceiveWithAnyArgs().AddAsync(default!, default!, default!);
        _connection.Received(1).Open();
        _transaction.Received(1).Commit();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task SaveSigns_WithoutRowStamp_AddsNewRecord(string? rowStamp)
    {
        var model = CreateModel("Blindness", "MilkYield");

        var result = await model.OnPostSaveSignsAsync(rowStamp);

        result.Should().BeOfType<RedirectToPageResult>();
        await _clinical.Received(1).AddAsync(
            Arg.Is<AddCaseClinicalCommand>(c => c.Rbse == Rbse && c.Blindness && c.MilkYield && !c.Apprehension),
            _connection, _transaction);
        await _clinical.DidNotReceiveWithAnyArgs().EditAsync(default!, default!, default!);
        _transaction.Received(1).Commit();
    }

    [Fact]
    public async Task SaveSigns_MapsEveryPostedSign()
    {
        var all = new[]
        {
            "Apprehension","HypersensitiveTouch","HypersensitiveSound","Maniacal","PanicStricken","TemperamentChange",
            "AbnormalHeadCarriage","EarTwitching","EarsOddAngle","AbnormalBehaviour","HeadShyness","LickingFlank","LickingNose",
            "Kicking","ReluctantDoorways","HeadPressing","HeadRubbing","TeethGrinding","Blindness","Circling","HindAtaxia",
            "Falling","Paresis","ForeAtaxia","Recumbent","Tremor","KnucklingFetlock","WeightLoss","ConditionLoss","MilkYield"
        };
        var model = CreateModel(all);
        AddCaseClinicalCommand? captured = null;
        await _clinical.AddAsync(Arg.Do<AddCaseClinicalCommand>(c => captured = c), _connection, _transaction);

        await model.OnPostSaveSignsAsync(null);

        captured.Should().NotBeNull();
        var boolProps = typeof(AddCaseClinicalCommand).GetProperties().Where(p => p.PropertyType == typeof(bool)).ToList();
        boolProps.Should().HaveCount(all.Length);
        boolProps.Should().OnlyContain(p => (bool)p.GetValue(captured)!);
    }

    [Fact]
    public async Task SaveSigns_UncheckedOrNonTrueValues_AreFalse()
    {
        var model = CreateModel();
        model.HttpContext.Request.Form = new FormCollection(new Dictionary<string, StringValues> { ["Signs.Apprehension"] = "on" });

        await model.OnPostSaveSignsAsync(null);

        await _clinical.Received(1).AddAsync(Arg.Is<AddCaseClinicalCommand>(c => !c.Apprehension), _connection, _transaction);
    }
}
