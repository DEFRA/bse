using System.Data;
using BSE.Modules.CaseWork.Commands;
using BSE.Modules.CaseWork.Models;

namespace BSE.Modules.CaseWork.Repositories;

public interface ICaseWorkRepository
{
    // ── Reads ──────────────────────────────────────────────────────────────────

    /// <summary>Returns the lightweight casework record (GetCaseWorkByRBSE — 12 cols).</summary>
    Task<CaseWorkRecord?> GetByRbseAsync(string rbse);

    /// <summary>Returns the full casework entry with all dates and computed due dates (GetCaseWorkEntryByRBSE).</summary>
    Task<CaseWorkEntryRecord?> GetEntryByRbseAsync(string rbse);

    /// <summary>
    /// Returns minute print details for the given minute type (GetMinuteDetails).
    /// Valid values for <paramref name="minuteType"/>: ActiveMemo, AnnexA, AnnexB, AnnexC, AnnexD, AMFS.
    /// </summary>
    Task<MinuteDetailsRecord?> GetMinuteDetailsAsync(string rbse, string minuteType);

    // ── Updates ────────────────────────────────────────────────────────────────

    /// <summary>Sets the sent-date for a minute type to today's date (SetMinuteSentDate).</summary>
    Task SetMinuteSentDateAsync(string rbse, string minuteType);

    /// <summary>Full casework entry update including IsCaseClosed audit (EditCaseWorkEntry).</summary>
    Task EditEntryAsync(EditCaseWorkEntryCommand command);

    // ── Transactional writes (enlisted in caller's transaction) ────────────────

    Task AddAsync(AddCaseWorkCommand command, IDbConnection connection, IDbTransaction transaction);
    Task EditAsync(EditCaseWorkCommand command, IDbConnection connection, IDbTransaction transaction);
}
