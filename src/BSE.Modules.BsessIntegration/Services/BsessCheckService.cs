using BSE.Modules.BsessIntegration.Models;
using BSE.Modules.BsessIntegration.Repositories;

namespace BSE.Modules.BsessIntegration.Services;

public sealed class BsessCheckService : IBsessCheckService
{
    private readonly IBsessRepository _repository;

    public BsessCheckService(IBsessRepository repository)
    {
        _repository = repository;
    }

    public Task<BsessCheckByRbseResult?> GetCheckByRbseAsync(string rbse, CancellationToken cancellationToken = default)
        => _repository.GetCheckByRbseAsync(rbse, cancellationToken);

    public Task<IReadOnlyList<BsessDiscrepancyRecord>> GetCheckByDateAsync(
        DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
        => _repository.GetCheckByDateAsync(startDate, endDate, cancellationToken);
}
