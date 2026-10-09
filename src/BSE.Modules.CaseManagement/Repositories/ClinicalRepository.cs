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
    /// <summary>Returns null on success, or a legacy-parity warning message if the EditCaseClinical SP
    /// reports the row couldn't be updated — legacy wraps this whole update in a blanket try/catch and
    /// always treats failures here as soft/non-fatal, so this method never throws for an SP-level
    /// failure.</summary>
    Task<string?> EditAsync(EditCaseClinicalCommand command, IDbConnection connection, IDbTransaction transaction);
    Task AddVisitAsync(AddClinicalVisitCommand command, IDbConnection connection, IDbTransaction transaction);
    Task EditVisitAsync(EditClinicalVisitCommand command, IDbConnection connection, IDbTransaction transaction);
    Task DeleteVisitAsync(int id, byte[] rowStamp, IDbConnection connection, IDbTransaction transaction);
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

    public async Task<string?> EditAsync(EditCaseClinicalCommand c, IDbConnection conn, IDbTransaction tx)
    {
        try
        {
            var p = new DynamicParameters(BuildClinicalParams(c.Rbse, c));
            p.Add("@RowStamp", c.RowStamp, DbType.Binary);
            p.Add("RETURN_VALUE", dbType: DbType.Int32, direction: ParameterDirection.ReturnValue);
            await ExecuteWithOutputAsync("EditCaseClinical", p, conn, tx);
            return (int)p.Get<int>("RETURN_VALUE") switch
            {
                0 => null,
                1 => "Failed to update the Clinical table.  The data may have been changed by another user.",
                2 => "Failed to update the Clinical table.",
                var code => $"Failed to update the Clinical table (code {code})."
            };
        }
        catch (Exception ex)
        {
            // Legacy parity: clsCase.UpdateClinicalRecord wraps the whole update in a try/catch and
            // treats any failure here as soft/non-fatal — never rolls back the rest of the save.
            return ex.Message;
        }
    }

    public Task AddVisitAsync(AddClinicalVisitCommand c, IDbConnection conn, IDbTransaction tx)
        => ExecuteAsync("AddClinicalVisit", new { RBSE = c.Rbse, VisitDate = c.VisitDate }, conn, tx);

    public Task EditVisitAsync(EditClinicalVisitCommand c, IDbConnection conn, IDbTransaction tx)
        => ExecuteAsync("EditClinicalVisit", new { ID = c.Id, VisitDate = c.VisitDate, RowStamp = c.RowStamp }, conn, tx);

    public Task DeleteVisitAsync(int id, byte[] rowStamp, IDbConnection conn, IDbTransaction tx)
        => ExecuteAsync("DeleteClinicalVisit", new { ID = id, RowStamp = rowStamp }, conn, tx);

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
