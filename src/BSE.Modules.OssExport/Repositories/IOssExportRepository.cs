using System.Data;
using BSE.Modules.OssExport.Models;

namespace BSE.Modules.OssExport.Repositories;

public interface IOssExportRepository
{
    /// <summary>
    /// Populates the four OSS staging tables (<c>expCase</c>, <c>expFarm</c>,
    /// <c>expAge</c>, <c>expRelation</c>) inside a single transaction by calling the
    /// four <c>Copy*ToExportTable</c> stored procedures in sequence.
    /// All-or-none: if any SP fails the transaction is rolled back.
    /// </summary>
    Task PopulateStagingTablesAsync();

    /// <summary>
    /// Returns OSS export details for a single case from <c>GetOSSExportByRBSE</c>.
    /// </summary>
    Task<OssExportRecord?> GetExportDetailsByRbseAsync(string rbse);

    /// <summary>
    /// Creates a new 1989-year batch record and returns its ID, year, and number.
    /// Calls <c>AddBatchNumber1989</c>. Returns null if the SP indicates failure.
    /// </summary>
    Task<BatchNumber1989Result?> CreateBatchNumber1989Async();

    /// <summary>
    /// Returns staged BSE1 RBSE values from <c>expCase</c> after staging population.
    /// Used to create BSE1 batch links prior to export file generation.
    /// </summary>
    Task<IReadOnlyList<string>> GetStagedBse1RbseAsync();
}
