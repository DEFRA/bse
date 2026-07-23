namespace BSE.Modules.BsessIntegration.Services;

public interface IBsessEtlService
{
    /// <summary>
    /// Truncates the local <c>BSESSImport</c> staging table and reloads it
    /// in full from the TSES source database using <c>SqlBulkCopy</c>.
    /// The operation is idempotent and safe to call repeatedly.
    /// </summary>
    Task ImportAsync(CancellationToken cancellationToken = default);
}
