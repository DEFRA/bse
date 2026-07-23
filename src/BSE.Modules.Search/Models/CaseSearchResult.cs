namespace BSE.Modules.Search.Models;

/// <summary>Result record for GetSearchCase — the primary 17-parameter case search.</summary>
public record CaseSearchResult(
    string Rbse,
    string Cphh,
    string? Sex,
    string? Survey,
    string? Eartag,
    DateTime? BirthDate,
    string? IsBirthDateEst,
    DateTime? FormADate,
    string? Fate,
    string? FinalResult,
    DateTime? FinalResultDate,
    string? Dbse,
    string? Notes,
    string? BabNotes,
    string? Origin,
    string? ValuationAge);
