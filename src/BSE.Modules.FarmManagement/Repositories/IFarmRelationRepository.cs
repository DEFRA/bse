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
    Task UpdateAsync(int id, string relatedCphh, byte[] rowStamp);
    Task DeleteAsync(int id, byte[] rowStamp);

    // Transactional variants, enlisted in a caller-supplied connection/transaction.
    Task AddAsync(string cphh, string relatedCphh, IDbConnection connection, IDbTransaction transaction);
    /// <summary>Returns rows affected — 0 means the row's RowStamp was stale (changed by another user).</summary>
    Task<int> UpdateAsync(int id, string relatedCphh, byte[] rowStamp, IDbConnection connection, IDbTransaction transaction);
    /// <summary>Returns rows affected — 0 means the row's RowStamp was stale (changed by another user).</summary>
    Task<int> DeleteAsync(int id, byte[] rowStamp, IDbConnection connection, IDbTransaction transaction);
}
