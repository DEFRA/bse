using BSE.Infrastructure;
using BSE.Modules.ReferenceData.Repositories;
using BSE.Modules.ReferenceData.Services;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Testcontainers.MsSql;
using Xunit;

namespace BSE.Modules.ReferenceData.Tests.Integration;

/// <summary>
/// Base class for integration tests that spin up a real SQL Server via Testcontainers.
/// Tests inheriting this class should be decorated with [Trait("Category", "Integration")]
/// so they can be excluded from fast CI runs: <c>dotnet test --filter "Category!=Integration"</c>.
/// </summary>
public abstract class ReferenceDataIntegrationTestBase : IAsyncLifetime
{
    private readonly MsSqlContainer _container = new MsSqlBuilder().Build();
    protected ILookupDataService LookupDataService { get; private set; } = null!;
    protected IGeoLookupService GeoLookupService { get; private set; } = null!;
    protected ILookupRepository Repository { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:BSE"] = _container.GetConnectionString()
            })
            .Build();

        var factory = new SqlConnectionFactory(configuration);
        Repository = new LookupRepository(factory);
        LookupDataService = new LookupDataService(Repository);
        GeoLookupService = new GeoLookupService(Repository);

        await SeedSchemaAsync(_container.GetConnectionString());
    }

    public async Task DisposeAsync()
    {
        await _container.StopAsync();
        await _container.DisposeAsync();
    }

    private static async Task SeedSchemaAsync(string connectionString)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        await using var cmd = connection.CreateCommand();
        cmd.CommandText = SeedSql;
        await cmd.ExecuteNonQueryAsync();
    }

    /// <summary>Minimal schema and seed data required by the integration tests.</summary>
    private const string SeedSql = """
        -- luBreed
        IF OBJECT_ID('luBreed', 'U') IS NULL
        CREATE TABLE luBreed (
            [Code]           varchar(20) NOT NULL PRIMARY KEY,
            [FullName]       varchar(50) NOT NULL,
            [AmalgamatedName] varchar(50) NOT NULL
        );
        IF NOT EXISTS (SELECT 1 FROM luBreed WHERE Code = 'HF')
            INSERT INTO luBreed (Code, FullName, AmalgamatedName) VALUES ('HF', 'Hereford', 'Hereford');
        IF NOT EXISTS (SELECT 1 FROM luBreed WHERE Code = 'AA')
            INSERT INTO luBreed (Code, FullName, AmalgamatedName) VALUES ('AA', 'Aberdeen Angus', 'Aberdeen Angus');

        -- GetluBreed SP
        IF OBJECT_ID('GetluBreed', 'P') IS NOT NULL DROP PROCEDURE GetluBreed;
        EXEC('
        CREATE PROCEDURE GetluBreed AS
        DECLARE @t TABLE ([ID] int IDENTITY(1,1), [Code] varchar(20), [FullName] varchar(50), [AmalgamatedName] varchar(50))
        INSERT INTO @t ([Code],[FullName],[AmalgamatedName])
            SELECT [Code],[FullName],[AmalgamatedName] FROM luBreed ORDER BY [FullName]
        SELECT [ID],[Code],[FullName],[AmalgamatedName] FROM @t
        ');

        -- luParish + luADNSRegion + luAuthority + luAuthorityCounty for parish geo lookups
        IF OBJECT_ID('luAuthorityCounty', 'U') IS NULL
        CREATE TABLE luAuthorityCounty ([ID] int NOT NULL PRIMARY KEY, [County] varchar(50) NOT NULL);

        IF OBJECT_ID('luAuthority', 'U') IS NULL
        CREATE TABLE luAuthority ([ID] int NOT NULL PRIMARY KEY, [Name] varchar(100) NOT NULL, [AuthorityCountyID] int NULL);

        IF OBJECT_ID('luADNSRegion', 'U') IS NULL
        CREATE TABLE luADNSRegion ([ID] int NOT NULL PRIMARY KEY, [Name] varchar(50) NOT NULL, [AuthorityID] int NULL);

        IF OBJECT_ID('luParish', 'U') IS NULL
        CREATE TABLE luParish (
            [County]       char(2)  NOT NULL,
            [Parish]       char(3)  NOT NULL,
            [Name]         varchar(50) NOT NULL,
            [ADNSRegionID] int NULL,
            [BSECounty]    char(2) NULL,
            PRIMARY KEY ([County],[Parish])
        );

        -- luMapReference + luParishMapReference for geo map lookups
        IF OBJECT_ID('luMapReference', 'U') IS NULL
        CREATE TABLE luMapReference (
            [Code]         char(2) NOT NULL PRIMARY KEY,
            [XCoordPrefix] char(1) NOT NULL,
            [YCoordPrefix] varchar(2) NOT NULL
        );

        IF OBJECT_ID('luParishMapReference', 'U') IS NULL
        CREATE TABLE luParishMapReference (
            [County]        char(2) NOT NULL,
            [Parish]        char(3) NOT NULL,
            [MapReference1] varchar(6) NOT NULL,
            [MapReference2] varchar(6) NOT NULL
        );

        -- Seed geo data
        IF NOT EXISTS (SELECT 1 FROM luAuthorityCounty WHERE ID = 1)
            INSERT INTO luAuthorityCounty VALUES (1, 'Grampian');
        IF NOT EXISTS (SELECT 1 FROM luAuthority WHERE ID = 1)
            INSERT INTO luAuthority VALUES (1, 'Aberdeenshire', 1);
        IF NOT EXISTS (SELECT 1 FROM luADNSRegion WHERE ID = 1)
            INSERT INTO luADNSRegion VALUES (1, 'North Scotland', 1);
        IF NOT EXISTS (SELECT 1 FROM luParish WHERE County = 'AB' AND Parish = '001')
            INSERT INTO luParish VALUES ('AB', '001', 'Aboyne', 1, 'AB');
        IF NOT EXISTS (SELECT 1 FROM luMapReference WHERE Code = 'NJ')
            INSERT INTO luMapReference VALUES ('NJ', '3', '8');
        IF NOT EXISTS (SELECT 1 FROM luParishMapReference WHERE County='AB' AND Parish='001')
            INSERT INTO luParishMapReference VALUES ('AB', '001', 'NJ5030', 'NJ5535');

        -- GetMapReferenceByCountyParish SP
        IF OBJECT_ID('GetMapReferenceByCountyParish', 'P') IS NOT NULL DROP PROCEDURE GetMapReferenceByCountyParish;
        EXEC('
        CREATE PROCEDURE GetMapReferenceByCountyParish @County char(2), @Parish char(3) AS
        SELECT
            ''0'' + m.XCoordPrefix + SUBSTRING(p.MapReference1,3,2) AS XReference1,
            CASE LEN(m.YCoordPrefix) WHEN 1 THEN ''0''+m.YCoordPrefix ELSE m.YCoordPrefix END
                + SUBSTRING(p.MapReference1,5,2) AS YReference1,
            ''0'' + m.XCoordPrefix + SUBSTRING(p.MapReference2,3,2) AS XReference2,
            CASE LEN(m.YCoordPrefix) WHEN 1 THEN ''0''+m.YCoordPrefix ELSE m.YCoordPrefix END
                + SUBSTRING(p.MapReference2,5,2) AS YReference2
        FROM luParishMapReference p
        INNER JOIN luMapReference m ON LEFT(p.MapReference1,2) = m.Code
        WHERE p.County = @County AND p.Parish = @Parish
        ');
        """;
}
