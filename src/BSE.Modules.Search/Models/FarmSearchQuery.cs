namespace BSE.Modules.Search.Models;

/// <summary>Input parameters for GetSearchFarm SP.</summary>
public record FarmSearchQuery(
    string Cphh = "",
    string OwnerName = "",
    string Address = "",
    string County = "",
    string Herdmark = "",
    string NumericHerdmark = "",
    bool IsDealer = false,
    string Aho = "",
    bool IncludeNonGbFarms = false);
