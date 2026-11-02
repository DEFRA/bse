using System.Globalization;
using BSE.Host.Pages.Bsess;
using BSE.Host.Pages.Case;
using BSE.Modules.BsessIntegration.Models;
using BSE.Modules.BsessIntegration.Services;
using BSE.Modules.CaseManagement.Models;
using BSE.Modules.CaseManagement.Services;
using BSE.SharedKernel;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace BSE.Modules.UserManagement.Tests;

public sealed class RemainingUncoveredPageModelTests
{
    [Fact]
    public async Task CheckByRbse_OnGet_EmptyQuery_AddsValidationError()
    {
        var service = Substitute.For<IBsessCheckService>();
        var model = new CheckByRbseModel(service)
        {
            Rbse = ""
        };
        PageTestHelpers.Attach(model);
        model.PageContext.HttpContext.Request.QueryString = new QueryString("?Rbse=");

        var result = await model.OnGetAsync();

        result.Should().BeOfType<PageResult>();
        model.ModelState.ContainsKey(nameof(CheckByRbseModel.Rbse)).Should().BeTrue();
        model.ModelState[nameof(CheckByRbseModel.Rbse)]!.Errors.Should().Contain(e => e.ErrorMessage == "Enter an RBSE number");
        await service.DidNotReceiveWithAnyArgs().GetCheckByRbseAsync(default!, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CheckByRbse_OnGet_ValidQuery_FormatsRbseAndUsesRawValue()
    {
        var service = Substitute.For<IBsessCheckService>();
        var model = new CheckByRbseModel(service)
        {
            Rbse = "26/1"
        };
        PageTestHelpers.Attach(model);
        model.PageContext.HttpContext.Request.QueryString = new QueryString("?Rbse=26/1");
        service.GetCheckByRbseAsync("002600001", Arg.Any<CancellationToken>()).Returns(new BsessCheckByRbseResult(
            NotificationDate: "2024-01-01",
            BsessEartag: "ABC",
            BsessBirthDate: "2023-01-01",
            TestGroupName: "Group",
            BsssFinalResult: "Pos",
            Barcode: "B123",
            FormADate: "2024-01-02",
            BseEartag: "DEF",
            BseBirthDate: "2023-01-02",
            Survey: "Survey",
            BseFinalResult: "Neg"));

        var result = await model.OnGetAsync();

        result.Should().BeOfType<PageResult>();
        model.Rbse.Should().Be("00/26/00001");
        model.HasSearched.Should().BeTrue();
        model.Result.Should().NotBeNull();
        await service.Received(1).GetCheckByRbseAsync("002600001", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ResultMemo_OnGet_BlankRbse_RedirectsToFinalResultEntry()
    {
        var service = Substitute.For<ICaseService>();
        var model = new ResultMemoModel(service, NullLogger<ResultMemoModel>.Instance)
        {
            Rbse = " "
        };
        PageTestHelpers.Attach(model);

        var result = await model.OnGetAsync();

        result.Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/Case/FinalResultEntry");
        await service.DidNotReceiveWithAnyArgs().GetFinalResultAsync(default!);
    }

    [Fact]
    public async Task ResultMemo_OnGet_ValidResult_SetsWordHeadersAndMemoMetadata()
    {
        var service = Substitute.For<ICaseService>();
        var model = new ResultMemoModel(service, NullLogger<ResultMemoModel>.Instance)
        {
            Rbse = "00/26/00001",
            Print = false
        };
        PageTestHelpers.Attach(model);
        service.GetFinalResultAsync("002600001").Returns(new FinalResultRecord
        {
            Rbse = "002600001",
            FinalResult = "Pos",
            AlternateDiagnosis = "BSE-related"
        });

        var result = await model.OnGetAsync();

        result.Should().BeOfType<PageResult>();
        model.Result.Should().NotBeNull();
        model.IsPositive.Should().BeTrue();
        model.HasAlternateDiagnosis.Should().BeTrue();
        model.CaseRef.Should().Be("26/00001");
        model.PageContext.HttpContext.Response.ContentType.Should().Be("application/vnd.ms-word");
        model.PageContext.HttpContext.Response.Headers.ContentDisposition.ToString().Should().Contain("memo002600001.doc");
    }

    [Fact]
    public async Task ResultMemo_OnGet_ServiceThrows_RedirectsToFinalResultEntry()
    {
        var service = Substitute.For<ICaseService>();
        var model = new ResultMemoModel(service, NullLogger<ResultMemoModel>.Instance)
        {
            Rbse = "00/26/00001"
        };
        PageTestHelpers.Attach(model);
        service.GetFinalResultAsync("002600001").Returns(Task.FromException<FinalResultRecord?>(new InvalidOperationException("boom")));

        var result = await model.OnGetAsync();

        result.Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/Case/FinalResultEntry");
    }
}
