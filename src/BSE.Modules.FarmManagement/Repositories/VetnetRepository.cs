using BSE.Infrastructure;
using BSE.Modules.FarmManagement.Models;

namespace BSE.Modules.FarmManagement.Repositories;

/// <summary>
/// Dapper-backed repository for Vetnet herdmark stored procedure calls.
/// SP name matches the filename in src/BSE.Database/StoredProcedures/FarmManagement/ exactly.
/// </summary>
public sealed class VetnetRepository : DapperRepository, IVetnetRepository
{
    public VetnetRepository(IDbConnectionFactory connectionFactory)
        : base(connectionFactory) { }

    public Task<IEnumerable<VetnetRecord>> GetByCphhAsync(string cphh)
        => QueryAsync<VetnetRecord>("GetVetnetDetailsByCPHH", new { CPHH = cphh });
}
