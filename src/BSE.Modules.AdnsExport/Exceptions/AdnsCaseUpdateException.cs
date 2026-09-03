namespace BSE.Modules.AdnsExport.Exceptions;

/// <summary>
/// Thrown when <c>EditCaseADNS</c> returns a non-zero code during ADNS dispatch.
/// Wraps the SP return code and includes the RBSE that caused the failure.
/// </summary>
public sealed class AdnsCaseUpdateException : Exception
{
    public string Rbse { get; }
    public int ReturnCode { get; }

    public AdnsCaseUpdateException(string rbse, int returnCode, string message)
        : base(message)
    {
        Rbse = rbse;
        ReturnCode = returnCode;
    }
}
