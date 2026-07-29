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

    public Task AddRelationAsync(AddCaseRelationCommand c, IDbConnection conn, IDbTransaction tx)
        => ExecuteAsync("AddCaseRelation", new
        {
            RBSE = c.Rbse,
            RelationType = c.RelationType,
            RelationRBSE = c.RelationRbse,
            Sex = c.Sex,
            BirthDay = c.BirthDay,
            BirthMonth = c.BirthMonth,
            BirthYear = c.BirthYear,
            RelationFate = c.RelationFate,
            LeftDate = c.LeftDate,
            EartagCountry = c.EartagCountry,
            EartagHerdmark = c.EartagHerdmark,
            Eartag = c.Eartag,
            Sire = c.Sire
        }, conn, tx);

    public Task EditRelationAsync(EditCaseRelationCommand c, IDbConnection conn, IDbTransaction tx)
        => ExecuteAsync("EditCaseRelation", new
        {
            ID = c.Id,
            RelationType = c.RelationType,
            RelationRBSE = c.RelationRbse,
            Sex = c.Sex,
            BirthDay = c.BirthDay,
            BirthMonth = c.BirthMonth,
            BirthYear = c.BirthYear,
            RelationFate = c.RelationFate,
            LeftDate = c.LeftDate,
            EartagCountry = c.EartagCountry,
            EartagHerdmark = c.EartagHerdmark,
            Eartag = c.Eartag,
            Sire = c.Sire,
            RowStamp = c.RowStamp
        }, conn, tx);

    public Task DeleteRelationAsync(DeleteCaseRelationCommand c, IDbConnection conn, IDbTransaction tx)
        => ExecuteAsync("DeleteCaseRelation", new { ID = c.Id, RowStamp = c.RowStamp }, conn, tx);
}
