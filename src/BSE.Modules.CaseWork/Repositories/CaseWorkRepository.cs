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

    public Task<MinuteDetailsRecord?> GetMinuteDetailsAsync(string rbse, string minuteType)
        => QuerySingleOrDefaultAsync<MinuteDetailsRecord>("GetMinuteDetails", new { RBSE = rbse, MinuteType = minuteType });

    // ── Updates ────────────────────────────────────────────────────────────────

    public Task SetMinuteSentDateAsync(string rbse, string minuteType)
        => ExecuteAsync("SetMinuteSentDate", new { RBSE = rbse, MinuteType = minuteType });

    public Task EditEntryAsync(EditCaseWorkEntryCommand c)
        => ExecuteAsync("EditCaseWorkEntry", new
        {
            RBSE = c.Rbse,
            Barcode = c.Barcode,
            AHFReference = c.AhfReference,
            PurchaserBSE1ReceivedDate = c.PurchaserBse1ReceivedDate,
            BreederBSE1ReceivedDate = c.BreederBse1ReceivedDate,
            Vendor1BSE1ReceivedDate = c.Vendor1Bse1ReceivedDate,
            HomebredBSE1ReceivedDate = c.HomebredBse1ReceivedDate,
            SummarySheetReceivedDate = c.SummarySheetReceivedDate,
            PaperworkCompleteDate = c.PaperworkCompleteDate,
            ActiveMemoDate = c.ActiveMemoDate,
            AnnexADate = c.AnnexADate,
            AnnexBDate = c.AnnexBDate,
            AnnexCDate = c.AnnexCDate,
            AnnexDDate = c.AnnexDDate,
            RegionalLab = c.RegionalLab,
            ReceivedByRegionalLabDate = c.ReceivedByRegionalLabDate,
            InitialReceivedDate = c.InitialReceivedDate,
            FinalReceivedDate = c.FinalReceivedDate,
            FinalSentDate = c.FinalSentDate,
            LabChasedDate = c.LabChasedDate,
            BarbMinuteSentDate = c.BarbMinuteSentDate,
            Post2000SentDate = c.Post2000SentDate,
            CaseWorkNotes = c.CaseWorkNotes,
            DataCompleteDate = c.DataCompleteDate,
            IsCaseClosed = c.IsCaseClosed,
            UserID = c.UserId,
            TseTestingSite = c.TseTestingSite,
            SamplingDate = c.SamplingDate,
            AHROId = c.AhroId
        });

    // ── Transactional writes ───────────────────────────────────────────────────

    public Task AddAsync(AddCaseWorkCommand c, IDbConnection conn, IDbTransaction tx)
        => ExecuteAsync("AddCaseWork", new
        {
            RBSE = c.Rbse,
            RBSEDate = c.RbseDate,
            Barcode = c.Barcode,
            AHFReference = c.AhfReference,
            PurchaserBSE1ReceivedDate = c.PurchaserBse1ReceivedDate,
            BreederBSE1ReceivedDate = c.BreederBse1ReceivedDate,
            Vendor1BSE1ReceivedDate = c.Vendor1Bse1ReceivedDate,
            HomebredBSE1ReceivedDate = c.HomebredBse1ReceivedDate,
            SummarySheetReceivedDate = c.SummarySheetReceivedDate,
            PaperworkCompleteDate = c.PaperworkCompleteDate
        }, conn, tx);

    public Task EditAsync(EditCaseWorkCommand c, IDbConnection conn, IDbTransaction tx)
        => ExecuteAsync("EditCaseWork", new
        {
            RBSE = c.Rbse,
            RBSEDate = c.RbseDate,
            Barcode = c.Barcode,
            AHFReference = c.AhfReference,
            PurchaserBSE1ReceivedDate = c.PurchaserBse1ReceivedDate,
            BreederBSE1ReceivedDate = c.BreederBse1ReceivedDate,
            Vendor1BSE1ReceivedDate = c.Vendor1Bse1ReceivedDate,
            HomebredBSE1ReceivedDate = c.HomebredBse1ReceivedDate,
            SummarySheetReceivedDate = c.SummarySheetReceivedDate,
            PaperworkCompleteDate = c.PaperworkCompleteDate
        }, conn, tx);
}
