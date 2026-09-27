using System.Text;
using BSE.Modules.OssExport.Models;
using BSE.Modules.OssExport.Repositories;
using BSE.SharedKernel;

namespace BSE.Modules.OssExport.Services;

public sealed class OssExportService : IOssExportService
{
    private readonly IOssExportRepository _repository;

    public OssExportService(IOssExportRepository repository)
        => _repository = repository;

    public Task PopulateStagingTablesAsync()
        => _repository.PopulateStagingTablesAsync();

    public Task<OssExportRecord?> GetExportDetailsByRbseAsync(string rbse)
        => _repository.GetExportDetailsByRbseAsync(rbse);

    public Task<BatchNumber1989Result?> CreateBatchNumber1989Async()
        => _repository.CreateBatchNumber1989Async();

    public async Task<OssExportRecord?> ValidateAndGetRbseDetailsAsync(string rbseInput)
    {
        // Normalize the RBSE input (accepts "00/26/00001" or "002600001" format)
        var normalized = RbseHelper.Normalize(rbseInput);
        
        // Validation: must be exactly 9 digits after normalization
        if (!RbseHelper.IsValid(normalized))
            return null;

        // Query the database for RBSE details
        // Returns null if RBSE not found
        return await _repository.GetExportDetailsByRbseAsync(normalized);
    }

    public async Task<string> GenerateOssExportFileAsync(int batchId, short batchYear, int batchNumber, IEnumerable<OssExportBatchEntryRecord> entries)
    {
        var entryList = entries.ToList();

        // Link all RBSEs currently in the grid to the batch
        foreach (var entry in entryList)
        {
            await _repository.AddBatchNumberLinkAsync(batchId, entry.Rbse, "BSE1");
        }

        // Generate the batch key: last 2 digits of year + 3-digit batch number
        // Example: year=1989, number=123 → "89123"
        var yearSuffix = batchYear.ToString("00").Substring(Math.Max(0, batchYear.ToString("00").Length - 2));
        var batchNumberPadded = batchNumber.ToString("000");
        var batchKey = yearSuffix + batchNumberPadded;

        // Build the file content from the entries currently in the grid only — a case
        // that was added then removed before export must not reappear here, even though
        // its BatchNumberLink row from an earlier export of this same batch still exists in the database.
        var sb = new StringBuilder();
        foreach (var entry in entryList)
        {
            sb.Append('|').Append(batchKey).Append('|');
            sb.Append(entry.Rbse).Append('|');
            sb.Append(entry.Cphh).Append('|');
            sb.Append("\r\n");
        }

        return sb.ToString();
    }

    public Task<IReadOnlyList<OssExportFileRecord>> GetCasesByBatchIdAsync(int batchId)
        => _repository.GetCasesByBatchIdAsync(batchId);

    public Task<bool> AddBatchNumberLinkAsync(int batchId, string rbse)
        => _repository.AddBatchNumberLinkAsync(batchId, rbse, "BSE1");
}
