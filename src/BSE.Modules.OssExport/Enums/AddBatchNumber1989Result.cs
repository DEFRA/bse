namespace BSE.Modules.OssExport.Enums;

/// <summary>RETURN codes from <c>AddBatchNumber1989</c>.</summary>
public enum AddBatchNumber1989Result
{
    /// <summary>Batch created successfully.</summary>
    Success = 0,
    /// <summary>INSERT into [Batch] failed (@@ROWCOUNT != 1 or @@ERROR != 0).</summary>
    InsertFailed = 1,
    /// <summary>Read-back of new batch record failed.</summary>
    ReadBackFailed = 2
}
