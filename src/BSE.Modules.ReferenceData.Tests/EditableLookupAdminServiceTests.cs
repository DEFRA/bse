using BSE.Infrastructure;
using BSE.Modules.ReferenceData.Models;
using BSE.Modules.ReferenceData.Repositories;
using BSE.Modules.ReferenceData.Services;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace BSE.Modules.ReferenceData.Tests;

public sealed class EditableLookupAdminServiceTests
{
    private readonly ILookupRepository _repo = Substitute.For<ILookupRepository>();
    private readonly IDbRepository _db = Substitute.For<IDbRepository>();
    private readonly EditableLookupAdminService _sut;

    public EditableLookupAdminServiceTests()
        => _sut = new EditableLookupAdminService(_repo, _db);

    [Fact]
    public async Task GetEditableLookupsAsync_DelegatesToRepository()
    {
        var items = new[] { new EditableLookup { Id = 1, TableName = "luBreed", Description = "Breed" } };
        _repo.GetEditableLookupsAsync().Returns(items);

        var result = await _sut.GetEditableLookupsAsync();

        result.Should().BeEquivalentTo(items);
    }

    [Fact]
    public async Task AddBreedAsync_ExecutesCorrectSP_WithCorrectParams()
    {
        await _sut.AddBreedAsync("HF", "Hereford", "Hereford");

        await _db.Received(1).ExecuteAsync(
            "AddluBreed",
            Arg.Is<object>(p => MatchesAnonymous(p, "Code", "HF")));
    }

    [Fact]
    public async Task DeleteBreedAsync_ExecutesCorrectSP()
    {
        await _sut.DeleteBreedAsync("HF");

        await _db.Received(1).ExecuteAsync("DeleteluBreed", Arg.Any<object?>());
    }

    [Fact]
    public async Task AddCodeDescriptionItemAsync_PassesCorrectSpName()
    {
        await _sut.AddCodeDescriptionItemAsync("AddluSex", "M", "Male");

        await _db.Received(1).ExecuteAsync("AddluSex", Arg.Any<object?>());
    }

    [Fact]
    public async Task AddSupplierAsync_ExecutesCorrectSP()
    {
        await _sut.AddSupplierAsync("ACME Farm Supplies", null);

        await _db.Received(1).ExecuteAsync("AddluSupplier", Arg.Any<object?>());
    }

    [Fact]
    public async Task AddADNSRegionAsync_ExecutesCorrectSP()
    {
        await _sut.AddADNSRegionAsync(42, "New Region");

        await _db.Received(1).ExecuteAsync("AddluADNSRegion", Arg.Any<object?>());
    }

    // Helper: checks an anonymous-object property value via reflection.
    private static bool MatchesAnonymous(object? obj, string property, object expected)
    {
        if (obj is null) return false;
        var prop = obj.GetType().GetProperty(property);
        return prop is not null && Equals(prop.GetValue(obj), expected);
    }
}
