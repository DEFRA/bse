namespace BSE.Modules.AdnsExport.Models;

public sealed record AdnsCaseUpdate(
    string Rbse,
    DateTime SentDate,
    int AdnsRegionId,
    short AdnsYear,
    int AdnsNumber,
    byte[] RowStamp);
