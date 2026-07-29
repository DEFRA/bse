using BSE.Modules.Search.Models;

namespace BSE.Host.Models.ViewModels;

public class FarmSearchViewModel
{
    public string Cphh { get; set; } = "";
    public string OwnerName { get; set; } = "";
    public string Address { get; set; } = "";
    public string County { get; set; } = "";
    public string Herdmark { get; set; } = "";
    public bool IncludeNonGb { get; set; }

    public IReadOnlyList<FarmSearchResult> Results { get; set; } = [];
    public bool HasSearched { get; set; }

    public FarmSearchQuery ToQuery() => new(
        Cphh: Cphh,
        OwnerName: OwnerName,
        Address: Address,
        County: County,
        Herdmark: Herdmark,
        IncludeNonGbFarms: IncludeNonGb);
}
