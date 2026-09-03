using System.Data;
using BSE.Infrastructure;
using BSE.Modules.AdnsExport.Models;
using Dapper;

namespace BSE.Modules.AdnsExport.Repositories;

public sealed class AdnsRepository : DapperRepository, IAdnsRepository
{
    public AdnsRepository(IDbConnectionFactory connectionFactory) : base(connectionFactory) { }

    public Task<(IReadOnlyList<AdnsCaseRecord> Cases, IReadOnlyList<MissingAdnsCaseRecord> Missing)>
        GetCasesForGbAsync(int adnsYear, int startAdnsNumber)
        => QueryMultipleAsync("GetADNSCasesForGB",
            new { ADNSYear = adnsYear, StartADNSNumber = startAdnsNumber },
            async grid =>
            {
                var cases = (await grid.ReadAsync<AdnsCaseRecord>()).ToList();
                var missing = (await grid.ReadAsync<MissingAdnsCaseRecord>()).ToList();
                return ((IReadOnlyList<AdnsCaseRecord>)cases, (IReadOnlyList<MissingAdnsCaseRecord>)missing);
            });

    public Task<LastAdnsReferenceRecord?> GetLastReferenceByAreaAsync(string area)
        => QuerySingleOrDefaultAsync<LastAdnsReferenceRecord>("GetLastADNSReferenceByArea", new { Area = area });

    public async Task<int> EditCaseAdnsAsync(
        string rbse, DateTime sentDate, int adnsRegionId,
        short adnsYear, int adnsNumber, byte[] rowStamp,
        IDbConnection connection, IDbTransaction transaction)
    {
        var param = new DynamicParameters();
        param.Add("RBSE", rbse);
        param.Add("SentDate", sentDate);
        param.Add("ADNSRegionID", adnsRegionId);
        param.Add("ADNSYear", adnsYear);
        param.Add("ADNSNumber", adnsNumber);
        param.Add("RowStamp", rowStamp);
        param.Add("RETURN_VALUE", dbType: DbType.Int32, direction: ParameterDirection.ReturnValue);

        await connection.ExecuteAsync(
            "EditCaseADNS", param, transaction,
            commandType: System.Data.CommandType.StoredProcedure);

        return param.Get<int>("RETURN_VALUE");
    }

    public Task EditLastAdnsReferenceAsync(
        string area, string emailReference, short adnsReferenceYear, int adnsReferenceNumber,
        IDbConnection connection, IDbTransaction transaction)
        => ExecuteAsync("EditLastADNSReference", new
        {
            Area = area,
            EmailReference = emailReference,
            ADNSReferenceYear = adnsReferenceYear,
            ADNSReferenceNumber = adnsReferenceNumber
        }, connection, transaction);
}
