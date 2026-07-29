using BSE.Modules.Search.Models;
using BSE.Modules.Search.Repositories;
using BSE.Modules.Search.Services;

namespace BSE.Modules.Search.Tests;

public sealed class CaseSearchServiceTests
{
    private readonly ISearchRepository _repo = Substitute.For<ISearchRepository>();
    private readonly CaseSearchService _sut;

    public CaseSearchServiceTests()
    {
        _sut = new CaseSearchService(_repo);
    }

    [Fact]
    public async Task SearchCasesAsync_DelegatesToRepository()
    {
        var query = new CaseSearchQuery(Rbse: "01/23/12345");
        var expected = new List<CaseSearchResult>
        {
            new("01/23/12345", "12/345/6789/01", "Male", "Passive", "UK 123456 12345",
                new DateTime(2018, 1, 1), "N", new DateTime(2020, 5, 1), "Slaughter",
                "Neg", new DateTime(2020, 6, 1), "01/12345", null, null, "UK Bred", "Adult")
        };
        _repo.SearchCasesAsync(query, default).Returns(expected);

        var result = await _sut.SearchCasesAsync(query);

        result.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task SearchCasesAsync_EmptyQuery_ReturnsEmpty()
    {
        var query = new CaseSearchQuery();
        _repo.SearchCasesAsync(query, default).Returns(Array.Empty<CaseSearchResult>());

        var result = await _sut.SearchCasesAsync(query);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetCasesByCphhAsync_DelegatesToRepository()
    {
        // CaseDetailSearchResult: Rbse, Cphh, Sex, Eartag, BirthDate, PurchaseDate,
        // PurchaseAgeInMonths, OnsetDate, FormADate, SlaughterDate, FinalResultDate,
        // OnsetAgeInMonths, Fate, FinalResult, Survey, CaseStatus, TimeElapsed, DaysElapsed, Origin
        var detail = new List<CaseDetailSearchResult>
        {
            new("01/23/12345", "12/345/6789/01", "Male", "UK 123456 12345",
                new DateTime(2018, 1, 1), null, null, null, new DateTime(2020, 5, 1), null,
                new DateTime(2020, 6, 1), null, "Slaughter", "Neg", "Passive", null, null, null, "UK Bred")
        };
        _repo.GetCasesByCphhAsync("12/345/6789/01", "", "", false, default).Returns(detail);

        var result = await _sut.GetCasesByCphhAsync("12/345/6789/01", "", "", false);

        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetCasesByEartagHerdmarkAsync_DelegatesToRepository()
    {
        _repo.GetCasesByEartagHerdmarkAsync("123456", false, default)
             .Returns(Array.Empty<CaseDetailSearchResult>());

        var result = await _sut.GetCasesByEartagHerdmarkAsync("123456", false);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetRelatedAnimalsAsync_DelegatesToRepository()
    {
        var animals = new List<RelatedAnimalResult>
        {
            new("01/23/12345", "12/345/6789/01", "Dam", "Female", "UK 123456 11111",
                "01/01/2015", "Slaughter", null, "Bessie", "UK 123456 11111", "01/23/11111")
        };
        _repo.GetRelatedAnimalsAsync("010000001", "", "", "", "", default).Returns(animals);

        var result = await _sut.GetRelatedAnimalsAsync("010000001", "", "", "", "");

        result.Should().HaveCount(1);
        result[0].RelationType.Should().Be("Dam");
    }

    [Fact]
    public async Task SearchCasesAsync_WithNonGbFlag_PassesFlagToRepository()
    {
        var query = new CaseSearchQuery(IncludeNonGbCases: true);
        _repo.SearchCasesAsync(query, default).Returns(Array.Empty<CaseSearchResult>());

        await _sut.SearchCasesAsync(query);

        await _repo.Received(1).SearchCasesAsync(
            Arg.Is<CaseSearchQuery>(q => q.IncludeNonGbCases == true), default);
    }

    [Fact]
    public async Task SearchCasesAsync_WithPassiveActiveFilter_PassesFilterToRepository()
    {
        var query = new CaseSearchQuery(PassiveActive: "P");
        _repo.SearchCasesAsync(query, default).Returns(Array.Empty<CaseSearchResult>());

        await _sut.SearchCasesAsync(query);

        await _repo.Received(1).SearchCasesAsync(
            Arg.Is<CaseSearchQuery>(q => q.PassiveActive == "P"), default);
    }

    [Fact]
    public async Task SearchCasesAsync_WithDateRange_PassesRangesToRepository()
    {
        var earliest = new DateTime(2020, 1, 1);
        var latest = new DateTime(2020, 12, 31);
        var query = new CaseSearchQuery(EarliestFormADate: earliest, LatestFormADate: latest);
        _repo.SearchCasesAsync(query, default).Returns(Array.Empty<CaseSearchResult>());

        await _sut.SearchCasesAsync(query);

        await _repo.Received(1).SearchCasesAsync(
            Arg.Is<CaseSearchQuery>(q =>
                q.EarliestFormADate == earliest && q.LatestFormADate == latest), default);
    }

    [Fact]
    public async Task GetRelatedAnimalsAsync_EmptyParams_ReturnsEmpty()
    {
        _repo.GetRelatedAnimalsAsync("", "", "", "", "", default)
             .Returns(Array.Empty<RelatedAnimalResult>());

        var result = await _sut.GetRelatedAnimalsAsync("", "", "", "", "");

        result.Should().BeEmpty();
    }
}
