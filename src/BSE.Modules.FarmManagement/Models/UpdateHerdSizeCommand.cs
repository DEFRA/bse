namespace BSE.Modules.FarmManagement.Models;

/// <summary>
/// Command parameters for the <c>EditHerdSize</c> stored procedure.
/// <c>RowStamp</c> is the current concurrency token.
/// </summary>
public record UpdateHerdSizeCommand(
    int ID,
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
    short Lactation10PlusSize,
    byte[]? RowStamp
);