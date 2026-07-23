using BSE.Modules.OssExport.Models;
using BSE.Modules.OssExport.Repositories;
using BSE.Modules.OssExport.Services;

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
}
