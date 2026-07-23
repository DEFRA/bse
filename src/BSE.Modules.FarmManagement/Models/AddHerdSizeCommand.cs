namespace BSE.Modules.FarmManagement.Models;

/// <summary>
/// Command parameters for the <c>AddHerdSize</c> stored procedure.
/// </summary>
public record AddHerdSizeCommand(
    string CPHH,
    short HerdYear,
    short TotalSize,
    short Lactation1Size,
    short Lactation2Size,
    short Lactation3Size,
    short Lactation4Size,
    short Lactation5Size,
    short Lactation6Size,
    short Lactation7Size,
    short Lactation8Size,
    short Lactation9Size,
    short Lactation10Size,
    short Lactation10PlusSize
);
