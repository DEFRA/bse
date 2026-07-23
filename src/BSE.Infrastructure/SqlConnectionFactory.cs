using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace BSE.Infrastructure;

public sealed class SqlConnectionFactory : IDbConnectionFactory
{
    private readonly string _connectionString;

    public SqlConnectionFactory(IConfiguration configuration)
    {
        _connectionString = configuration["ConnectionStrings:BSE"]
            ?? throw new InvalidOperationException(
                "ConnectionStrings:BSE is not configured. " +
                "Set the BSE connection string in appsettings.json or via the BSE_CONNECTION_STRING environment variable.");
    }

    public IDbConnection CreateConnection() => new SqlConnection(_connectionString);
}
