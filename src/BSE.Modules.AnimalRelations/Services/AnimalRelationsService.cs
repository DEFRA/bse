using BSE.Modules.AnimalRelations.Models;
using BSE.Modules.AnimalRelations.Repositories;

namespace BSE.Modules.AnimalRelations.Services;

public sealed class AnimalRelationsService : IAnimalRelationsService
{
    private readonly IAnimalRelationsRepository _repository;

    public AnimalRelationsService(IAnimalRelationsRepository repository)
        => _repository = repository;

    public Task<IReadOnlyList<CaseRelationRecord>> GetRelationsByRbseAsync(string rbse)
        => _repository.GetRelationsByRbseAsync(rbse);

    public Task<RelationDetailsRecord> GetRelationsDetailsByRbseAsync(string rbse)
        => _repository.GetRelationsDetailsByRbseAsync(rbse);

    public Task<RelatedCaseDetailsRecord?> GetRelationDetailsOfRelatedCaseAsync(string rbse)
        => _repository.GetRelationDetailsOfRelatedCaseAsync(rbse);

    public Task<IReadOnlyList<BatchRelationRecord>> GetRelationsByBatchIdAsync(int batchId)
        => _repository.GetRelationsByBatchIdAsync(batchId);

    public Task<IReadOnlyList<BatchDamSireRecord>> GetDamSireDetailsByBatchIdAsync(int batchId)
        => _repository.GetDamSireDetailsByBatchIdAsync(batchId);

    public Task<IReadOnlyList<DamSireDetailRecord>> GetDamSireDetailsMatchesAsync(
        string? eartag, string? name, string? rbse, string? herdbook, string sex)
        => _repository.GetDamSireDetailsMatchesAsync(eartag, name, rbse, herdbook, sex);
}
