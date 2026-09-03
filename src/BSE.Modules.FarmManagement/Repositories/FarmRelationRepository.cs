using BSE.Infrastructure;
using BSE.Modules.FarmManagement.Models;

namespace BSE.Modules.FarmManagement.Repositories;

/// <summary>
/// Dapper-backed repository for farm relation stored procedure calls.
/// SP names match filenames in src/BSE.Database/StoredProcedures/FarmManagement/ exactly.
/// </summary>
public sealed class FarmRelationRepository : DapperRepository, IFarmRelationRepository
{
    public FarmRelationRepository(IDbConnectionFactory connectionFactory)
        : base(connectionFactory) { }

    public Task<IEnumerable<FarmRelationRecord>> GetRelatedFarmAsync(string cphh)
        => QueryAsync<FarmRelationRecord>("GetRelatedFarm", new { CPHH = cphh });

    public Task AddAsync(string cphh, string relatedCphh)
        => ExecuteAsync("AddFarmRelation", new { CPHH = cphh, RelatedCPHH = relatedCphh });

    public Task UpdateAsync(int id, string relatedCphh, byte[] rowStamp)
        => ExecuteAsync("EditFarmRelation", new { ID = id, RelatedCPHH = relatedCphh, RowStamp = rowStamp });

    public Task DeleteAsync(int id, byte[] rowStamp)
        => ExecuteAsync("DeleteFarmRelation", new { ID = id, RowStamp = rowStamp });
}
