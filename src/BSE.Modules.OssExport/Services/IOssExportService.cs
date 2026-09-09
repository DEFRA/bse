using BSE.Modules.OssExport.Models;

namespace BSE.Modules.OssExport.Services;

public interface IOssExportService
{
    Task PopulateStagingTablesAsync();
    Task<OssExportRecord?> GetExportDetailsByRbseAsync(string rbse);
    Task<BatchNumber1989Result?> CreateBatchNumber1989Async();
    Task<IReadOnlyList<string>> GetStagedBse1RbseAsync();

    /// <summary>
    /// Validates and adds a single RBSE to a batch.
    /// Returns the lookup result if successful, null if validation fails or RBSE not found.
    /// </summary>
    Task<OssExportRecord?> ValidateAndGetRbseDetailsAsync(string rbseInput);

    /// <summary>
    /// Links all RBSEs in the grid to the batch and generates the export file.
    /// Returns the pipe-delimited file content as a string.
    /// </summary>
    Task<string> GenerateOssExportFileAsync(int batchId, short batchYear, int batchNumber, IEnumerable<OssExportBatchEntryRecord> entries);

    /// <summary>
    /// Retrieves all cases linked to a batch for export file generation.
    /// </summary>
    Task<IReadOnlyList<OssExportFileRecord>> GetCasesByBatchIdAsync(int batchId);

    /// <summary>
    /// Links a single RBSE to a batch.
    /// </summary>
    Task<bool> AddBatchNumberLinkAsync(int batchId, string rbse);
}