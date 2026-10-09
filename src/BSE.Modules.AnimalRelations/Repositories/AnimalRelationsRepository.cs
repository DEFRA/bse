using System.Data;
using BSE.Infrastructure;
using BSE.Modules.AnimalRelations.Commands;
using BSE.Modules.AnimalRelations.Models;

namespace BSE.Modules.AnimalRelations.Repositories;

public sealed class AnimalRelationsRepository : DapperRepository, IAnimalRelationsRepository
{
    public AnimalRelationsRepository(IDbConnectionFactory connectionFactory) : base(connectionFactory) { }

    // ── Reads ──────────────────────────────────────────────────────────────────

    public async Task<IReadOnlyList<CaseRelationRecord>> GetRelationsByRbseAsync(string rbse)
        => (await QueryAsync<CaseRelationRecord>("GetRelationsByRBSE", new { RBSE = rbse })).ToList();

    public Task<RelationDetailsRecord> GetRelationsDetailsByRbseAsync(string rbse)
        => QueryMultipleAsync("GetRelationsDetailsByRBSE", new { RBSE = rbse }, async grid =>
        {
            var dam = (await grid.ReadAsync<DamSireDetailRecord>()).FirstOrDefault();
            var sire = (await grid.ReadAsync<DamSireDetailRecord>()).FirstOrDefault();
            var relations = (await grid.ReadAsync<CaseRelationRecord>()).ToList();
            return new RelationDetailsRecord(dam, sire, relations);
        });

    public Task<RelatedCaseDetailsRecord?> GetRelationDetailsOfRelatedCaseAsync(string rbse)
        => QuerySingleOrDefaultAsync<RelatedCaseDetailsRecord>("GetRelationDetailsOfRelatedCase", new { RBSE = rbse });

    public async Task<IReadOnlyList<BatchRelationRecord>> GetRelationsByBatchIdAsync(int batchId)
        => (await QueryAsync<BatchRelationRecord>("GetRelationsByBatchID", new { BatchID = batchId })).ToList();

    public async Task<IReadOnlyList<BatchDamSireRecord>> GetDamSireDetailsByBatchIdAsync(int batchId)
        => (await QueryAsync<BatchDamSireRecord>("GetDamSireDetailsByBatchID", new { BatchID = batchId })).ToList();

    public async Task<IReadOnlyList<DamSireDetailRecord>> GetDamSireDetailsMatchesAsync(
        string? eartag, string? name, string? rbse, string? herdbook, string sex)
        => (await QueryAsync<DamSireDetailRecord>("GetDamSireDetailsMatches", new
        {
            Eartag = eartag,
            Name = name,
            RBSE = rbse,
            Herdbook = herdbook,
            Sex = sex
        })).ToList();

    // ── Writes ─────────────────────────────────────────────────────────────────

    public Task AddRelationAsync(AddCaseRelationCommand command, IDbConnection connection, IDbTransaction transaction)
        => ExecuteAsync("AddCaseRelation", new
        {
            RBSE = command.Rbse,
            RelationType = command.RelationType,
            RelationRBSE = command.RelationRbse,
            Sex = command.Sex,
            BirthDay = command.BirthDay,
            BirthMonth = command.BirthMonth,
            BirthYear = command.BirthYear,
            RelationFate = command.RelationFate,
            LeftDate = command.LeftDate,
            EartagCountry = command.EartagCountry,
            EartagHerdmark = command.EartagHerdmark,
            Eartag = command.Eartag,
            Sire = command.Sire
        }, connection, transaction);

    public Task<int> EditRelationAsync(EditCaseRelationCommand command, IDbConnection connection, IDbTransaction transaction)
        => ExecuteWithRowCountAsync("EditCaseRelation", new
        {
            ID = command.Id,
            RelationType = command.RelationType,
            RelationRBSE = command.RelationRbse,
            Sex = command.Sex,
            BirthDay = command.BirthDay,
            BirthMonth = command.BirthMonth,
            BirthYear = command.BirthYear,
            RelationFate = command.RelationFate,
            LeftDate = command.LeftDate,
            EartagCountry = command.EartagCountry,
            EartagHerdmark = command.EartagHerdmark,
            Eartag = command.Eartag,
            Sire = command.Sire,
            RowStamp = command.RowStamp
        }, connection, transaction);

    public Task<int> DeleteRelationAsync(DeleteCaseRelationCommand command, IDbConnection connection, IDbTransaction transaction)
        => ExecuteWithRowCountAsync("DeleteCaseRelation", new { ID = command.Id, RowStamp = command.RowStamp }, connection, transaction);
}
