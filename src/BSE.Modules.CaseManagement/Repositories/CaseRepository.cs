using System.Data;
using BSE.Infrastructure;
using BSE.Modules.CaseManagement.Commands;
using BSE.Modules.CaseManagement.Enums;
using BSE.Modules.CaseManagement.Models;
using Dapper;

namespace BSE.Modules.CaseManagement.Repositories;

/// <summary>
/// Dapper-backed repository for core case stored procedure calls.
/// SP names match filenames in src/BSE.Database/StoredProcedures/CaseManagement/ exactly.
///
/// GetCaseDetailsByRBSE uses QueryMultipleAsync to map all 11 result sets to CaseDetailRecord.
/// The 11 result sets come from: GetCaseByRBSE, GetClinicalByRBSE, GetBABByRBSE,
/// GetOtherOwnerByRBSE, GetFeedByRBSE, GetClinicalVisitByRBSE, GetDamDetailsByRBSE,
/// GetSireDetailsByRBSE, GetRelationsByRBSE, GetTestByRBSE, GetCaseWorkByRBSE.
///
/// AddCase and EditCase accept a caller-supplied IDbConnection + IDbTransaction so that
/// CaseService.UpdateCaseDetailsAsync can enlist them in the case-save transaction.
/// DeleteCase, MoveCase, and ChangeRBSE manage their own transactions internally.
/// </summary>
public sealed class CaseRepository : DapperRepository, ICaseRepository
{
    public CaseRepository(IDbConnectionFactory connectionFactory) : base(connectionFactory) { }

    public Task<CaseRecord?> GetCaseByRbseAsync(string rbse)
        => QuerySingleOrDefaultAsync<CaseRecord>("GetCaseByRBSE", new { RBSE = rbse });

    public Task<FinalResultRecord?> GetFinalResultByRbseAsync(string rbse)
        => QuerySingleOrDefaultAsync<FinalResultRecord>("GetFinalResultByRBSE", new { RBSE = rbse });

    public async Task<IReadOnlyList<CaseFarmSummaryRecord>> GetCasesByBatchIdAsync(int batchId)
        => (await QueryAsync<CaseFarmSummaryRecord>("GetCaseFarmByBatchID", new { BatchID = batchId })).ToList();

    public async Task<string?> GetLatestRbseForYearAsync(short year)
    {
        var r = await QuerySingleOrDefaultAsync<LatestReferenceRecord>("GetLatestRBSEForYear", new { Year = year });
        return r?.LatestReference;
    }

    public async Task<string?> GetLatestDbseForYearAsync(short year)
    {
        var r = await QuerySingleOrDefaultAsync<LatestReferenceRecord>("GetLatestDBSEForYear", new { Year = year });
        return r?.LatestReference;
    }

    public Task<CaseDetailRecord?> GetCaseDetailsByRbseAsync(string rbse)
        => QueryMultipleAsync<CaseDetailRecord?>("GetCaseDetailsByRBSE", new { RBSE = rbse }, async grid =>
        {
            var caseRecord  = await grid.ReadSingleOrDefaultAsync<CaseRecord>();
            var clinical    = await grid.ReadSingleOrDefaultAsync<CaseClinicalRecord>();
            var bab         = await grid.ReadSingleOrDefaultAsync<CaseBabRecord>();
            var otherOwners = (await grid.ReadAsync<OtherOwnerRecord>()).ToList();
            var feeds       = (await grid.ReadAsync<CaseFeedRecord>()).ToList();
            var visits      = (await grid.ReadAsync<ClinicalVisitRecord>()).ToList();
            var dam         = await grid.ReadSingleOrDefaultAsync<DamSireDetailRecord>();
            var sire        = await grid.ReadSingleOrDefaultAsync<DamSireDetailRecord>();
            var relations   = (await grid.ReadAsync<CaseRelationRecord>()).ToList();
            var tests       = (await grid.ReadAsync<CaseTestRecord>()).ToList();
            var caseWork    = await grid.ReadSingleOrDefaultAsync<CaseWorkRecord>();

            if (caseRecord is null) return null;
            return new CaseDetailRecord(caseRecord, clinical, bab, otherOwners, feeds,
                visits, dam, sire, relations, tests, caseWork);
        });

    public async Task<AddCaseResult> AddCaseAsync(
        AddCaseCommand c, int userId, IDbConnection connection, IDbTransaction transaction)
    {
        var p = BuildAddCaseParams(c, userId);
        await ExecuteAsync("AddCase", p, connection, transaction);
        return (AddCaseResult)p.Get<int>("ReturnValue");
    }

    public async Task<EditCaseResult> EditCaseAsync(
        EditCaseCommand c, int userId, IDbConnection connection, IDbTransaction transaction)
    {
        var p = BuildEditCaseParams(c, userId);
        await ExecuteAsync("EditCase", p, connection, transaction);
        return (EditCaseResult)p.Get<int>("ReturnValue");
    }

    public async Task<EditCaseResult> EditFinalResultAsync(
        EditFinalResultCommand c, int userId, IDbConnection connection, IDbTransaction transaction)
    {
        var p = new DynamicParameters();
        p.Add("ReturnValue", direction: ParameterDirection.ReturnValue);
        p.Add("@RBSE", c.Rbse);
        p.Add("@FinalResult", c.FinalResult);
        p.Add("@FinalResultDate", c.FinalResultDate);
        p.Add("@DBSE", c.Dbse);
        p.Add("@UserID", userId);
        await ExecuteAsync("EditCaseFinalResult", p, connection, transaction);
        return (EditCaseResult)p.Get<int>("ReturnValue");
    }

    public async Task<DeleteCaseResult> DeleteCaseAsync(string rbse, int userId)
    {
        var p = new DynamicParameters();
        p.Add("ReturnValue", direction: ParameterDirection.ReturnValue);
        p.Add("@RBSE", rbse);
        p.Add("@UserID", userId);
        await ExecuteWithOutputAsync("DeleteCase", p);
        return (DeleteCaseResult)p.Get<int>("ReturnValue");
    }

    public async Task<MoveCaseResult> MoveCaseAsync(string rbse, string newCphh, int userId)
    {
        var p = new DynamicParameters();
        p.Add("ReturnValue", direction: ParameterDirection.ReturnValue);
        p.Add("@RBSE", rbse);
        p.Add("@NewCPHH", newCphh);
        p.Add("@UserID", userId);
        await ExecuteWithOutputAsync("MoveCase", p);
        return (MoveCaseResult)p.Get<int>("ReturnValue");
    }

    public async Task<ChangeRbseResult> ChangeRbseAsync(string oldRbse, string newRbse, int userId)
    {
        var p = new DynamicParameters();
        p.Add("ReturnValue", direction: ParameterDirection.ReturnValue);
        p.Add("@OldRBSE", oldRbse);
        p.Add("@NewRBSE", newRbse);
        p.Add("@UserID", userId);
        await ExecuteWithOutputAsync("ChangeRBSE", p);
        return (ChangeRbseResult)p.Get<int>("ReturnValue");
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static DynamicParameters BuildAddCaseParams(AddCaseCommand c, int userId)
    {
        var p = new DynamicParameters();
        p.Add("ReturnValue", direction: ParameterDirection.ReturnValue);
        p.Add("@RBSE", c.Rbse); p.Add("@CPHH", c.Cphh);
        p.Add("@EartagCountry", c.EartagCountry); p.Add("@EartagHerdmark", c.EartagHerdmark);
        p.Add("@Eartag", c.Eartag); p.Add("@PreviousEartag", c.PreviousEartag);
        p.Add("@BSE1ReceivedDate", c.Bse1ReceivedDate); p.Add("@FormADate", c.FormADate);
        p.Add("@FormAResubmittedDate", c.FormAResubmittedDate); p.Add("@FormBDate", c.FormBDate);
        p.Add("@Fate", c.Fate); p.Add("@FormCDate", c.FormCDate);
        p.Add("@IsPurchaserBSE1Received", c.IsPurchaserBse1Received);
        p.Add("@IsBreederBSE1Received", c.IsBreederBse1Received);
        p.Add("@IsVendor1BSE1Received", c.IsVendor1Bse1Received);
        p.Add("@IsHomebredBSE1Received", c.IsHomebredBse1Received);
        p.Add("@IsSummarySheetReceived", c.IsSummarySheetReceived);
        p.Add("@IsPaperworkComplete", c.IsPaperworkComplete);
        p.Add("@ReportedLocation", c.ReportedLocation); p.Add("@Survey", c.Survey);
        p.Add("@Notes", c.Notes); p.Add("@BirthDate", c.BirthDate);
        p.Add("@IsBirthDateEst", c.IsBirthDateEst); p.Add("@DamStatus", c.DamStatus);
        p.Add("@BirthDateSource", c.BirthDateSource); p.Add("@ValuationAge", c.ValuationAge);
        p.Add("@Sex", c.Sex); p.Add("@Breed", c.Breed); p.Add("@Origin", c.Origin);
        p.Add("@PurchaseDate", c.PurchaseDate); p.Add("@PurchaseAgeInMonths", c.PurchaseAgeInMonths);
        p.Add("@PurchasedCounty", c.PurchasedCounty); p.Add("@HerdEntryDate", c.HerdEntryDate);
        p.Add("@OnsetDate", c.OnsetDate); p.Add("@IsOnsetDateEst", c.IsOnsetDateEst);
        p.Add("@MonthsPregnant", c.MonthsPregnant); p.Add("@MonthsPostCalving", c.MonthsPostCalving);
        p.Add("@OnsetAgeInMonths", c.OnsetAgeInMonths); p.Add("@SlaughterDate", c.SlaughterDate);
        p.Add("@UserID", userId); p.Add("@AlternateDiagnosis", c.AlternateDiagnosis);
        p.Add("@LabComment", c.LabComment); p.Add("@CaseType", c.CaseType);
        return p;
    }

    private static DynamicParameters BuildEditCaseParams(EditCaseCommand c, int userId)
    {
        var p = new DynamicParameters();
        p.Add("ReturnValue", direction: ParameterDirection.ReturnValue);
        p.Add("@RBSE", c.Rbse);
        p.Add("@EartagCountry", c.EartagCountry); p.Add("@EartagHerdmark", c.EartagHerdmark);
        p.Add("@Eartag", c.Eartag); p.Add("@PreviousEartag", c.PreviousEartag);
        p.Add("@BSE1ReceivedDate", c.Bse1ReceivedDate); p.Add("@FormADate", c.FormADate);
        p.Add("@FormAResubmittedDate", c.FormAResubmittedDate); p.Add("@FormBDate", c.FormBDate);
        p.Add("@Fate", c.Fate); p.Add("@FormCDate", c.FormCDate);
        p.Add("@IsPurchaserBSE1Received", c.IsPurchaserBse1Received);
        p.Add("@IsBreederBSE1Received", c.IsBreederBse1Received);
        p.Add("@IsVendor1BSE1Received", c.IsVendor1Bse1Received);
        p.Add("@IsHomebredBSE1Received", c.IsHomebredBse1Received);
        p.Add("@IsSummarySheetReceived", c.IsSummarySheetReceived);
        p.Add("@IsPaperworkComplete", c.IsPaperworkComplete);
        p.Add("@ReportedLocation", c.ReportedLocation); p.Add("@Survey", c.Survey);
        p.Add("@Notes", c.Notes); p.Add("@BirthDate", c.BirthDate);
        p.Add("@IsBirthDateEst", c.IsBirthDateEst); p.Add("@DamStatus", c.DamStatus);
        p.Add("@BirthDateSource", c.BirthDateSource); p.Add("@ValuationAge", c.ValuationAge);
        p.Add("@Sex", c.Sex); p.Add("@Breed", c.Breed); p.Add("@Origin", c.Origin);
        p.Add("@PurchaseDate", c.PurchaseDate); p.Add("@PurchaseAgeInMonths", c.PurchaseAgeInMonths);
        p.Add("@PurchasedCounty", c.PurchasedCounty); p.Add("@HerdEntryDate", c.HerdEntryDate);
        p.Add("@OnsetDate", c.OnsetDate); p.Add("@IsOnsetDateEst", c.IsOnsetDateEst);
        p.Add("@MonthsPregnant", c.MonthsPregnant); p.Add("@MonthsPostCalving", c.MonthsPostCalving);
        p.Add("@OnsetAgeInMonths", c.OnsetAgeInMonths); p.Add("@SlaughterDate", c.SlaughterDate);
        p.Add("@RowStamp", c.RowStamp, DbType.Binary);
        p.Add("@UserID", userId); p.Add("@AlternateDiagnosis", c.AlternateDiagnosis);
        p.Add("@LabComment", c.LabComment); p.Add("@CaseType", c.CaseType);
        return p;
    }
}
