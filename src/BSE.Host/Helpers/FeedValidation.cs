using BSE.Modules.CaseManagement.Models;

namespace BSE.Host.Helpers;

/// <summary>
/// Feed record validation, mirroring legacy CaseEntryFeeds.aspx.vb ControlsHaveValues().
/// Shared so Add and Edit enforce identical rules.
/// </summary>
public static class FeedValidation
{
    public const string YearFromEmpty = "You must enter a Year From value.";
    public const string YearToEmpty = "You must enter a Year To value.";
    public const string RationTypeEmpty = "You must choose a Ration Type";
    public const string SupplierEmpty = "You must validate the supplier before adding it to the table";
    public const string YearFromBeforeBirth = "The date you entered was before the Birth Year.";
    public const string YearToBeforeYearFrom = "The date you entered was before the Year From Value.";

    public sealed record Input(short? YearFrom, short? YearTo, string? RationType, int? SupplierId);

    /// <summary>Returns field-keyed messages; an empty dictionary means the record is valid.</summary>
    public static IDictionary<string, string> Validate(Input input, CaseRecord? caseRecord)
    {
        var errors = new Dictionary<string, string>();

        if (input.YearFrom is null)
        {
            errors["YearFrom"] = YearFromEmpty;
        }
        else if (caseRecord?.BirthDate is { } birthDate && input.YearFrom < birthDate.Year)
        {
            errors["YearFrom"] = YearFromBeforeBirth;
        }

        if (input.YearTo is null)
        {
            errors["YearTo"] = YearToEmpty;
        }
        else if (input.YearFrom is { } from && from > input.YearTo)
        {
            errors["YearTo"] = YearToBeforeYearFrom;
        }

        if (string.IsNullOrWhiteSpace(input.RationType))
        {
            errors["RationType"] = RationTypeEmpty;
        }

        if (input.SupplierId is null)
        {
            errors["SupplierId"] = SupplierEmpty;
        }

        return errors;
    }
}
