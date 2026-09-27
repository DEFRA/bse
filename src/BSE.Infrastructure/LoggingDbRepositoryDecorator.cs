using System.Data;
using System.Diagnostics;
using Dapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BSE.Infrastructure;

public sealed class LoggingDbRepositoryDecorator : IDbRepository
{
    private readonly IDbRepository _inner;
    private readonly ILogger<LoggingDbRepositoryDecorator> _logger;
    private readonly int _slowThresholdMs;

    public LoggingDbRepositoryDecorator(
        IDbRepository inner,
        ILogger<LoggingDbRepositoryDecorator> logger,
        IConfiguration configuration)
    {
        _inner = inner;
        _logger = logger;
        _slowThresholdMs = configuration.GetValue<int?>("Logging:DbSlowQueryThresholdMs") ?? 1000;
    }

    public Task<IEnumerable<T>> QueryAsync<T>(string storedProcedure, object? param = null)
        => ExecuteWithLoggingAsync(
            storedProcedure,
            "Query",
            () => _inner.QueryAsync<T>(storedProcedure, param));

    public Task ExecuteAsync(string storedProcedure, object? param = null)
        => ExecuteWithLoggingAsync(
            storedProcedure,
            "Execute",
            () => _inner.ExecuteAsync(storedProcedure, param));

    public Task<T?> QuerySingleOrDefaultAsync<T>(string storedProcedure, object? param = null)
        where T : class
        => ExecuteWithLoggingAsync(
            storedProcedure,
            "QuerySingleOrDefault",
            () => _inner.QuerySingleOrDefaultAsync<T>(storedProcedure, param));

    public Task<IEnumerable<T>> QueryAsync<T>(string storedProcedure, object? param, int commandTimeoutSeconds)
        => ExecuteWithLoggingAsync(
            storedProcedure,
            "QueryWithTimeout",
            () => _inner.QueryAsync<T>(storedProcedure, param, commandTimeoutSeconds),
            commandTimeoutSeconds);

    public Task ExecuteWithOutputAsync(string storedProcedure, DynamicParameters param)
        => ExecuteWithLoggingAsync(
            storedProcedure,
            "ExecuteWithOutput",
            () => _inner.ExecuteWithOutputAsync(storedProcedure, param));

    public Task ExecuteWithOutputAsync(string storedProcedure, DynamicParameters param, IDbConnection connection, IDbTransaction? transaction)
        => ExecuteWithLoggingAsync(
            storedProcedure,
            "ExecuteWithOutputInTransaction",
            () => _inner.ExecuteWithOutputAsync(storedProcedure, param, connection, transaction));

    public Task ExecuteAsync(string storedProcedure, object? param, IDbConnection connection, IDbTransaction? transaction)
        => ExecuteWithLoggingAsync(
            storedProcedure,
            "ExecuteInTransaction",
            () => _inner.ExecuteAsync(storedProcedure, param, connection, transaction));

    public Task<int> ExecuteWithRowCountAsync(string storedProcedure, object? param, IDbConnection connection, IDbTransaction? transaction)
        => ExecuteWithLoggingAsync(
            storedProcedure,
            "ExecuteWithRowCountInTransaction",
            () => _inner.ExecuteWithRowCountAsync(storedProcedure, param, connection, transaction));

    public Task<T> QueryMultipleAsync<T>(string storedProcedure, object? param, Func<SqlMapper.GridReader, Task<T>> read)
        => ExecuteWithLoggingAsync(
            storedProcedure,
            "QueryMultiple",
            () => _inner.QueryMultipleAsync(storedProcedure, param, read));

    private async Task ExecuteWithLoggingAsync(string storedProcedure, string operation, Func<Task> execute, int? timeout = null)
    {
        var sw = Stopwatch.StartNew();

        try
        {
            await execute();
            sw.Stop();
            LogSuccess(storedProcedure, operation, sw.ElapsedMilliseconds, timeout);
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(
                ex,
                "Database {Operation} failed for {StoredProcedure} after {ElapsedMs}ms (Timeout={TimeoutSeconds})",
                operation,
                storedProcedure,
                sw.ElapsedMilliseconds,
                timeout);
            throw;
        }
    }

    private async Task<T> ExecuteWithLoggingAsync<T>(string storedProcedure, string operation, Func<Task<T>> execute, int? timeout = null)
    {
        var sw = Stopwatch.StartNew();

        try
        {
            var result = await execute();
            sw.Stop();
            LogSuccess(storedProcedure, operation, sw.ElapsedMilliseconds, timeout);
            return result;
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(
                ex,
                "Database {Operation} failed for {StoredProcedure} after {ElapsedMs}ms (Timeout={TimeoutSeconds})",
                operation,
                storedProcedure,
                sw.ElapsedMilliseconds,
                timeout);
            throw;
        }
    }

    private void LogSuccess(string storedProcedure, string operation, long elapsedMs, int? timeout)
    {
        if (elapsedMs >= _slowThresholdMs)
        {
            _logger.LogWarning(
                "Slow database {Operation} for {StoredProcedure} completed in {ElapsedMs}ms (ThresholdMs={ThresholdMs}, Timeout={TimeoutSeconds})",
                operation,
                storedProcedure,
                elapsedMs,
                _slowThresholdMs,
                timeout);
            return;
        }

        _logger.LogInformation(
            "Database {Operation} for {StoredProcedure} completed in {ElapsedMs}ms (Timeout={TimeoutSeconds})",
            operation,
            storedProcedure,
            elapsedMs,
            timeout);
    }
}
