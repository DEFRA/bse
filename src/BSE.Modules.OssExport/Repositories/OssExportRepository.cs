using System.Data;
using BSE.Infrastructure;
using BSE.Modules.OssExport.Models;
using Dapper;

namespace BSE.Modules.OssExport.Repositories;

public sealed class OssExportRepository : DapperRepository, IOssExportRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public OssExportRepository(IDbConnectionFactory connectionFactory) : base(connectionFactory)
        => _connectionFactory = connectionFactory;

    public async Task PopulateStagingTablesAsync()
    {
        // The Copy* SPs are DDL+DML (DROP TABLE / CREATE TABLE / INSERT).
        // SQL Server does not permit DDL inside an explicit transaction when the table
        // being dropped/created has open references in the same connection, but the
        // SPs themselves are self-contained. We call them sequentially on one connection
        // so the staging tables are consistent within a single export run.
        // If any SP throws, subsequent SPs are not called and the export is aborted.
        using var connection = _connectionFactory.CreateConnection();
        connection.Open();

        await connection.ExecuteAsync("CopyCaseToExportTable", commandType: CommandType.StoredProcedure);
        await connection.ExecuteAsync("CopyFarmToExportTable", commandType: CommandType.StoredProcedure);
        await connection.ExecuteAsync("CopyHerdSizeToExportTable", commandType: CommandType.StoredProcedure);
        await connection.ExecuteAsync("CopyRelationToExportTable", commandType: CommandType.StoredProcedure);
    }

    public Task<OssExportRecord?> GetExportDetailsByRbseAsync(string rbse)
        => QuerySingleOrDefaultAsync<OssExportRecord>("GetOSSExportByRBSE", new { RBSE = rbse });

    public async Task<BatchNumber1989Result?> CreateBatchNumber1989Async()
    {
        var param = new DynamicParameters();
        param.Add("BatchID", dbType: DbType.Int32, direction: ParameterDirection.Output);
        param.Add("BatchYear", dbType: DbType.Int16, direction: ParameterDirection.Output);
        param.Add("BatchNumber", dbType: DbType.Int32, direction: ParameterDirection.Output);
        param.Add("RETURN_VALUE", dbType: DbType.Int32, direction: ParameterDirection.ReturnValue);

        using var connection = _connectionFactory.CreateConnection();
        connection.Open();
        await connection.ExecuteAsync("AddBatchNumber1989", param, commandType: CommandType.StoredProcedure);

        var returnCode = param.Get<int>("RETURN_VALUE");
        if (returnCode != 0)
            return null;

        return new BatchNumber1989Result(
            param.Get<int>("BatchID"),
            param.Get<short>("BatchYear"),
            param.Get<int>("BatchNumber"));
    }
}
