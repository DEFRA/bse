using System.Data;
using Dapper;

namespace BSE.Infrastructure;

public interface IDbRepository
{
    Task<IEnumerable<T>> QueryAsync<T>(string storedProcedure, object? param = null);
    Task ExecuteAsync(string storedProcedure, object? param = null);
    Task<T?> QuerySingleOrDefaultAsync<T>(string storedProcedure, object? param = null) where T : class;

    /// <summary>
    /// Executes a query with an explicit command timeout (seconds).
    /// Use for long-running search SPs such as GetSearchCase and GetSearchFarm.
    /// </summary>
    Task<IEnumerable<T>> QueryAsync<T>(string storedProcedure, object? param, int commandTimeoutSeconds);

    /// <summary>
    /// Executes a stored procedure that includes OUTPUT parameters.
    /// Caller passes a <see cref="DynamicParameters"/> instance; after the call, output
    /// values are read back from the same object via <c>param.Get&lt;T&gt;(name)</c>.
    /// </summary>
    Task ExecuteWithOutputAsync(string storedProcedure, DynamicParameters param);

    /// <summary>
    /// Executes a stored procedure on a caller-supplied <paramref name="connection"/> and
    /// optional <paramref name="transaction"/>. Use when the caller manages the transaction
    /// boundary (e.g. Case Management enlisting <c>AddBatchNumberLink</c> in the case-save
    /// transaction). The connection must already be open.
    /// </summary>
    Task ExecuteAsync(string storedProcedure, object? param, IDbConnection connection, IDbTransaction? transaction);

    /// <summary>
    /// Executes a stored procedure that returns multiple result sets and passes the
    /// <see cref="Dapper.SqlMapper.GridReader"/> to <paramref name="read"/> for processing.
    /// The GridReader is always disposed after <paramref name="read"/> completes.
    /// Use for composite SPs like <c>GetCaseDetailsByRBSE</c> that EXEC multiple child SPs.
    /// </summary>
    Task<T> QueryMultipleAsync<T>(string storedProcedure, object? param, Func<SqlMapper.GridReader, Task<T>> read);
}
