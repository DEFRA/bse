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

    public Task AddAsync(AddCaseClinicalCommand command, IDbConnection connection, IDbTransaction transaction)
        => ExecuteAsync("AddCaseClinical", BuildClinicalParams(command.Rbse, command), connection, transaction);

    public async Task<string?> EditAsync(EditCaseClinicalCommand command, IDbConnection connection, IDbTransaction transaction)
    {
        try
        {
            var p = new DynamicParameters(BuildClinicalParams(command.Rbse, command));
            p.Add("@RowStamp", command.RowStamp, DbType.Binary);
            p.Add("RETURN_VALUE", dbType: DbType.Int32, direction: ParameterDirection.ReturnValue);
            await ExecuteWithOutputAsync("EditCaseClinical", p, connection, transaction);
            return p.Get<int>("RETURN_VALUE") switch
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

    public Task AddVisitAsync(AddClinicalVisitCommand command, IDbConnection connection, IDbTransaction transaction)
        => ExecuteAsync("AddClinicalVisit", new { RBSE = command.Rbse, VisitDate = command.VisitDate }, connection, transaction);

    public Task EditVisitAsync(EditClinicalVisitCommand command, IDbConnection connection, IDbTransaction transaction)
        => ExecuteAsync("EditClinicalVisit", new { ID = command.Id, VisitDate = command.VisitDate, RowStamp = command.RowStamp }, connection, transaction);

    public Task DeleteVisitAsync(int id, byte[] rowStamp, IDbConnection connection, IDbTransaction transaction)
        => ExecuteAsync("DeleteClinicalVisit", new { ID = id, RowStamp = rowStamp }, connection, transaction);

    private static object BuildClinicalParams(string rbse, dynamic command) => new
    {
        RBSE = rbse,
        Apprehension = command.Apprehension, HypersensitiveTouch = command.HypersensitiveTouch,
        HypersensitiveSound = command.HypersensitiveSound, Maniacal = command.Maniacal,
        PanicStricken = command.PanicStricken, TemperamentChange = command.TemperamentChange,
        AbnormalHeadCarriage = command.AbnormalHeadCarriage, EarTwitching = command.EarTwitching,
        EarsOddAngle = command.EarsOddAngle, AbnormalBehaviour = command.AbnormalBehaviour,
        HeadShyness = command.HeadShyness, LickingFlank = command.LickingFlank,
        LickingNose = command.LickingNose, Kicking = command.Kicking,
        ReluctantDoorways = command.ReluctantDoorways, HeadPressing = command.HeadPressing,
        HeadRubbing = command.HeadRubbing, TeethGrinding = command.TeethGrinding,
        Blindness = command.Blindness, Circling = command.Circling,
        HindAtaxia = command.HindAtaxia, Falling = command.Falling, Paresis = command.Paresis,
        ForeAtaxia = command.ForeAtaxia, Recumbent = command.Recumbent, Tremor = command.Tremor,
        KnucklingFetlock = command.KnucklingFetlock, WeightLoss = command.WeightLoss,
        ConditionLoss = command.ConditionLoss, MilkYield = command.MilkYield
    };
}
