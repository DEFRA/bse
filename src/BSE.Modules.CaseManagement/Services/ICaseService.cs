using BSE.Modules.CaseManagement.Commands;
using BSE.Modules.CaseManagement.Enums;
using BSE.Modules.CaseManagement.Models;

namespace BSE.Modules.CaseManagement.Services;

public interface ICaseService
{
    Task<CaseDetailRecord?> GetCaseDetailsAsync(string rbse);
    Task<CaseRecord?> GetCaseAsync(string rbse);
    Task<FinalResultRecord?> GetFinalResultAsync(string rbse);

    /// <summary>
    /// Creates a new case and all child records within a single SQL transaction.
    /// All SP calls are enlisted in the same transaction — a failure at any step
    /// rolls back the entire operation (the transactional save fix, Slice 8 R01).
    /// </summary>
    Task<AddCaseResult> CreateCaseAsync(UpdateCaseDetailsCommand command, int userId);

    /// <summary>
    /// Updates the core case row and optional clinical/BAB/dam-sire child tables
    /// within a single SQL transaction. Returns <see cref="EditCaseResult.ConcurrencyConflict"/>
    /// if the RowStamp has changed since the record was loaded.
    /// </summary>
    Task<EditCaseResult> EditCaseAsync(EditCaseDetailsCommand command, int userId);

    Task<DeleteCaseResult> DeleteCaseAsync(string rbse, int userId);
    Task<MoveCaseResult> MoveCaseAsync(string rbse, string newCphh, int userId);

    /// <summary>
    /// Changes an RBSE number and cascades the rename across all 15 child tables.
    /// </summary>
    Task<ChangeRbseResult> ChangeRbseAsync(string oldRbse, string newRbse, int userId);
}
