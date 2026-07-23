namespace BSE.Modules.Search.Models;

/// <summary>Result record for GetSearchFarm — farm search with case counts.</summary>
public record FarmSearchResult(
    string Cphh,
    string? OwnerName,
    string? Address,
    string? County,
    string? Herdmark,
    string? NumericHerdmark,
    string? MapReference,
    string? Aho,
    string? HerdType,
    string? CorrespondenceAddress,
    int CasesCount,
    int ConfirmedCasesCount);
