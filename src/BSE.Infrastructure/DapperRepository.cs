using System.Data;
using Dapper;

namespace BSE.Infrastructure;

public class DapperRepository : IDbRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public DapperRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    // Microsoft.Data.SqlClient sets ARITHABORT OFF by default, which causes
    // SQL Server to reuse cached plans compiled under different SET options
    // (e.g. from SSMS which uses ARITHABORT ON), returning wrong results.
    // Setting it ON here fixes all SP calls without touching individual SPs.
    private IDbConnection OpenConnection()
    {
        var connection = _connectionFactory.CreateConnection();
        connection.Open();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "SET ARITHABORT ON";
        cmd.ExecuteNonQuery();
        return connection;
    }

    public async Task<IEnumerable<T>> QueryAsync<T>(string storedProcedure, object? param = null)
    {
        using var connection = OpenConnection();
        return await connection.QueryAsync<T>(
            storedProcedure,
            param,
            commandType: CommandType.StoredProcedure);
    }

    public async Task ExecuteAsync(string storedProcedure, object? param = null)
    {
        using var connection = OpenConnection();
        await connection.ExecuteAsync(
            storedProcedure,
            param,
            commandType: CommandType.StoredProcedure);
    }

    public async Task<T?> QuerySingleOrDefaultAsync<T>(string storedProcedure, object? param = null)
        where T : class
    {
        using var connection = OpenConnection();
        return await connection.QuerySingleOrDefaultAsync<T>(
            storedProcedure,
            param,
            commandType: CommandType.StoredProcedure);
    }

    public async Task<IEnumerable<T>> QueryAsync<T>(string storedProcedure, object? param, int commandTimeoutSeconds)
    {
        using var connection = OpenConnection();
        return await connection.QueryAsync<T>(
            storedProcedure,
            param,
            commandType: CommandType.StoredProcedure,
            commandTimeout: commandTimeoutSeconds);
    }

    public async Task ExecuteWithOutputAsync(string storedProcedure, DynamicParameters param)
    {
        using var connection = OpenConnection();
        await connection.ExecuteAsync(
            storedProcedure,
            param,
            commandType: CommandType.StoredProcedure);
    }

    public Task ExecuteAsync(string storedProcedure, object? param, IDbConnection connection, IDbTransaction? transaction)
        => connection.ExecuteAsync(
            storedProcedure,
            param,
            transaction: transaction,
            commandType: CommandType.StoredProcedure);

    public async Task<T> QueryMultipleAsync<T>(string storedProcedure, object? param, Func<SqlMapper.GridReader, Task<T>> read)
    {
        using var connection = OpenConnection();
        using var multi = await connection.QueryMultipleAsync(
            storedProcedure,
            param,
            commandType: CommandType.StoredProcedure);
        return await read(multi);
    }
}
