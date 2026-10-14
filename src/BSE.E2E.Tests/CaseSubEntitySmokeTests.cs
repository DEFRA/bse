namespace BSE.E2E.Tests;

/// <summary>
/// SMOKE coverage for the Case sub-entity Add/Edit pages, the Casework data-entry wizard,
/// and two standalone pages that had no regression coverage at all. Every page is asserted
/// "not to fault" (ERR-02 style, via <see cref="E2ETestBase.AssertPageHealthyAsync"/>) rather
/// than requiring an exact 200 — several Edit pages legitimately redirect when the given
/// child-record Id doesn't exist, which is correct behaviour, not a fault.
///
/// Seed data: Case RBSE 000000001 (CPHH 03043000301) has a real ClinicalVisit (Id 1),
/// HerdSize (Id 51504) and CaseRelation (Id 1) — those three use the exact Id so the Edit
/// page genuinely renders its 200 path; everything else uses an unknown Id to exercise the
/// not-found redirect path safely (no mutation anywhere — every scenario here is GET-only).
/// </summary>
public sealed class CaseSubEntitySmokeTests(BrowserFixture fixture) : E2ETestBase(fixture)
{
    private const string Rbse = "000000001";
    private const int UnknownId = 999999;

    [Theory]
    [InlineData("Case new", "/Case/New")]
    [InlineData("Case new non-GB", "/Case/NewNonGb")]
    [InlineData("Farm move-case new farm", "/Farm/MoveCaseNewFarm")]
    [InlineData("Batch print batch (standalone)", "/Batch/PrintBatch")]
    public async Task StandalonePage_DoesNotFault(string name, string path)
    {
        _ = name;
        await AssertPageHealthyAsync(path);
    }

    [Theory]
    [InlineData("Bab edit", "/Case/BabEdit")]
    [InlineData("Clinical edit", "/Case/ClinicalEdit")]
    [InlineData("Vla edit", "/Case/VlaEdit")]
    [InlineData("Feed add", "/Case/FeedAdd")]
    public async Task CaseRoutePage_DoesNotFault(string name, string path)
    {
        _ = name;
        await AssertPageHealthyAsync($"{path}/{Rbse}");
    }

    [Theory]
    [InlineData("Case test add", "/Case/CaseTestAdd")]
    [InlineData("Clinical visit add", "/Case/ClinicalVisitAdd")]
    [InlineData("Farm herd size add", "/Case/FarmHerdSizeAdd")]
    [InlineData("Farm linked farm add", "/Case/FarmLinkedFarmAdd")]
    [InlineData("Other owner add", "/Case/OtherOwnerAdd")]
    [InlineData("Relation add", "/Case/RelationAdd")]
    [InlineData("Pick supplier", "/Case/PickSupplier")]
    public async Task CaseQueryPage_DoesNotFault(string name, string path)
    {
        _ = name;
        await AssertPageHealthyAsync($"{path}?Rbse={Rbse}");
    }

    // Served as a Word-document download (Content-Disposition: attachment), not an HTML page —
    // Playwright's GotoAsync cannot navigate a real browser to a download response, so this
    // checks the API status directly instead of going through AssertPageHealthyAsync.
    [Fact]
    public async Task ResultMemo_DoesNotFault()
    {
        var response = await Api.GetAsync($"/Case/ResultMemo?Rbse={Rbse}");
        Assert.True(response.Status < 500, $"/Case/ResultMemo: server error HTTP {response.Status}");
    }

    [Fact]
    public async Task PickSireDam_DoesNotFault()
    {
        await AssertPageHealthyAsync($"/Case/PickSireDam?Rbse={Rbse}&Sex=F");
    }

    // Real child records — exact Id renders the genuine 200 edit path.
    [Fact]
    public async Task ClinicalVisitEdit_WithRealVisit_Renders()
    {
        await AssertPageRendersAsync($"/Case/ClinicalVisitEdit?Rbse={Rbse}&Id=1");
    }

    [Fact]
    public async Task FarmHerdSizeEdit_WithRealHerdSize_Renders()
    {
        await AssertPageRendersAsync($"/Case/FarmHerdSizeEdit?Rbse={Rbse}&Id=51504");
    }

    [Fact]
    public async Task RelationEdit_WithRealRelation_DoesNotFault()
    {
        await AssertPageHealthyAsync($"/Case/RelationEdit/{Rbse}/1");
    }

    // No matching child record for this seed case — exercises the graceful not-found redirect.
    [Theory]
    [InlineData("Case test edit", "/Case/CaseTestEdit")]
    [InlineData("Other owner edit", "/Case/OtherOwnerEdit")]
    [InlineData("Farm linked farm edit", "/Case/FarmLinkedFarmEdit")]
    public async Task CaseEditPage_UnknownId_DoesNotFault(string name, string path)
    {
        _ = name;
        await AssertPageHealthyAsync($"{path}?Rbse={Rbse}&Id={UnknownId}");
    }

    [Fact]
    public async Task FeedEdit_UnknownFeedId_DoesNotFault()
    {
        await AssertPageHealthyAsync($"/Case/FeedEdit/{Rbse}/{UnknownId}");
    }

    // ── CaseWork data-entry wizard + minute documents ───────────────────────────

    [Fact]
    public async Task CaseWorkEntry_DoesNotFault()
    {
        await AssertPageHealthyAsync($"/CaseWork/Entry/{Rbse}");
    }

    [Fact]
    public async Task CaseWorkMinute_DoesNotFault()
    {
        await AssertPageHealthyAsync($"/CaseWork/Minute/{Rbse}/AnnexA");
    }

    [Fact]
    public async Task CaseWorkMinuteDocument_DoesNotFault()
    {
        await AssertPageHealthyAsync($"/CaseWork/MinuteDocument/{Rbse}/AnnexA");
    }
}
