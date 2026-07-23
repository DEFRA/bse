namespace BSE.Modules.FarmManagement.Tests;

/// <summary>
/// Integration test stubs for FarmRepository. All tests are skipped pending a test database.
/// These validate real SP round-trips and should run in an environment with SQL Server access.
/// Mark the test run with <c>--filter "Category=Integration"</c> to include them.
/// </summary>
[Trait("Category", "Integration")]
public sealed class FarmRepositoryIntegrationTests
{
    [Fact(Skip = "Requires SQL Server — run manually against a test database.")]
    public async Task GetByCphh_WhenFarmExists_ReturnsFarmRecord()
    {
        // Arrange: real IDbConnectionFactory pointing at test DB
        // Act:     FarmRepository.GetByCphhAsync(knownCphh)
        // Assert:  result.CPHH == knownCphh
        await Task.CompletedTask;
    }

    [Fact(Skip = "Requires SQL Server — run manually against a test database.")]
    public async Task ChangeCphh_AllEightErrorScenarios_ReturnCorrectResultCodes()
    {
        // Scenario 1: old CPHH not found → code 1
        // Scenario 2: new CPHH already exists → code 1
        // Scenario 3–8: SP internal errors are not easily injected in integration tests;
        //               these are covered by the unit tests via mocked return values.
        await Task.CompletedTask;
    }

    [Fact(Skip = "Requires SQL Server — run manually against a test database.")]
    public async Task FarmDetail_RoundTrip_GetByCphh_GetDetails_Match()
    {
        // Arrange: known CPHH with at least one related farm and one herd size record
        // Act:     GetByCphhAsync + GetDetailsByCphhAsync
        // Assert:  Farm records match; related farms and herd sizes are non-empty
        await Task.CompletedTask;
    }
}
