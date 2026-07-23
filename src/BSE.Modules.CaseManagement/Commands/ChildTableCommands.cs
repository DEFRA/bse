namespace BSE.Modules.CaseManagement.Commands;

// ── Clinical ────────────────────────────────────────────────────────────────

public sealed record AddCaseClinicalCommand(
    string Rbse,
    bool Apprehension, bool HypersensitiveTouch, bool HypersensitiveSound,
    bool Maniacal, bool PanicStricken, bool TemperamentChange, bool AbnormalHeadCarriage,
    bool EarTwitching, bool EarsOddAngle, bool AbnormalBehaviour, bool HeadShyness,
    bool LickingFlank, bool LickingNose, bool Kicking, bool ReluctantDoorways,
    bool HeadPressing, bool HeadRubbing, bool TeethGrinding, bool Blindness,
    bool Circling, bool HindAtaxia, bool Falling, bool Paresis, bool ForeAtaxia,
    bool Recumbent, bool Tremor, bool KnucklingFetlock, bool WeightLoss,
    bool ConditionLoss, bool MilkYield);

public sealed record EditCaseClinicalCommand(
    string Rbse,
    bool Apprehension, bool HypersensitiveTouch, bool HypersensitiveSound,
    bool Maniacal, bool PanicStricken, bool TemperamentChange, bool AbnormalHeadCarriage,
    bool EarTwitching, bool EarsOddAngle, bool AbnormalBehaviour, bool HeadShyness,
    bool LickingFlank, bool LickingNose, bool Kicking, bool ReluctantDoorways,
    bool HeadPressing, bool HeadRubbing, bool TeethGrinding, bool Blindness,
    bool Circling, bool HindAtaxia, bool Falling, bool Paresis, bool ForeAtaxia,
    bool Recumbent, bool Tremor, bool KnucklingFetlock, bool WeightLoss,
    bool ConditionLoss, bool MilkYield,
    byte[] RowStamp);

public record AddClinicalVisitCommand(string Rbse, DateTime? VisitDate);
public record EditClinicalVisitCommand(int Id, DateTime? VisitDate, byte[] RowStamp);

// ── BAB ─────────────────────────────────────────────────────────────────────

public sealed record AddCaseBabCommand(
    string Rbse, string? NatalCphh, string? Notes, string? TracedName,
    string? TracedAddress1, string? TracedAddress2, string? TracedAddress3,
    string? TracedPostcode, string? FeedRisk, string? HorizontalRisk, string? MaternalRisk);

public sealed record EditCaseBabCommand(
    string Rbse, string? NatalCphh, string? Notes, string? TracedName,
    string? TracedAddress1, string? TracedAddress2, string? TracedAddress3,
    string? TracedPostcode, string? FeedRisk, string? HorizontalRisk, string? MaternalRisk,
    byte[] RowStamp);

// ── Feed ─────────────────────────────────────────────────────────────────────

public record AddFeedCommand(
    string Rbse, short? YearFrom, short? YearTo, string RationType,
    int? SupplierId, string? RationName, bool IsPrePurchase);

public record EditFeedCommand(
    int Id, string Rbse, short? YearFrom, short? YearTo, string RationType,
    int? SupplierId, string? RationName, bool IsPrePurchase, byte[] RowStamp);

// ── Test ─────────────────────────────────────────────────────────────────────

public record AddTestCommand(string Rbse, string TestType, string? TestResult);
public record EditTestCommand(int Id, string Rbse, string TestType, string? TestResult, byte[] RowStamp);

// ── Other Owner ───────────────────────────────────────────────────────────────

public record AddOtherOwnerCommand(string Rbse, string Type, string? Name, string? Cphh);
public record EditOtherOwnerCommand(int Id, string Rbse, string Type, string? Name, string? Cphh, byte[] RowStamp);

// ── Pedigree/Dam-Sire ─────────────────────────────────────────────────────────

public sealed record AddEditDamSireCommand(
    string Rbse,
    string? DamEartag, string? DamName, string? DamHerdbook,
    int? DamBirthDay, int? DamBirthMonth, int? DamBirthYear,
    string? SireEartag, string? SireName, string? SireHerdbook,
    int? SireBirthDay, int? SireBirthMonth, int? SireBirthYear);

// ── Final Result ──────────────────────────────────────────────────────────────

public record EditFinalResultCommand(
    string Rbse, string? FinalResult, DateTime? FinalResultDate, string? Dbse);
