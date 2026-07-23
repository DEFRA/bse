namespace BSE.Modules.OssExport.Models;

/// <summary>
/// Output from <c>AddBatchNumber1989</c> stored procedure.
/// The SP uses OUTPUT parameters; Dapper's DynamicParameters captures them.
/// Return codes: 0 = success, 1 = insert failed, 2 = read-back failed.
/// </summary>
public sealed record BatchNumber1989Result(
    int BatchId,
    short BatchYear,
    int BatchNumber);
