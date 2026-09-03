namespace BSE.Modules.FarmManagement.Models;

/// <summary>
/// Composite farm detail record, equivalent to the three result sets produced by
/// <c>GetFarmDetailsByCPHH</c> (which internally calls <c>GetFarmByCPHH</c>,
/// <c>GetRelatedFarm</c>, and <c>GetHerdSizeByCPHH</c>).
/// The repository populates this by calling the three individual stored procedures.
/// </summary>
public record FarmDetailRecord(
    FarmRecord? Farm,
    IEnumerable<FarmRelationRecord> RelatedFarms,
    IEnumerable<HerdSizeRecord> HerdSizes
);
