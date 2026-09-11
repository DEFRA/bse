using BSE.SharedKernel;

namespace BSE.Host.Helpers;

/// <summary>
/// Relation validation, mirroring legacy CaseEntryRelations.aspx.vb
/// (ctlRelationRBSE_RBSEChanged and btnAddAsNew_Click).
/// Shared so Add and Edit enforce identical rules.
/// </summary>
public static class RelationValidation
{
    public const string RelationTypeRequired = "You must choose a Relation Type";
    public const string SexRequired = "You must choose a Sex";
    public const string IdentifierRequired = "Either a relation RBSE or an eartag must be provided.";
    public const string SameAsCaseRbse = "This RBSE is the same as the case RBSE";
    public const string SameAsDamRbse = "This RBSE is the same as the dam's RBSE";
    public const string SameAsSireRbse = "This RBSE is the same as the sire's RBSE";
    public const string AlreadyARelation = "This RBSE is already a relation";
    public const string LeftDateFuture = "Must be today or earlier";

    /// <summary>Legacy rejected birth dates before this date.</summary>
    public static readonly DateTime EarliestBirthDate = new(1970, 1, 1);

    public sealed record Input(
        string CaseRbse,
        string? RelationRbse,
        string? RelationType,
        string? Sex,
        string? EartagCountry,
        string? EartagHerdmark,
        string? Eartag,
        int? BirthDay,
        int? BirthMonth,
        int? BirthYear,
        DateTime? LeftDate);

    /// <summary>
    /// Returns field-keyed messages; empty means valid. <paramref name="existingRelationRbses"/>
    /// excludes the row being edited so a record can be saved without changing its RBSE.
    /// </summary>
    public static IDictionary<string, string> Validate(
        Input input,
        IEnumerable<string?> existingRelationRbses,
        string? damRbse,
        string? sireRbse)
    {
        var errors = new Dictionary<string, string>();

        if (string.IsNullOrWhiteSpace(input.RelationType))
        {
            errors["RelationType"] = RelationTypeRequired;
        }

        if (string.IsNullOrWhiteSpace(input.Sex))
        {
            errors["Sex"] = SexRequired;
        }

        var relationRbse = RbseHelper.Normalize(input.RelationRbse);
        var hasEartag =
            !string.IsNullOrWhiteSpace(input.EartagCountry)
            || !string.IsNullOrWhiteSpace(input.EartagHerdmark)
            || !string.IsNullOrWhiteSpace(input.Eartag);

        if (relationRbse.Length == 0 && !hasEartag)
        {
            errors["RelationRbse"] = IdentifierRequired;
        }
        else if (relationRbse.Length > 0)
        {
            if (relationRbse == RbseHelper.Normalize(input.CaseRbse))
            {
                errors["RelationRbse"] = SameAsCaseRbse;
            }
            else if (relationRbse == RbseHelper.Normalize(damRbse))
            {
                errors["RelationRbse"] = SameAsDamRbse;
            }
            else if (relationRbse == RbseHelper.Normalize(sireRbse))
            {
                errors["RelationRbse"] = SameAsSireRbse;
            }
            else if (existingRelationRbses.Any(r => RbseHelper.Normalize(r) == relationRbse))
            {
                errors["RelationRbse"] = AlreadyARelation;
            }
        }

        if (input.BirthDay is { } day && (day < 1 || day > 31))
        {
            errors["BirthDay"] = "Birth day must be between 1 and 31.";
        }

        if (input.BirthMonth is { } month && (month < 1 || month > 12))
        {
            errors["BirthMonth"] = "Birth month must be between 1 and 12.";
        }

        // Legacy bounded the birth date by today, or by the left date when one was supplied.
        var latestBirth = input.LeftDate ?? DateTime.Today;

        if (input.LeftDate is { } leftDate && leftDate.Date > DateTime.Today)
        {
            errors["LeftDate"] = LeftDateFuture;
        }

        if (input.BirthYear is { } year
            && (year < EarliestBirthDate.Year || year > latestBirth.Year))
        {
            errors["BirthYear"] =
                $"Please enter a birth date between {EarliestBirthDate:dd/MM/yyyy} and {latestBirth:dd/MM/yyyy}";
        }

        return errors;
    }
}
