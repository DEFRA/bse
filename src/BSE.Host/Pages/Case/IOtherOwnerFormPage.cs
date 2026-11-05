using BSE.Modules.ReferenceData.Models;

namespace BSE.Host.Pages.Case;

/// <summary>What the shared owner form partial needs from the OtherOwner add and edit pages.</summary>
public interface IOtherOwnerFormPage
{
    string Rbse { get; }
    string? OwnerType { get; }
    string? OwnerName { get; }
    string? OwnerCphh { get; }
    IEnumerable<LookupItem> OwnerTypes { get; }
}
