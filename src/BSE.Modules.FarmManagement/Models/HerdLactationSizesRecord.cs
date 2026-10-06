namespace BSE.Modules.FarmManagement.Models;

/// <summary>Shared SMALLINT-nullable lactation size columns used by <see cref="HerdSizeRecord"/>
/// and <see cref="HerdDetailRecord"/>, both mapped directly from SQL SELECT results.</summary>
public abstract record HerdLactationSizesRecord
{
    public short? Lactation1Size { get; init; }
    public short? Lactation2Size { get; init; }
    public short? Lactation3Size { get; init; }
    public short? Lactation4Size { get; init; }
    public short? Lactation5Size { get; init; }
    public short? Lactation6Size { get; init; }
    public short? Lactation7Size { get; init; }
    public short? Lactation8Size { get; init; }
    public short? Lactation9Size { get; init; }
    public short? Lactation10Size { get; init; }
    public short? Lactation10PlusSize { get; init; }
}
