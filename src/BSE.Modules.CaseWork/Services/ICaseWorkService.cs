using BSE.Modules.CaseWork.Commands;
using BSE.Modules.CaseWork.Models;

namespace BSE.Modules.CaseWork.Services;

public interface ICaseWorkService
{
    /// <summary>Returns the lightweight casework record for a case.</summary>
    Task<CaseWorkRecord?> GetCaseWorkAsync(string rbse);

    /// <summary>Returns the full casework entry with all dates and server-computed due dates.</summary>
    Task<CaseWorkEntryRecord?> GetCaseWorkEntryAsync(string rbse);

    /// <summary>
    /// Returns minute print details for the given minute type.
    /// Valid values: ActiveMemo, AnnexA, AnnexB, AnnexC, AnnexD, AMFS.
    /// </summary>
    Task<MinuteDetailsRecord?> GetMinuteDetailsAsync(string rbse, string minuteType);

    /// <summary>Sets the sent-date for a minute type to today's date.</summary>
    Task SetMinuteSentDateAsync(string rbse, string minuteType);

    /// <summary>Full casework entry update — includes IsCaseClosed audit logging via SP.</summary>
    Task EditCaseWorkEntryAsync(EditCaseWorkEntryCommand command);
}
