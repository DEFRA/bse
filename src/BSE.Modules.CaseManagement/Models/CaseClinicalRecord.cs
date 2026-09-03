namespace BSE.Modules.CaseManagement.Models;

/// <summary>Result record for GetClinicalByRBSE — BSE clinical signs (all bool flags).</summary>
public sealed record CaseClinicalRecord
{
    public string Rbse { get; init; } = string.Empty;
    public bool Apprehension { get; init; }
    public bool HypersensitiveTouch { get; init; }
    public bool HypersensitiveSound { get; init; }
    public bool Maniacal { get; init; }
    public bool PanicStricken { get; init; }
    public bool TemperamentChange { get; init; }
    public bool AbnormalHeadCarriage { get; init; }
    public bool EarTwitching { get; init; }
    public bool EarsOddAngle { get; init; }
    public bool AbnormalBehaviour { get; init; }
    public bool HeadShyness { get; init; }
    public bool LickingFlank { get; init; }
    public bool LickingNose { get; init; }
    public bool Kicking { get; init; }
    public bool ReluctantDoorways { get; init; }
    public bool HeadPressing { get; init; }
    public bool HeadRubbing { get; init; }
    public bool TeethGrinding { get; init; }
    public bool Blindness { get; init; }
    public bool Circling { get; init; }
    public bool HindAtaxia { get; init; }
    public bool Falling { get; init; }
    public bool Paresis { get; init; }
    public bool ForeAtaxia { get; init; }
    public bool Recumbent { get; init; }
    public bool Tremor { get; init; }
    public bool KnucklingFetlock { get; init; }
    public bool WeightLoss { get; init; }
    public bool ConditionLoss { get; init; }
    public bool MilkYield { get; init; }
    public byte[]? RowStamp { get; init; }
}
