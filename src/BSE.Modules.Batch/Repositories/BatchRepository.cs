using System.Data;
using BSE.Infrastructure;
using BSE.Modules.Batch.Models;
using Dapper;

namespace BSE.Modules.Batch.Repositories;

/// <summary>
/// Dapper-backed repository for all Batch stored procedure calls.
/// SP names match filenames in src/BSE.Database/StoredProcedures/Batch/ exactly.
/// SP parameter names match @-parameter names in each .sql file exactly.
///
/// AddBatchNumber and AddBatchNumber1989 use three OUTPUT parameters read back via
/// DynamicParameters after the SP call (see <see cref="ExecuteWithOutputAsync"/>).
///
/// AddBatchNumberLink has two overloads: standalone (own connection) and
/// transactionally composable (caller-supplied connection/transaction) so that
/// the Case Management slice can enlist it in the case-save transaction without
/// leaking connection management into the service layer.
/// </summary>
public sealed class BatchRepository : DapperRepository, IBatchRepository
{
    public BatchRepository(IDbConnectionFactory connectionFactory)
        : base(connectionFactory) { }

    public async Task<BatchRecord> GetOrCreateBatchNumberAsync()
    {
        var param = new DynamicParameters();
        param.Add("@BatchID", dbType: DbType.Int32, direction: ParameterDirection.Output);
        param.Add("@BatchYear", dbType: DbType.Int16, direction: ParameterDirection.Output);
        param.Add("@BatchNumber", dbType: DbType.Int32, direction: ParameterDirection.Output);

        await ExecuteWithOutputAsync("AddBatchNumber", param);

        return new BatchRecord(
            param.Get<int>("@BatchID"),
            param.Get<short>("@BatchYear"),
            param.Get<int>("@BatchNumber"));
    }

    public async Task<BatchRecord> CreateBatchNumber1989Async()
    {
        var param = new DynamicParameters();
        param.Add("@BatchID", dbType: DbType.Int32, direction: ParameterDirection.Output);
        param.Add("@BatchYear", dbType: DbType.Int16, direction: ParameterDirection.Output);
        param.Add("@BatchNumber", dbType: DbType.Int32, direction: ParameterDirection.Output);

        await ExecuteWithOutputAsync("AddBatchNumber1989", param);

        return new BatchRecord(
            param.Get<int>("@BatchID"),
            param.Get<short>("@BatchYear"),
            param.Get<int>("@BatchNumber"));
    }

    public Task AddBatchNumberLinkAsync(int batchId, string rbse, string document)
        => ExecuteAsync("AddBatchNumberLink", new
        {
            BatchID = batchId,
            RBSE = rbse,
            Document = document
        });

    public Task AddBatchNumberLinkAsync(
        int batchId, string rbse, string document,
        IDbConnection connection, IDbTransaction? transaction)
        => ExecuteAsync("AddBatchNumberLink", new
        {
            BatchID = batchId,
            RBSE = rbse,
            Document = document
        }, connection, transaction);

    public async Task<int?> GetBatchIdAsync(short batchYear, int batchNumber)
    {
        var param = new DynamicParameters();
        param.Add("@BatchYear", batchYear, dbType: DbType.Int16);
        param.Add("@BatchNumber", batchNumber, dbType: DbType.Int32);
        param.Add("@BatchID", dbType: DbType.Int32, direction: ParameterDirection.Output);

        await ExecuteWithOutputAsync("GetBatchIDForBatch", param);

        var id = param.Get<int?>( "@BatchID");
        return id == 0 ? null : id;
    }

    public async Task<IReadOnlyList<BatchNumberEntry>> GetBatchNumbersByRbseAsync(string rbse)
    {
        var result = await QueryAsync<BatchNumberEntry>("GetBatchNumberByRBSE", new { RBSE = rbse });
        return result.ToList();
    }

    public async Task<IReadOnlyList<LatestBatchRecord>> GetLatestBatchNumbersAsync()
    {
        var result = await QueryAsync<LatestBatchRecord>("GetLatestBatchNumbers");
        return result.ToList();
    }

    public async Task<IReadOnlyList<BatchCaseSummaryRecord>> GetCasesByBatchIdAsync(int batchId)
    {
        var result = await QueryAsync<BatchCaseSummaryRecord>("GetCaseByBatchID", new { BatchID = batchId });
        return result.ToList();
    }

    public async Task<IReadOnlyList<BatchCaseSummaryRecord>> GetCasesByBatchAsync(short batchYear, int batchNumber)
    {
        var result = await QueryAsync<BatchCaseSummaryRecord>("GetCPHHRBSEForBatch", new
        {
            BatchYear = batchYear,
            BatchNumber = batchNumber
        });
        return result.ToList();
    }

    public async Task<IReadOnlyList<BatchCaseRecord>> GetCaseDetailsByBatchIdAsync(int batchId)
    {
        var result = await QueryAsync<BatchCaseRecord>("GetCPHHRBSEForBatchID", new { BatchID = batchId });
        return result.ToList();
    }
}
