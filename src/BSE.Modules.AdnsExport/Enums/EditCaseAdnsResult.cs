namespace BSE.Modules.AdnsExport.Enums;

/// <summary>RETURN codes from <c>EditCaseADNS</c> stored procedure.</summary>
public enum EditCaseAdnsResult
{
    /// <summary>SP returned 0 — case updated successfully.</summary>
    Success = 0,
    /// <summary>RowStamp mismatch — case details changed by another user.</summary>
    ConcurrencyConflict = 1,
    /// <summary>Case has already been reported to Brussels.</summary>
    AlreadyReported = 2,
    /// <summary>Case ADNS region has changed since export was generated.</summary>
    RegionChanged = 3,
    /// <summary>Case already has an ADNS reference number assigned.</summary>
    AlreadyAssigned = 4,
    /// <summary>Database UPDATE failed (@@ROWCOUNT != 1).</summary>
    UpdateFailed = 5
}
