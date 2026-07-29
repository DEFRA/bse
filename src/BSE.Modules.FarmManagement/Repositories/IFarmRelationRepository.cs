using BSE.Modules.FarmManagement.Models;

namespace BSE.Modules.FarmManagement.Repositories;

/// <summary>
/// Data access contract for farm relation operations.
/// SP names match filenames in src/BSE.Database/StoredProcedures/FarmManagement/ exactly.
/// </summary>
public interface IFarmRelationRepository
{
    Task<IEnumerable<FarmRelationRecord>> GetRelatedFarmAsync(string cphh);
    Task AddAsync(string cphh, string relatedCphh);
    Task UpdateAsync(int id, string relatedCphh, byte[] rowStamp);
    Task DeleteAsync(int id, byte[] rowStamp);
}
