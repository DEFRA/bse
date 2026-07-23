using System.Data;
using BSE.Infrastructure;
using BSE.Modules.BsessIntegration.Models;
using Dapper;

namespace BSE.Modules.BsessIntegration.Repositories;

public sealed class BsessRepository : DapperRepository, IBsessRepository
{
    public BsessRepository(IDbConnectionFactory connectionFactory) : base(connectionFactory) { }

    public async Task<BsessCheckByRbseResult?> GetCheckByRbseAsync(string rbse, CancellationToken cancellationToken = default)
    {
        var p = new DynamicParameters();
        p.Add("@RBSE", rbse, DbType.StringFixedLength, size: 9);
        p.Add("@NotificationDate", dbType: DbType.String, size: 30, direction: ParameterDirection.Output);
        p.Add("@BSESSEartag", dbType: DbType.String, size: 20, direction: ParameterDirection.Output);
        p.Add("@BSESSBirthDate", dbType: DbType.String, size: 30, direction: ParameterDirection.Output);
        p.Add("@TestGroupName", dbType: DbType.String, size: 50, direction: ParameterDirection.Output);
        p.Add("@BSESSFinalResult", dbType: DbType.String, size: 25, direction: ParameterDirection.Output);
        p.Add("@Barcode", dbType: DbType.String, size: 20, direction: ParameterDirection.Output);
        p.Add("@FormADate", dbType: DbType.String, size: 30, direction: ParameterDirection.Output);
        p.Add("@BSEEartag", dbType: DbType.String, size: 33, direction: ParameterDirection.Output);
        p.Add("@BSEBirthDate", dbType: DbType.String, size: 30, direction: ParameterDirection.Output);
        p.Add("@Survey", dbType: DbType.String, size: 50, direction: ParameterDirection.Output);
        p.Add("@BSEFinalResult", dbType: DbType.String, size: 50, direction: ParameterDirection.Output);

        await ExecuteWithOutputAsync("GetBSESSCheckByRBSE", p);

        var notificationDate = p.Get<string?>("@NotificationDate");
        var bsessEartag = p.Get<string?>("@BSESSEartag");

        // Both outputs null means no matching record was found.
        if (notificationDate is null && bsessEartag is null)
            return null;

        return new BsessCheckByRbseResult(
            NotificationDate: notificationDate,
            BsessEartag: bsessEartag,
            BsessBirthDate: p.Get<string?>("@BSESSBirthDate"),
            TestGroupName: p.Get<string?>("@TestGroupName"),
            BsssFinalResult: p.Get<string?>("@BSESSFinalResult"),
            Barcode: p.Get<string?>("@Barcode"),
            FormADate: p.Get<string?>("@FormADate"),
            BseEartag: p.Get<string?>("@BSEEartag"),
            BseBirthDate: p.Get<string?>("@BSEBirthDate"),
            Survey: p.Get<string?>("@Survey"),
            BseFinalResult: p.Get<string?>("@BSEFinalResult"));
    }

    public async Task<IReadOnlyList<BsessDiscrepancyRecord>> GetCheckByDateAsync(
        DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        var results = await QueryAsync<BsessDiscrepancyRecord>(
            "GetBSESSCheckByDate",
            new { StartDate = startDate, EndDate = endDate });

        return results.ToList().AsReadOnly();
    }
}
