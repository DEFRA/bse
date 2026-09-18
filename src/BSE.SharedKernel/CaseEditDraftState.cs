namespace BSE.SharedKernel;

public sealed class CaseEditDraftState
{
    public string Rbse { get; set; } = string.Empty;
    public List<CaseEditDraftTestItem> Tests { get; set; } = [];
    public List<CaseEditDraftOtherOwnerItem> OtherOwners { get; set; } = [];
    public bool HasPendingChanges { get; set; }
}

public sealed class CaseEditDraftTestItem
{
    public string ClientKey { get; set; } = Guid.NewGuid().ToString("N");
    public int? Id { get; set; }
    public string TestType { get; set; } = string.Empty;
    public string? TestTypeDescription { get; set; }
    public string? TestResult { get; set; }
    public string? TestResultDescription { get; set; }
    public string RowStampBase64 { get; set; } = string.Empty;
}

public sealed class CaseEditDraftOtherOwnerItem
{
    public string ClientKey { get; set; } = Guid.NewGuid().ToString("N");
    public int? Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public string? Name { get; set; }
    public string? Cphh { get; set; }
    public string RowStampBase64 { get; set; } = string.Empty;
}
