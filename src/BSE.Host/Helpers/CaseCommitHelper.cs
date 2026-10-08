using BSE.Host.Pages.Case;
using BSE.Host.Services;
using BSE.Modules.CaseManagement.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BSE.Host.Helpers;

/// <summary>
/// Shared cross-tab commit handling used by every case-edit tab's Save handler (Case (DEFRA)/Farm/
/// Bab/Clinical/Feeds) — each stages its own fields/rows, then calls
/// <see cref="ICaseEditOrchestrationService.CommitAllAsync"/> and applies the same legacy-parity
/// redirect rules: missing mandatory fields and partial success both route to
/// <see cref="SaveResultModel"/>, a hard failure returns to Home with an error banner.
/// </summary>
public static class CaseCommitHelper
{
    /// <summary>
    /// Invokes <see cref="ICaseEditOrchestrationService.CommitAllAsync"/> and converts the
    /// exception/failure outcome into the shared SaveResult/Home redirects. Returns a null
    /// <c>Outcome</c> when the caller must redirect immediately (<c>FailureRedirect</c> set);
    /// otherwise returns the successful <see cref="CaseCommitOutcome"/> so the caller can run its
    /// own post-commit side effects (e.g. persisting staged child rows) before checking warnings
    /// via <see cref="TryStageWarnings"/>.
    /// </summary>
    public static async Task<(IActionResult? FailureRedirect, CaseCommitOutcome? Outcome)> CommitAllAsync(
        PageModel page,
        ICaseEditOrchestrationService orchestration,
        string rbse,
        int userId,
        Func<EditCaseResult, string>? failureMessage = null)
    {
        CaseCommitOutcome commitOutcome;
        try
        {
            commitOutcome = await orchestration.CommitAllAsync(rbse, userId);
        }
        catch (MandatoryCaseFieldsMissingException ex)
        {
            // Legacy parity: CaseEntrySave.aspx shows the consolidated list of missing items with
            // a "Return" button instead of a single inline banner.
            SaveResultModel.Stage(page.TempData, SaveResultMode.MissingMandatoryFields, ex.Errors);
            return (page.RedirectToPage("/Case/SaveResult", new { rbse }), null);
        }

        if (commitOutcome.Result != EditCaseResult.Success)
        {
            page.TempData["ErrorMessage"] = failureMessage?.Invoke(commitOutcome.Result)
                ?? $"Update failed: {commitOutcome.Result}";
            return (page.RedirectToPage("/Home"), null);
        }

        return (null, commitOutcome);
    }

    /// <summary>Legacy parity: CaseEntrySave.aspx shows "saved with some errors" instead of silently
    /// succeeding whenever a per-table concurrency conflict was skipped during the commit. Returns
    /// null when there are no warnings, so the caller continues with its normal success redirect.</summary>
    public static IActionResult? TryStageWarnings(PageModel page, CaseCommitOutcome outcome, string rbse)
    {
        if (outcome.Warnings.Count == 0)
            return null;

        SaveResultModel.Stage(page.TempData, SaveResultMode.PartialSuccess, outcome.Warnings);
        return page.RedirectToPage("/Case/SaveResult", new { rbse });
    }
}
