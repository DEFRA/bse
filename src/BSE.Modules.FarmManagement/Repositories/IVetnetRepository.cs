using BSE.Modules.FarmManagement.Models;

namespace BSE.Modules.FarmManagement.Repositories;

/// <summary>
/// Data access contract for Vetnet herdmark lookups.
/// SP name matches the filename in src/BSE.Database/StoredProcedures/FarmManagement/ exactly.
/// </summary>
public interface IVetnetRepository
{
    Task<IEnumerable<VetnetRecord>> GetByCphhAsync(string cphh);
}
