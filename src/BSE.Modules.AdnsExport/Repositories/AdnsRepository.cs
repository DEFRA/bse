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
        AdnsCaseUpdate update,
        IDbConnection connection,
        IDbTransaction transaction)
    {
        var param = new DynamicParameters();
        param.Add("RBSE", update.Rbse);
        param.Add("SentDate", update.SentDate);
        param.Add("ADNSRegionID", update.AdnsRegionId);
        param.Add("ADNSYear", update.AdnsYear);
        param.Add("ADNSNumber", update.AdnsNumber);
        param.Add("RowStamp", update.RowStamp);
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
