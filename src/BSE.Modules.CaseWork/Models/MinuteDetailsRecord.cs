namespace BSE.Modules.CaseWork.Models;

/// <summary>
/// Maximal result record for <c>GetMinuteDetails</c>.
/// The SP is polymorphic — the column set varies by <c>@MinuteType</c>.
/// Dapper maps only the columns present in each result set; absent properties remain null.
///
/// <list type="table">
///   <listheader><term>MinuteType</term><description>Columns returned</description></listheader>
///   <item><term>ActiveMemo</term><description>Rbse, Cphh, EartagHerdmark, Eartag, BirthDate, SlaughterDate, SurveyDescription, AhoNumber, AhoName, OwnerName, ActiveMemoDate, EartagCountry</description></item>
///   <item><term>AnnexA</term><description>Rbse, AnnexADate, AhoName</description></item>
///   <item><term>AnnexB</term><description>Rbse, AnnexADate, AnnexBDate, AhoName</description></item>
///   <item><term>AnnexC</term><description>Rbse, AnnexCDate, AhoName</description></item>
///   <item><term>AnnexD</term><description>Rbse, AnnexCDate, AnnexDDate, AhoName</description></item>
///   <item><term>AMFS</term><description>Rbse, Cphh, EartagHerdmark, Eartag, BirthDate, SlaughterDate, SurveyDescription, TseTestingAhoName, FarmAhoName, OwnerName, ActiveMemoDate, EartagCountry, TseTestingSite, SamplingDate, FarmAhoNumber, TseAddress, TseCph, CaseWorkAhro, TestResult</description></item>
/// </list>
/// </summary>
public sealed record MinuteDetailsRecord
{
    public string? Rbse { get; init; }
    public string? Cphh { get; init; }
    public string? EartagHerdmark { get; init; }
    public string? Eartag { get; init; }
    public DateTime? BirthDate { get; init; }
    public DateTime? SlaughterDate { get; init; }
    public string? SurveyDescription { get; init; }
    /// <summary>AHO number (Farm.AHO) — ActiveMemo type only.</summary>
    public string? AhoNumber { get; init; }
    /// <summary>AHO name resolved via luAHO — ActiveMemo/Annex types.</summary>
    public string? AhoName { get; init; }
    public string? OwnerName { get; init; }
    public DateTime? ActiveMemoDate { get; init; }
    public string? EartagCountry { get; init; }
    public DateTime? AnnexADate { get; init; }
    public DateTime? AnnexBDate { get; init; }
    public DateTime? AnnexCDate { get; init; }
    public DateTime? AnnexDDate { get; init; }
    /// <summary>TSE testing site AHO name — AMFS type only.</summary>
    public string? TseTestingAhoName { get; init; }
    /// <summary>Farm AHO name — AMFS type only.</summary>
    public string? FarmAhoName { get; init; }
    public string? TseTestingSite { get; init; }
    public DateTime? SamplingDate { get; init; }
    public string? FarmAhoNumber { get; init; }
    /// <summary>TSE testing site address — AMFS type only.</summary>
    public string? TseAddress { get; init; }
    /// <summary>TSE testing site CPH — AMFS type only.</summary>
    public string? TseCph { get; init; }
    public string? CaseWorkAhro { get; init; }
    public string? TestResult { get; init; }
}
