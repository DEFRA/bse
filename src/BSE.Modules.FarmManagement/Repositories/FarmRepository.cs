using System.Data;
using BSE.Infrastructure;
using BSE.Modules.FarmManagement.Models;
using BSE.SharedKernel;
using Dapper;

namespace BSE.Modules.FarmManagement.Repositories;

/// <summary>
/// Dapper-backed repository for core farm stored procedure calls.
/// SP names match filenames in src/BSE.Database/StoredProcedures/FarmManagement/ exactly.
/// SP parameter names match @-parameter names defined in each .sql file exactly.
/// <c>GetFarmDetailsByCPHH</c> returns three result sets so is implemented here by
/// calling the three individual SPs rather than the composite SP.
/// </summary>
public sealed class FarmRepository : DapperRepository, IFarmRepository
{
    private const string ReturnValueParam = "RETURN_VALUE";

    public FarmRepository(IDbConnectionFactory connectionFactory)
        : base(connectionFactory) { }

    public Task<FarmRecord?> GetByCphhAsync(string cphh)
        => QuerySingleOrDefaultAsync<FarmRecord>("GetFarmByCPHH", new { CPHH = cphh });

    public async Task<FarmDetailRecord> GetDetailsByCphhAsync(string cphh)
    {
        var param = new { CPHH = cphh };
        var farm = await QuerySingleOrDefaultAsync<FarmRecord>("GetFarmByCPHH", param);
        var relations = await QueryAsync<FarmRelationRecord>("GetRelatedFarm", param);
        var herdSizes = await QueryAsync<HerdSizeRecord>("GetHerdSizeByCPHH", param);
        return new FarmDetailRecord(farm, relations, herdSizes);
    }

    public Task<IEnumerable<FarmSummaryRecord>> GetByCphAsync(string cph)
        => QueryAsync<FarmSummaryRecord>("GetFarmsByCPH", new { CPH = cph });

    public Task AddAsync(AddFarmCommand command, int userId)
        => ExecuteAsync("AddFarm", BuildAddFarmParams(command, userId));

    /// <summary>Transactional variant — enlisted in a caller-supplied connection/transaction so a
    /// brand-new farm can be committed atomically alongside the new case row and other first-time
    /// case-creation work (mirrors legacy's single <c>UpdateCaseDetails</c> transaction, which
    /// inserts the Farm row before the Case row that references it).</summary>
    public Task AddAsync(AddFarmCommand command, int userId, IDbConnection connection, IDbTransaction transaction)
        => ExecuteAsync("AddFarm", BuildAddFarmParams(command, userId), connection, transaction);

    private static object BuildAddFarmParams(AddFarmCommand command, int userId) => new
    {
        command.CPHH,
        command.OwnerName,
        command.Address1,
        command.Address2,
        command.Address3,
        command.Postcode,
        command.Parish,
        command.District,
        command.County,
        command.CorrespondenceAddress1,
        command.CorrespondenceAddress2,
        command.CorrespondenceAddress3,
        command.CorrespondencePostcode,
        command.MapReference,
        command.Herdmark1,
        command.Herdmark2,
        command.Herdmark3,
        command.NumericHerdmark1,
        command.NumericHerdmark2,
        command.AHO,
        command.HerdType,
        command.PedigreeType,
        command.IsDealer,
        command.ADNSRegionID,
        UserID = userId
    };

    public Task UpdateAsync(UpdateFarmCommand command, int userId)
        => ExecuteAsync("EditFarm", BuildEditFarmParams(command, userId));

    /// <summary>Mirrors legacy clsFarm.UpdateFarmDetails's EditFarm return-code handling exactly:
    /// code 3 ("modified by another user") is soft and returned as a warning; codes 1/2/4 are hard
    /// failures that throw, matching legacy's FarmUpdateException, which rolls back the whole
    /// transaction.</summary>
    public async Task<string?> UpdateAsync(UpdateFarmCommand command, int userId, IDbConnection connection, IDbTransaction transaction)
    {
        var p = new DynamicParameters(BuildEditFarmParams(command, userId));
        p.Add(ReturnValueParam, dbType: DbType.Int32, direction: ParameterDirection.ReturnValue);
        await ExecuteWithOutputAsync("EditFarm", p, connection, transaction);
        return p.Get<int>(ReturnValueParam) switch
        {
            0 => null,
            1 => throw new InvalidOperationException($"The farm with CPHH {command.CPHH} has been deleted by another user"),
            2 => throw new InvalidOperationException("Failed to insert into the audit log"),
            3 => $"The farm record with CPHH {command.CPHH} has been modified by another user",
            4 => throw new InvalidOperationException("Failed to update the Farm table"),
            var code => throw new InvalidOperationException($"EditFarm returned unexpected code {code}")
        };
    }

    private static object BuildEditFarmParams(UpdateFarmCommand command, int userId) => new
    {
        command.CPHH,
        command.OwnerName,
        command.Address1,
        command.Address2,
        command.Address3,
        command.Postcode,
        command.Parish,
        command.District,
        command.County,
        command.CorrespondenceAddress1,
        command.CorrespondenceAddress2,
        command.CorrespondenceAddress3,
        command.CorrespondencePostcode,
        command.MapReference,
        command.Herdmark1,
        command.Herdmark2,
        command.Herdmark3,
        command.NumericHerdmark1,
        command.NumericHerdmark2,
        command.AHO,
        command.HerdType,
        command.PedigreeType,
        command.IsDealer,
        command.ADNSRegionID,
        command.RowStamp,
        UserID = userId
    };

    public async Task<ChangeCphhResult> ChangeCphhAsync(string oldCphh, string newCphh, int userId)
    {
        var param = new DynamicParameters();
        param.Add("OldCPHH", oldCphh, DbType.StringFixedLength, size: 11);
        param.Add("NewCPHH", newCphh, DbType.StringFixedLength, size: 11);
        param.Add("UserID", userId);
        param.Add(ReturnValueParam, dbType: DbType.Int32, direction: ParameterDirection.ReturnValue);
        await ExecuteWithOutputAsync("ChangeCPHH", param);
        return (ChangeCphhResult)param.Get<int>(ReturnValueParam);
    }

    public async Task<int> GetConfirmedCaseCountAsync(string cphh)
    {
        var results = await QueryAsync<int>("GetNumberOfConfirmedCases", new { CPHH = cphh });
        return results.FirstOrDefault();
    }

    public async Task<int> GetCaseCountByCphhAsync(string cphh)
    {
        var results = await QueryAsync<int>("GetNumberOfCasesByCPHH", new { CPHH = cphh });
        return results.FirstOrDefault();
    }
}
