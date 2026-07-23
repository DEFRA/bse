using BSE.Modules.Batch.Models;
using BSE.Modules.Batch.Repositories;

namespace BSE.Modules.Batch.Services;

public sealed class BatchService : IBatchService
{
    private readonly IBatchRepository _repository;

    public BatchService(IBatchRepository repository)
    {
        _repository = repository;
    }

    public Task<BatchRecord> GetOrCreateBatchNumberAsync()
        => _repository.GetOrCreateBatchNumberAsync();

    public Task<BatchRecord> CreateBatchNumber1989Async()
        => _repository.CreateBatchNumber1989Async();

    public Task AddBatchNumberLinkAsync(int batchId, string rbse, string document)
        => _repository.AddBatchNumberLinkAsync(batchId, rbse, document);

    public Task<int?> GetBatchIdAsync(short batchYear, int batchNumber)
        => _repository.GetBatchIdAsync(batchYear, batchNumber);

    public Task<IReadOnlyList<BatchNumberEntry>> GetBatchNumbersByRbseAsync(string rbse)
        => _repository.GetBatchNumbersByRbseAsync(rbse);

    public Task<IReadOnlyList<LatestBatchRecord>> GetLatestBatchNumbersAsync()
        => _repository.GetLatestBatchNumbersAsync();

    public Task<IReadOnlyList<BatchCaseSummaryRecord>> GetCasesByBatchIdAsync(int batchId)
        => _repository.GetCasesByBatchIdAsync(batchId);

    public Task<IReadOnlyList<BatchCaseSummaryRecord>> GetCasesByBatchAsync(short batchYear, int batchNumber)
        => _repository.GetCasesByBatchAsync(batchYear, batchNumber);

    public Task<IReadOnlyList<BatchCaseRecord>> GetCaseDetailsByBatchIdAsync(int batchId)
        => _repository.GetCaseDetailsByBatchIdAsync(batchId);
}
