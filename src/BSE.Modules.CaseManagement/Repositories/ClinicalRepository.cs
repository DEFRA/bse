using System.Data;
using BSE.Infrastructure;
using BSE.Modules.CaseManagement.Commands;
using BSE.Modules.CaseManagement.Models;
using Dapper;

namespace BSE.Modules.CaseManagement.Repositories;

public interface IClinicalRepository
{
    Task<CaseClinicalRecord?> GetByRbseAsync(string rbse);
    Task<IReadOnlyList<ClinicalVisitRecord>> GetVisitsByRbseAsync(string rbse);
    Task AddAsync(AddCaseClinicalCommand command, IDbConnection connection, IDbTransaction transaction);
    Task EditAsync(EditCaseClinicalCommand command, IDbConnection connection, IDbTransaction transaction);
    Task AddVisitAsync(AddClinicalVisitCommand command, IDbConnection connection, IDbTransaction transaction);
    Task EditVisitAsync(EditClinicalVisitCommand command, IDbConnection connection, IDbTransaction transaction);
    Task DeleteVisitAsync(int id, IDbConnection connection, IDbTransaction transaction);
}

public sealed class ClinicalRepository : DapperRepository, IClinicalRepository
{
    public ClinicalRepository(IDbConnectionFactory connectionFactory) : base(connectionFactory) { }

    public Task<CaseClinicalRecord?> GetByRbseAsync(string rbse)
        => QuerySingleOrDefaultAsync<CaseClinicalRecord>("GetClinicalByRBSE", new { RBSE = rbse });

    public async Task<IReadOnlyList<ClinicalVisitRecord>> GetVisitsByRbseAsync(string rbse)
        => (await QueryAsync<ClinicalVisitRecord>("GetClinicalVisitByRBSE", new { RBSE = rbse })).ToList();

    public Task AddAsync(AddCaseClinicalCommand c, IDbConnection conn, IDbTransaction tx)
        => ExecuteAsync("AddCaseClinical", BuildClinicalParams(c.Rbse, c), conn, tx);

    public Task EditAsync(EditCaseClinicalCommand c, IDbConnection conn, IDbTransaction tx)
    {
        var p = new DynamicParameters(BuildClinicalParams(c.Rbse, c));
        p.Add("@RowStamp", c.RowStamp, DbType.Binary);
        return ExecuteAsync("EditCaseClinical", p, conn, tx);
    }

    public Task AddVisitAsync(AddClinicalVisitCommand c, IDbConnection conn, IDbTransaction tx)
        => ExecuteAsync("AddClinicalVisit", new { RBSE = c.Rbse, VisitDate = c.VisitDate }, conn, tx);

    public Task EditVisitAsync(EditClinicalVisitCommand c, IDbConnection conn, IDbTransaction tx)
        => ExecuteAsync("EditClinicalVisit", new { ID = c.Id, VisitDate = c.VisitDate, RowStamp = c.RowStamp }, conn, tx);

    public Task DeleteVisitAsync(int id, IDbConnection conn, IDbTransaction tx)
        => ExecuteAsync("DeleteClinicalVisit", new { ID = id }, conn, tx);

    private static object BuildClinicalParams(string rbse, dynamic c) => new
    {
        RBSE = rbse,
        Apprehension = c.Apprehension, HypersensitiveTouch = c.HypersensitiveTouch,
        HypersensitiveSound = c.HypersensitiveSound, Maniacal = c.Maniacal,
        PanicStricken = c.PanicStricken, TemperamentChange = c.TemperamentChange,
        AbnormalHeadCarriage = c.AbnormalHeadCarriage, EarTwitching = c.EarTwitching,
        EarsOddAngle = c.EarsOddAngle, AbnormalBehaviour = c.AbnormalBehaviour,
        HeadShyness = c.HeadShyness, LickingFlank = c.LickingFlank,
        LickingNose = c.LickingNose, Kicking = c.Kicking,
        ReluctantDoorways = c.ReluctantDoorways, HeadPressing = c.HeadPressing,
        HeadRubbing = c.HeadRubbing, TeethGrinding = c.TeethGrinding,
        Blindness = c.Blindness, Circling = c.Circling,
        HindAtaxia = c.HindAtaxia, Falling = c.Falling, Paresis = c.Paresis,
        ForeAtaxia = c.ForeAtaxia, Recumbent = c.Recumbent, Tremor = c.Tremor,
        KnucklingFetlock = c.KnucklingFetlock, WeightLoss = c.WeightLoss,
        ConditionLoss = c.ConditionLoss, MilkYield = c.MilkYield
    };
}
