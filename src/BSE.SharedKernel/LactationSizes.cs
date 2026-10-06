namespace BSE.SharedKernel;

/// <summary>The eleven lactation size values held on a herd size row.</summary>
public interface ILactationSizes
{
    int? Lactation1Size { get; set; }
    int? Lactation2Size { get; set; }
    int? Lactation3Size { get; set; }
    int? Lactation4Size { get; set; }
    int? Lactation5Size { get; set; }
    int? Lactation6Size { get; set; }
    int? Lactation7Size { get; set; }
    int? Lactation8Size { get; set; }
    int? Lactation9Size { get; set; }
    int? Lactation10Size { get; set; }
    int? Lactation10PlusSize { get; set; }
}

/// <summary>Concrete base implementing the eleven <see cref="ILactationSizes"/> properties,
/// shared by every view-model/draft-state type that would otherwise redeclare them.</summary>
public abstract class LactationSizeFields : ILactationSizes
{
    public int? Lactation1Size { get; set; }
    public int? Lactation2Size { get; set; }
    public int? Lactation3Size { get; set; }
    public int? Lactation4Size { get; set; }
    public int? Lactation5Size { get; set; }
    public int? Lactation6Size { get; set; }
    public int? Lactation7Size { get; set; }
    public int? Lactation8Size { get; set; }
    public int? Lactation9Size { get; set; }
    public int? Lactation10Size { get; set; }
    public int? Lactation10PlusSize { get; set; }
}

public static class LactationSizes
{
    public static IEnumerable<int?> All(this ILactationSizes row)
    {
        yield return row.Lactation1Size;
        yield return row.Lactation2Size;
        yield return row.Lactation3Size;
        yield return row.Lactation4Size;
        yield return row.Lactation5Size;
        yield return row.Lactation6Size;
        yield return row.Lactation7Size;
        yield return row.Lactation8Size;
        yield return row.Lactation9Size;
        yield return row.Lactation10Size;
        yield return row.Lactation10PlusSize;
    }

    /// <summary>Sum of the entered values, treating blanks as zero.</summary>
    public static int Total(this ILactationSizes row) => row.All().Sum(v => v ?? 0);

    public static bool HasAnyValue(this ILactationSizes row) => row.All().Any(v => v is not null);

    /// <summary>
    /// Legacy CaseEntryFarm.aspx.vb parity: the eleven lactation columns are stored either
    /// all NULL or all populated. Leaving every box blank keeps them NULL so the grid cells
    /// render empty; entering at least one value fills the remaining blanks with zero.
    /// </summary>
    public static void Normalize(this ILactationSizes row)
    {
        if (!row.HasAnyValue())
            return;

        row.Lactation1Size ??= 0;
        row.Lactation2Size ??= 0;
        row.Lactation3Size ??= 0;
        row.Lactation4Size ??= 0;
        row.Lactation5Size ??= 0;
        row.Lactation6Size ??= 0;
        row.Lactation7Size ??= 0;
        row.Lactation8Size ??= 0;
        row.Lactation9Size ??= 0;
        row.Lactation10Size ??= 0;
        row.Lactation10PlusSize ??= 0;
    }
}
