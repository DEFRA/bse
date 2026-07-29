using BSE.Modules.OssExport.Models;
namespace BSE.Modules.OssExport.Services;
public interface IOssExportService
{
    Task PopulateStagingTablesAsync();
    Task<OssExportRecord?> GetExportDetailsByRbseAsync(string rbse);
    Task<BatchNumber1989Result?> CreateBatchNumber1989Async();
}