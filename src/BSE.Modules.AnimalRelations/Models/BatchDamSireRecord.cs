namespace BSE.Modules.AnimalRelations.Models;

/// <summary>
/// One row from <c>GetDamSireDetailsByBatchID</c> — the dam and sire pedigree for each case
/// in a batch, returned as a flat row. Nullable sire/dam fields indicate that no pedigree
/// record exists for that animal.
/// </summary>
public sealed record BatchDamSireRecord
{
    // ── Case ──────────────────────────────────────────────────────────────────
    public string Rbse { get; init; } = string.Empty;
    public string? DisplayRbse { get; init; }
    public string? Cphh { get; init; }
    public string? DisplayCphh { get; init; }

    // ── Sire ──────────────────────────────────────────────────────────────────
    public int? SireId { get; init; }
    public string? SireRbse { get; init; }
    public string? SireDisplayRbse { get; init; }
    public string? SireEartag { get; init; }
    public string? SireName { get; init; }
    /// <summary>Pre-formatted birth date string (DD/MM/YYYY or partial) produced by the SP.</summary>
    public string? SireBirthDate { get; init; }
    public string? SireFate { get; init; }
    public string? SireHerdbook { get; init; }
    public string? SireAlternativeName { get; init; }

    // ── Dam ───────────────────────────────────────────────────────────────────
    public int? DamId { get; init; }
    public string? DamRbse { get; init; }
    public string? DamDisplayRbse { get; init; }
    public string? DamEartag { get; init; }
    public string? DamName { get; init; }
    /// <summary>Pre-formatted birth date string (DD/MM/YYYY or partial) produced by the SP.</summary>
    public string? DamBirthDate { get; init; }
    public string? DamFate { get; init; }
    public string? DamStatus { get; init; }
    public string? DamHerdbook { get; init; }
    public string? DamAlternativeName { get; init; }
}
