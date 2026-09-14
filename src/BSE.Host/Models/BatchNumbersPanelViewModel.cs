using BSE.Modules.Batch.Models;

namespace BSE.Host.Models;

/// <summary>View model for the shared _BatchNumbersPanel partial (mirrors legacy BatchNumberDisplay control).</summary>
public sealed record BatchNumbersPanelViewModel(string Rbse, IReadOnlyList<BatchNumberEntry> BatchNumbers);
