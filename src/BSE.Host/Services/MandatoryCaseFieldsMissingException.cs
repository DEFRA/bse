namespace BSE.Host.Services;

/// <summary>
/// Thrown by <see cref="ICaseEditOrchestrationService.CommitAllAsync"/> when the case (across all
/// tabs, not just the one Save was pressed on) is missing mandatory data — mirrors legacy
/// CaseEntrySave.aspx's <c>CheckMandatoryFields</c> check, which runs before every commit regardless
/// of which tab's Save button triggered it. No data is written when this is thrown.
/// </summary>
public sealed class MandatoryCaseFieldsMissingException(IReadOnlyList<string> errors) : Exception
{
    public IReadOnlyList<string> Errors { get; } = errors;
}
