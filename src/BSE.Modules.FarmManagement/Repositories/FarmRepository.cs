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

    public Task AddAsync(AddFarmCommand c, int userId)
        => ExecuteAsync("AddFarm", new
        {
            c.CPHH,
            c.OwnerName,
            c.Address1,
            c.Address2,
            c.Address3,
            c.Postcode,
            c.Parish,
            c.District,
            c.County,
            c.CorrespondenceAddress1,
            c.CorrespondenceAddress2,
            c.CorrespondenceAddress3,
            c.CorrespondencePostcode,
            c.MapReference,
            c.Herdmark1,
            c.Herdmark2,
            c.Herdmark3,
            c.NumericHerdmark1,
            c.NumericHerdmark2,
            c.AHO,
            c.HerdType,
            c.PedigreeType,
            c.IsDealer,
            c.ADNSRegionID,
            UserID = userId
        });

    public Task UpdateAsync(UpdateFarmCommand c, int userId)
        => ExecuteAsync("EditFarm", new
        {
            c.CPHH,
            c.OwnerName,
            c.Address1,
            c.Address2,
            c.Address3,
            c.Postcode,
            c.Parish,
            c.District,
            c.County,
            c.CorrespondenceAddress1,
            c.CorrespondenceAddress2,
            c.CorrespondenceAddress3,
            c.CorrespondencePostcode,
            c.MapReference,
            c.Herdmark1,
            c.Herdmark2,
            c.Herdmark3,
            c.NumericHerdmark1,
            c.NumericHerdmark2,
            c.AHO,
            c.HerdType,
            c.PedigreeType,
            c.IsDealer,
            c.ADNSRegionID,
            c.RowStamp,
            UserID = userId
        });

    public async Task<ChangeCphhResult> ChangeCphhAsync(string oldCphh, string newCphh, int userId)
    {
        var param = new DynamicParameters();
        param.Add("OldCPHH", oldCphh, DbType.StringFixedLength, size: 11);
        param.Add("NewCPHH", newCphh, DbType.StringFixedLength, size: 11);
        param.Add("UserID", userId);
        param.Add("RETURN_VALUE", dbType: DbType.Int32, direction: ParameterDirection.ReturnValue);
        await ExecuteWithOutputAsync("ChangeCPHH", param);
        return (ChangeCphhResult)param.Get<int>("RETURN_VALUE");
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
