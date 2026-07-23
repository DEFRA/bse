using System.Data;
using BSE.Modules.AdnsExport.Models;

namespace BSE.Modules.AdnsExport.Repositories;

public interface IAdnsRepository
{
    /// <summary>
    /// Returns two result sets: valid ADNS cases (result 1) and missing cases without
    /// an ADNS region (result 2). Calls <c>GetADNSCasesForGB</c>.
    /// </summary>
    Task<(IReadOnlyList<AdnsCaseRecord> Cases, IReadOnlyList<MissingAdnsCaseRecord> Missing)>
        GetCasesForGbAsync(int adnsYear, int startAdnsNumber);

    /// <summary>Returns the last ADNS reference used for an area (GB/NI/CI). Calls <c>GetLastADNSReferenceByArea</c>.</summary>
    Task<LastAdnsReferenceRecord?> GetLastReferenceByAreaAsync(string area);

    /// <summary>
    /// Updates a case with its ADNS reference and sent date. Must be called inside an
    /// open transaction. Returns the SP RETURN code (0 = success, 1-5 = specific errors).
    /// Calls <c>EditCaseADNS</c>.
    /// </summary>
    Task<int> EditCaseAdnsAsync(
        string rbse, DateTime sentDate, int adnsRegionId,
        short adnsYear, int adnsNumber, byte[] rowStamp,
        IDbConnection connection, IDbTransaction transaction);

    /// <summary>
    /// Upserts the last email reference and ADNS reference number for an area.
    /// Must be called inside an open transaction. Calls <c>EditLastADNSReference</c>.
    /// </summary>
    Task EditLastAdnsReferenceAsync(
        string area, string emailReference, short adnsReferenceYear, int adnsReferenceNumber,
        IDbConnection connection, IDbTransaction transaction);
}
