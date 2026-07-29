using BSE.Modules.FarmManagement.Models;
using BSE.SharedKernel;

namespace BSE.Modules.FarmManagement.Services;

/// <summary>
/// Public contract for farm management operations. This is the interface other modules
/// should depend on — not the individual repositories.
/// </summary>
public interface IFarmService
{
    Task<FarmRecord?> GetByCphhAsync(string cphh);
    Task<FarmDetailRecord> GetDetailsByCphhAsync(string cphh);
    Task<IEnumerable<FarmSummaryRecord>> GetByCphAsync(string cph);
    Task AddAsync(AddFarmCommand command, int userId);
    Task UpdateAsync(UpdateFarmCommand command, int userId);
    Task<ChangeCphhResult> ChangeCphhAsync(string oldCphh, string newCphh, int userId);
    Task<int> GetConfirmedCaseCountAsync(string cphh);
    Task<int> GetCaseCountByCphhAsync(string cphh);
    Task<IEnumerable<FarmRelationRecord>> GetRelatedFarmsAsync(string cphh);
    Task<IEnumerable<HerdSizeRecord>> GetHerdSizesAsync(string cphh);
    Task<IEnumerable<VetnetRecord>> GetVetnetDetailsAsync(string cphh);
}
