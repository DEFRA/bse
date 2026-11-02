using BSE.Infrastructure;
using BSE.Modules.ReferenceData.Models;
using BSE.Modules.ReferenceData.Repositories;
using BSE.Modules.ReferenceData.Services;
using Dapper;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace BSE.Modules.ReferenceData.Tests;

public sealed class EditableLookupAdminServiceDelegationTests
{
    private readonly ILookupRepository _repo = Substitute.For<ILookupRepository>();
    private readonly IDbRepository _db = Substitute.For<IDbRepository>();
    private readonly EditableLookupAdminService _sut;

    public EditableLookupAdminServiceDelegationTests() => _sut = new EditableLookupAdminService(_repo, _db);

    public static TheoryData<string, Func<EditableLookupAdminService, Task>> ExecuteCalls() => new()
    {
        { "SpX", s => s.AddCodeDescriptionItemAsync("SpX", "C", "D") },
        { "SpX", s => s.EditCodeDescriptionItemAsync("SpX", "O", "C", "D") },
        { "SpX", s => s.DeleteCodeDescriptionItemAsync("SpX", "C") },
        { "EditluBreed", s => s.EditBreedAsync("O", "C", "F", "A") },
        { "AddluTestType", s => s.AddTestTypeAsync("C", "D", true) },
        { "EditluTestType", s => s.EditTestTypeAsync("O", "C", "D", false) },
        { "DeleteluTestType", s => s.DeleteTestTypeAsync("C") },
        { "AddluRelationFate", s => s.AddRelationFateAsync("C", "D", true) },
        { "EditluRelationFate", s => s.EditRelationFateAsync("O", "C", "D", true) },
        { "DeleteluRelationFate", s => s.DeleteRelationFateAsync("C") },
        { "AddluBSECounty", s => s.AddBSECountyAsync("ID", "C", "D", 1) },
        { "EditluBSECounty", s => s.EditBSECountyAsync("ID", "O", "C", "D", null) },
        { "DeleteluBSECounty", s => s.DeleteBSECountyAsync("C") },
        { "AddluAHO", s => s.AddAHOAsync("C", "N", 2) },
        { "EditluAHO", s => s.EditAHOAsync("O", "C", "N", null) },
        { "DeleteluAHO", s => s.DeleteAHOAsync("C") },
        { "EditluAHRO", s => s.EditAHROAsync(3, "N") },
        { "DeleteluAHRO", s => s.DeleteAHROAsync("N") },
        { "EditluSupplier", s => s.EditSupplierAsync(4, "N", null) },
        { "DeleteluSupplier", s => s.DeleteSupplierAsync(4) },
        { "EditluADNSRegion", s => s.EditADNSRegionAsync(5, "N") },
        { "DeleteluADNSRegion", s => s.DeleteADNSRegionAsync(5) },
        { "AddluTSETestingSite", s => s.AddTSETestingSiteAsync("N", "A", "CPH", "AHO") },
        { "EditluTSETestingSite", s => s.EditTSETestingSiteAsync("OCPH", "N", "A", "CPH", "AHO") },
        { "DeluTSETestingSite", s => s.DeleteTSETestingSiteAsync("CPH") },
    };

    [Theory]
    [MemberData(nameof(ExecuteCalls))]
    public async Task Call_ExecutesTheExpectedStoredProcedure(string expectedSp, Func<EditableLookupAdminService, Task> call)
    {
        if (expectedSp == "DeluTSETestingSite")
            expectedSp = "DeleteluTSETestingSite";

        await call(_sut);

        await _db.Received(1).ExecuteAsync(expectedSp, Arg.Any<object?>());
    }

    [Fact]
    public async Task OutputProcedures_UseExecuteWithOutput()
    {
        await _sut.AddAHROAsync("N");
        await _sut.AddSupplierAsync("N", "D");

        await _db.Received(1).ExecuteWithOutputAsync("AddluAHRO", Arg.Any<DynamicParameters>());
        await _db.Received(1).ExecuteWithOutputAsync("AddluSupplier", Arg.Any<DynamicParameters>());
    }

    [Fact]
    public async Task GetEditableLookupProcsAsync_DelegatesToRepository()
    {
        var procs = new EditableLookupProcs();
        _repo.GetEditableLookupProcsAsync(7).Returns(procs);

        (await _sut.GetEditableLookupProcsAsync(7)).Should().BeSameAs(procs);
    }

    [Fact]
    public async Task GetLookupRowsAsync_ReturnsRowsAsDictionaries()
    {
        IDictionary<string, object?> row = new System.Dynamic.ExpandoObject();
        row["Code"] = "X";
        _db.QueryAsync<dynamic>("SelectSp", Arg.Any<object?>()).Returns(new List<dynamic> { row });

        var rows = (await _sut.GetLookupRowsAsync("SelectSp")).ToList();

        rows.Should().ContainSingle().Which["Code"].Should().Be("X");
    }
}
