using System.Data;
using BSE.Modules.CaseManagement.Commands;
using BSE.Modules.CaseManagement.Enums;
using BSE.Modules.CaseManagement.Models;

namespace BSE.Modules.CaseManagement.Repositories;

public interface ICaseRepository
{
    Task<CaseDetailRecord?> GetCaseDetailsByRbseAsync(string rbse);
    Task<CaseRecord?> GetCaseByRbseAsync(string rbse);
    Task<FinalResultRecord?> GetFinalResultByRbseAsync(string rbse);
    Task<IReadOnlyList<CaseFarmSummaryRecord>> GetCasesByBatchIdAsync(int batchId);
    Task<string?> GetLatestRbseForYearAsync(short year);
    Task<string?> GetLatestDbseForYearAsync(short year);

    /// <summary>
    /// Calls AddCase within the caller-supplied connection and transaction.
    /// Use from CaseService.UpdateCaseDetailsAsync where the whole save is transactional.
    /// </summary>
    Task<AddCaseResult> AddCaseAsync(AddCaseCommand command, int userId, IDbConnection connection, IDbTransaction transaction);

    /// <summary>
    /// Calls EditCase within the caller-supplied connection and transaction.
    /// Returns ConcurrencyConflict (3) when RowStamp does not match.
    /// </summary>
    Task<EditCaseResult> EditCaseAsync(EditCaseCommand command, int userId, IDbConnection connection, IDbTransaction transaction);

    Task<EditCaseResult> EditFinalResultAsync(EditFinalResultCommand command, int userId, IDbConnection connection, IDbTransaction transaction);

    Task<DeleteCaseResult> DeleteCaseAsync(string rbse, int userId);
    Task<MoveCaseResult> MoveCaseAsync(string rbse, string newCphh, int userId);
    Task<ChangeRbseResult> ChangeRbseAsync(string oldRbse, string newRbse, int userId);
}
