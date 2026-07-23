using BSE.Modules.FarmManagement.Models;

namespace BSE.Modules.FarmManagement.Repositories;

/// <summary>
/// Data access contract for herd size operations.
/// SP names match filenames in src/BSE.Database/StoredProcedures/FarmManagement/ exactly.
/// </summary>
public interface IHerdSizeRepository
{
    Task<IEnumerable<HerdSizeRecord>> GetByCphhAsync(string cphh);
    Task<IEnumerable<HerdDetailRecord>> GetByBatchIdAsync(int batchId);
    Task AddAsync(AddHerdSizeCommand command);
    Task UpdateAsync(UpdateHerdSizeCommand command);
    Task DeleteAsync(int id, byte[] rowStamp);
}
