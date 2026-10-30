using BSE.Modules.ReferenceData.Models;
using BSE.SharedKernel;

namespace BSE.Host.Pages.Admin;

/// <summary>What the shared pick-list form partials need from the Add and Edit page models.</summary>
public interface IPickListFormModel
{
    int TableId { get; }
    string SortColumn { get; }
    bool SortDesc { get; }
    int PageNumber { get; }
    Dictionary<string, string> Fields { get; }
    IReadOnlyList<PickListFormPageModelBase.FieldSpec> DisplayFields { get; }
    IReadOnlyList<LuBseRegion> BseRegionOptions { get; }
    IReadOnlyList<ILookupItem> AhoOptions { get; }
}
