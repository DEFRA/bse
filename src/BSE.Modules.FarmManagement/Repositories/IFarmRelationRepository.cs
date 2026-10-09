using System.Data;
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

    /// <summary>Transactional variant, enlisted in a caller-supplied connection/transaction.</summary>
    Task AddAsync(string cphh, string relatedCphh, IDbConnection connection, IDbTransaction transaction);

    Task UpdateAsync(int id, string relatedCphh, byte[] rowStamp);

    /// <summary>Transactional variant, enlisted in a caller-supplied connection/transaction.</summary>
    Task UpdateAsync(int id, string relatedCphh, byte[] rowStamp, IDbConnection connection, IDbTransaction transaction);

    Task DeleteAsync(int id, byte[] rowStamp);

    /// <summary>Transactional variant, enlisted in a caller-supplied connection/transaction.</summary>
    Task DeleteAsync(int id, byte[] rowStamp, IDbConnection connection, IDbTransaction transaction);
}
