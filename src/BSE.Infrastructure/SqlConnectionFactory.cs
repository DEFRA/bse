using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace BSE.Infrastructure;

/// <summary>
/// Creates <see cref="SqlConnection"/> instances for the BSE database.
/// </summary>
/// <remarks>
/// Managed Identity (Azure App Service):
///   System-assigned — set <c>Authentication=Active Directory Managed Identity</c> in the connection string.
///   User-assigned   — additionally set <c>User Id=&lt;client-id&gt;</c> in the connection string.
///
/// Local development — use SQL auth or Windows auth; omit the Authentication keyword.
/// The driver selects the correct auth path automatically based on the connection string.
/// </remarks>
public sealed class SqlConnectionFactory : IDbConnectionFactory
{
    private readonly string _connectionString;

    public SqlConnectionFactory(IConfiguration configuration)
    {
        _connectionString = configuration["ConnectionStrings:BSE"]
            ?? throw new InvalidOperationException(
                "ConnectionStrings:BSE is not configured. " +
                "Set the connection string in appsettings.json or the ConnectionStrings__BSE environment variable. " +
                "For Managed Identity add: Authentication=Active Directory Managed Identity");
    }

    public IDbConnection CreateConnection() => new SqlConnection(_connectionString);
}
