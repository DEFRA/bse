namespace BSE.Modules.Search.Models;

/// <summary>
/// Result class for GetSearchCaseByCPHH and GetSearchCaseByEartagHerdmark —
/// extended case result including purchase/onset dates and status fields.
/// Uses a class with settable properties so Dapper maps by column name,
/// avoiding constructor signature mismatches caused by SQL smallint vs int.
/// </summary>
public class CaseDetailSearchResult
{
    public string Rbse { get; set; } = "";
    public string Cphh { get; set; } = "";
    public string? Sex { get; set; }
    public string? Eartag { get; set; }
    public DateTime? BirthDate { get; set; }
    public DateTime? PurchaseDate { get; set; }
    public short? PurchaseAgeInMonths { get; set; }
    public DateTime? OnsetDate { get; set; }
    public DateTime? FormADate { get; set; }
    public DateTime? SlaughterDate { get; set; }
    public DateTime? FinalResultDate { get; set; }
    public short? OnsetAgeInMonths { get; set; }
    public string? Fate { get; set; }
    public string? FinalResult { get; set; }
    public string? Survey { get; set; }
    public string? CaseStatus { get; set; }
    public string? TimeElapsed { get; set; }
    public int? DaysElapsed { get; set; }
    public string? Origin { get; set; }
}
