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

/// <summary>
/// Maps to the <c>AddEditDamSireDetails</c> stored procedure. DamId/SireId of 0 inserts a
/// new Pedigree row; a non-zero id updates the existing row and requires its RowStamp;
/// null clears the association entirely (legacy sets the session row's ID to DBNull to
/// remove a dam/sire — passing 0 instead would wrongly insert a new blank pedigree row).
/// DamRbse/SireRbse link the parent to an actual case (Pedigree.RBSE) rather than a
/// free-text pedigree entry — required for "Look Up" matches, null for manual entry.
/// CaseHerdbook/CaseRowStamp are the case's own herdbook and Pedigree row concurrency
/// token, both edited on the same legacy screen and required by every call to this SP.
/// </summary>
public sealed record AddEditDamSireCommand(
    string Rbse,
    int? DamId, string? DamRbse, string? DamEartag, string? DamName, string? DamHerdbook,
    int? DamBirthDay, int? DamBirthMonth, int? DamBirthYear, byte[]? DamRowStamp,
    int? SireId, string? SireRbse, string? SireEartag, string? SireName, string? SireHerdbook,
    int? SireBirthDay, int? SireBirthMonth, int? SireBirthYear, byte[]? SireRowStamp,
    string? CaseHerdbook, byte[]? CaseRowStamp);

// ── Final Result ──────────────────────────────────────────────────────────────

public record EditFinalResultCommand(
    string Rbse,
    string? FinalResult,
    DateTime? FinalResultDate,
    string? RetrospectiveTestType,
    string? RetrospectiveResult,
    DateTime? RetrospectiveResultDate,
    string? RetrospectiveComment,
    string? LabComment,
    byte[] RowStamp);
