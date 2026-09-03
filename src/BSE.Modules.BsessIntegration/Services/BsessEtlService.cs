using BSE.Modules.BsessIntegration.Configuration;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BSE.Modules.BsessIntegration.Services;

/// <summary>
/// Replaces the legacy <c>BSESS Import.dtsx</c> SSIS package.
/// Reads all qualifying BSESS records from the TSES source database and bulk-copies
/// them into the local <c>BSESSImport</c> staging table using <see cref="SqlBulkCopy"/>.
/// The table is truncated before each load, making the operation fully idempotent.
/// </summary>
public sealed class BsessEtlService : IBsessEtlService
{
    // Reconstructed verbatim from the SSIS OLE DB source SqlCommand, with &gt; decoded to >.
    private const string SourceQuery = """
        SELECT
            '00' + LEFT(LTRIM(rbseno), 2) + RIGHT('0000' + SUBSTRING(RTRIM(rbseno), 4, LEN(rbseno)), 5) AS FormattedRBSE,
            Sample.RBSENo,
            animal.AnimalID,
            animal.EartagNo,
            animal.BirthDate,
            CASE TestingGroup.TestingGroupID
                WHEN 3  THEN 'FS'
                WHEN 4  THEN 'CA'
                WHEN 9  THEN 'OTM3'
                WHEN 10 THEN 'OTM3'
                WHEN 11 THEN 'OTM3'
                WHEN 12 THEN 'OTM3'
                ELSE NULL
            END AS TestGroupDerivedSurvey,
            TestingGroup.TestGroupName,
            Sample.FinalResultID,
            Result.ResultName,
            animal.NotificationDate,
            Sample.BarCode
        FROM
            (animal INNER JOIN Sample ON animal.AnimalID = Sample.AnimalID)
            INNER JOIN TestingGroup ON animal.TestingGroupID = TestingGroup.TestingGroupID
            LEFT JOIN Result ON Sample.FinalResultID = Result.ResultID
        WHERE
            Len(RBSENo) > 3 AND
            animal.SpeciesID = 1
        """;

    private readonly BsessEtlOptions _options;
    private readonly string _targetConnectionString;
    private readonly ILogger<BsessEtlService> _logger;

    public BsessEtlService(
        IOptions<BsessEtlOptions> options,
        IConfiguration configuration,
        ILogger<BsessEtlService> logger)
    {
        _options = options.Value;
        _targetConnectionString = configuration.GetConnectionString("BSE")
            ?? throw new InvalidOperationException(
                "ConnectionStrings:BSE is not configured. " +
                "Set the BSE connection string in appsettings.json or via environment variable.");
        _logger = logger;
    }

    public async Task ImportAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.SourceConnectionString))
            throw new InvalidOperationException(
                "BsessEtl:SourceConnectionString is not configured. " +
                "Set it via environment variable BsessEtl__SourceConnectionString.");

        _logger.LogInformation("Starting BSESS import from TSES source");

        using var sourceConnection = new SqlConnection(_options.SourceConnectionString);
        await sourceConnection.OpenAsync(cancellationToken);

        using var sourceCommand = new SqlCommand(SourceQuery, sourceConnection)
        {
            CommandTimeout = 120
        };

        using var reader = await sourceCommand.ExecuteReaderAsync(cancellationToken);

        using var targetConnection = new SqlConnection(_targetConnectionString);
        await targetConnection.OpenAsync(cancellationToken);

        using var truncateCommand = new SqlCommand("TRUNCATE TABLE [dbo].[BSESSImport]", targetConnection);
        await truncateCommand.ExecuteNonQueryAsync(cancellationToken);

        using var bulkCopy = new SqlBulkCopy(targetConnection)
        {
            DestinationTableName = "[dbo].[BSESSImport]",
            BatchSize = 1000,
            BulkCopyTimeout = 300
        };

        // Explicit column mappings — source names differ from target names in several places.
        bulkCopy.ColumnMappings.Add("FormattedRBSE", "RBSE");
        bulkCopy.ColumnMappings.Add("RBSENo", "UnformattedRBSE");
        bulkCopy.ColumnMappings.Add("AnimalID", "AnimalIID");
        bulkCopy.ColumnMappings.Add("EartagNo", "Eartag");
        bulkCopy.ColumnMappings.Add("BirthDate", "BirthDate");
        bulkCopy.ColumnMappings.Add("TestGroupDerivedSurvey", "TestGroupDerivedSurvey");
        bulkCopy.ColumnMappings.Add("TestGroupName", "TestGroupName");
        bulkCopy.ColumnMappings.Add("FinalResultID", "FinalResultID");
        bulkCopy.ColumnMappings.Add("ResultName", "FinalResultName");
        bulkCopy.ColumnMappings.Add("NotificationDate", "NotificationDate");
        bulkCopy.ColumnMappings.Add("BarCode", "BarCode");

        await bulkCopy.WriteToServerAsync(reader, cancellationToken);

        _logger.LogInformation("BSESS import completed — {RowsCopied} rows copied", bulkCopy.RowsCopied);
    }
}
