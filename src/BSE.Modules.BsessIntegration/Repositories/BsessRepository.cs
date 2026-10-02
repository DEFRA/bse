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
        var bsessBirthDate = p.Get<string?>("@BSESSBirthDate");
        var testGroupName = p.Get<string?>("@TestGroupName");
        var bsessFinalResult = p.Get<string?>("@BSESSFinalResult");
        var barcode = p.Get<string?>("@Barcode");
        var formADate = p.Get<string?>("@FormADate");
        var bseEartag = p.Get<string?>("@BSEEartag");
        var bseBirthDate = p.Get<string?>("@BSEBirthDate");
        var survey = p.Get<string?>("@Survey");
        var bseFinalResult = p.Get<string?>("@BSEFinalResult");

        // A matched BSE case without a TSESS import row is still a valid comparison result.
        // Only return null when every output value is blank.
        if (!BsessCheckByRbseResult.HasAnyValues(
                notificationDate,
                bsessEartag,
                bsessBirthDate,
                testGroupName,
                bsessFinalResult,
                barcode,
                formADate,
                bseEartag,
                bseBirthDate,
                survey,
                bseFinalResult))
        {
            return null;
        }

        return new BsessCheckByRbseResult(
            NotificationDate: notificationDate,
            BsessEartag: bsessEartag,
            BsessBirthDate: bsessBirthDate,
            TestGroupName: testGroupName,
            BsssFinalResult: bsessFinalResult,
            Barcode: barcode,
            FormADate: formADate,
            BseEartag: bseEartag,
            BseBirthDate: bseBirthDate,
            Survey: survey,
            BseFinalResult: bseFinalResult);
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
