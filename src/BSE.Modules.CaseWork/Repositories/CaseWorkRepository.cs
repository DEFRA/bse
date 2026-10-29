using System.Data;
using BSE.Infrastructure;
using BSE.Modules.CaseWork.Commands;
using BSE.Modules.CaseWork.Models;

namespace BSE.Modules.CaseWork.Repositories;

public sealed class CaseWorkRepository : DapperRepository, ICaseWorkRepository
{
    public CaseWorkRepository(IDbConnectionFactory connectionFactory) : base(connectionFactory) { }

    // ── Reads ──────────────────────────────────────────────────────────────────

    public Task<CaseWorkRecord?> GetByRbseAsync(string rbse)
        => QuerySingleOrDefaultAsync<CaseWorkRecord>("GetCaseWorkByRBSE", new { RBSE = rbse });

    public Task<CaseWorkEntryRecord?> GetEntryByRbseAsync(string rbse)
        => QuerySingleOrDefaultAsync<CaseWorkEntryRecord>("GetCaseWorkEntryByRBSE", new { RBSE = rbse });

    public async Task<MinuteDetailsRecord?> GetMinuteDetailsAsync(string rbse, string minuteType)
    {
        var rows = await QueryAsync<MinuteDetailsRecord>("GetMinuteDetails", new { RBSE = rbse, MinuteType = minuteType });
        return rows.FirstOrDefault();
    }

    // ── Updates ────────────────────────────────────────────────────────────────

    public Task SetMinuteSentDateAsync(string rbse, string minuteType)
        => ExecuteAsync("SetMinuteSentDate", new { RBSE = rbse, MinuteType = minuteType });

    public Task<IEnumerable<CaseWorkEntryRecord>> GetOpenCasesAsync()
        => QueryAsync<CaseWorkEntryRecord>("GetOpenCaseReportData");

    public Task<IEnumerable<CaseWorkEntryRecord>> GetClosedCasesAsync()
        => QueryAsync<CaseWorkEntryRecord>("GetClosedCaseReportData");

    public Task EditEntryAsync(EditCaseWorkEntryCommand command)
        => ExecuteAsync("EditCaseWorkEntry", new
        {
            RBSE = command.Rbse,
            Barcode = command.Barcode,
            AHFReference = command.AhfReference,
            PurchaserBSE1ReceivedDate = command.PurchaserBse1ReceivedDate,
            BreederBSE1ReceivedDate = command.BreederBse1ReceivedDate,
            Vendor1BSE1ReceivedDate = command.Vendor1Bse1ReceivedDate,
            HomebredBSE1ReceivedDate = command.HomebredBse1ReceivedDate,
            SummarySheetReceivedDate = command.SummarySheetReceivedDate,
            PaperworkCompleteDate = command.PaperworkCompleteDate,
            ActiveMemoDate = command.ActiveMemoDate,
            AnnexADate = command.AnnexADate,
            AnnexBDate = command.AnnexBDate,
            AnnexCDate = command.AnnexCDate,
            AnnexDDate = command.AnnexDDate,
            RegionalLab = command.RegionalLab,
            ReceivedByRegionalLabDate = command.ReceivedByRegionalLabDate,
            InitialReceivedDate = command.InitialReceivedDate,
            FinalReceivedDate = command.FinalReceivedDate,
            FinalSentDate = command.FinalSentDate,
            LabChasedDate = command.LabChasedDate,
            BarbMinuteSentDate = command.BarbMinuteSentDate,
            Post2000SentDate = command.Post2000SentDate,
            CaseWorkNotes = command.CaseWorkNotes,
            DataCompleteDate = command.DataCompleteDate,
            IsCaseClosed = command.IsCaseClosed,
            UserID = command.UserId,
            TseTestingSite = command.TseTestingSite,
            SamplingDate = command.SamplingDate,
            AHROId = command.AhroId
        });

    // ── Transactional writes ───────────────────────────────────────────────────

    public Task AddAsync(AddCaseWorkCommand command, IDbConnection connection, IDbTransaction transaction)
        => ExecuteAsync("AddCaseWork", new
        {
            RBSE = command.Rbse,
            RBSEDate = command.RbseDate,
            Barcode = command.Barcode,
            AHFReference = command.AhfReference,
            PurchaserBSE1ReceivedDate = command.PurchaserBse1ReceivedDate,
            BreederBSE1ReceivedDate = command.BreederBse1ReceivedDate,
            Vendor1BSE1ReceivedDate = command.Vendor1Bse1ReceivedDate,
            HomebredBSE1ReceivedDate = command.HomebredBse1ReceivedDate,
            SummarySheetReceivedDate = command.SummarySheetReceivedDate,
            PaperworkCompleteDate = command.PaperworkCompleteDate
        }, connection, transaction);

    public Task EditAsync(EditCaseWorkCommand command, IDbConnection connection, IDbTransaction transaction)
        => ExecuteAsync("EditCaseWork", new
        {
            RBSE = command.Rbse,
            RBSEDate = command.RbseDate,
            Barcode = command.Barcode,
            AHFReference = command.AhfReference,
            PurchaserBSE1ReceivedDate = command.PurchaserBse1ReceivedDate,
            BreederBSE1ReceivedDate = command.BreederBse1ReceivedDate,
            Vendor1BSE1ReceivedDate = command.Vendor1Bse1ReceivedDate,
            HomebredBSE1ReceivedDate = command.HomebredBse1ReceivedDate,
            SummarySheetReceivedDate = command.SummarySheetReceivedDate,
            PaperworkCompleteDate = command.PaperworkCompleteDate
        }, connection, transaction);

    public Task EditAsync(EditCaseWorkCommand command)
        => ExecuteAsync("EditCaseWork", new
        {
            RBSE = command.Rbse,
            RBSEDate = command.RbseDate,
            Barcode = command.Barcode,
            AHFReference = command.AhfReference,
            PurchaserBSE1ReceivedDate = command.PurchaserBse1ReceivedDate,
            BreederBSE1ReceivedDate = command.BreederBse1ReceivedDate,
            Vendor1BSE1ReceivedDate = command.Vendor1Bse1ReceivedDate,
            HomebredBSE1ReceivedDate = command.HomebredBse1ReceivedDate,
            SummarySheetReceivedDate = command.SummarySheetReceivedDate,
            PaperworkCompleteDate = command.PaperworkCompleteDate
        });
}
