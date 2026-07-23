using BSE.Modules.FarmManagement.Models;
using BSE.Modules.FarmManagement.Repositories;
using BSE.SharedKernel;

namespace BSE.Modules.FarmManagement.Services;

/// <summary>
/// Orchestrates farm management operations across the farm, relation, herd size,
/// and vetnet repositories. Provides the public contract consumed by other modules
/// and (eventually) Razor Page handlers.
/// </summary>
public sealed class FarmService : IFarmService
{
    private readonly IFarmRepository _farmRepository;
    private readonly IFarmRelationRepository _relationRepository;
    private readonly IHerdSizeRepository _herdSizeRepository;
    private readonly IVetnetRepository _vetnetRepository;

    public FarmService(
        IFarmRepository farmRepository,
        IFarmRelationRepository relationRepository,
        IHerdSizeRepository herdSizeRepository,
        IVetnetRepository vetnetRepository)
    {
        _farmRepository = farmRepository;
        _relationRepository = relationRepository;
        _herdSizeRepository = herdSizeRepository;
        _vetnetRepository = vetnetRepository;
    }

    public Task<FarmRecord?> GetByCphhAsync(string cphh)
        => _farmRepository.GetByCphhAsync(cphh);

    public Task<FarmDetailRecord> GetDetailsByCphhAsync(string cphh)
        => _farmRepository.GetDetailsByCphhAsync(cphh);

    public Task<IEnumerable<FarmSummaryRecord>> GetByCphAsync(string cph)
        => _farmRepository.GetByCphAsync(cph);

    public Task AddAsync(AddFarmCommand command, int userId)
        => _farmRepository.AddAsync(command, userId);

    public Task UpdateAsync(UpdateFarmCommand command, int userId)
        => _farmRepository.UpdateAsync(command, userId);

    public Task<ChangeCphhResult> ChangeCphhAsync(string oldCphh, string newCphh, int userId)
        => _farmRepository.ChangeCphhAsync(oldCphh, newCphh, userId);

    public Task<int> GetConfirmedCaseCountAsync(string cphh)
        => _farmRepository.GetConfirmedCaseCountAsync(cphh);

    public Task<int> GetCaseCountByCphhAsync(string cphh)
        => _farmRepository.GetCaseCountByCphhAsync(cphh);

    public Task<IEnumerable<FarmRelationRecord>> GetRelatedFarmsAsync(string cphh)
        => _relationRepository.GetRelatedFarmAsync(cphh);

    public Task<IEnumerable<HerdSizeRecord>> GetHerdSizesAsync(string cphh)
        => _herdSizeRepository.GetByCphhAsync(cphh);

    public Task<IEnumerable<VetnetRecord>> GetVetnetDetailsAsync(string cphh)
        => _vetnetRepository.GetByCphhAsync(cphh);
}
