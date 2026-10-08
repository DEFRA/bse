using System.Data;
using BSE.Infrastructure;
using BSE.Modules.FarmManagement.Models;

namespace BSE.Modules.FarmManagement.Repositories;

/// <summary>
/// Dapper-backed repository for herd size stored procedure calls.
/// SP names match filenames in src/BSE.Database/StoredProcedures/FarmManagement/ exactly.
/// </summary>
public sealed class HerdSizeRepository : DapperRepository, IHerdSizeRepository
{
    public HerdSizeRepository(IDbConnectionFactory connectionFactory)
        : base(connectionFactory) { }

    public Task<IEnumerable<HerdSizeRecord>> GetByCphhAsync(string cphh)
        => QueryAsync<HerdSizeRecord>("GetHerdSizeByCPHH", new { CPHH = cphh });

    public Task<IEnumerable<HerdDetailRecord>> GetByBatchIdAsync(int batchId)
        => QueryAsync<HerdDetailRecord>("GetHerdDetailByBatchID", new { BatchID = batchId });

    public Task AddAsync(AddHerdSizeCommand c)
        => ExecuteAsync("AddHerdSize", BuildAddParams(c));

    public Task AddAsync(AddHerdSizeCommand c, IDbConnection connection, IDbTransaction transaction)
        => ExecuteAsync("AddHerdSize", BuildAddParams(c), connection, transaction);

    public Task UpdateAsync(UpdateHerdSizeCommand c)
        => ExecuteAsync("EditHerdSize", BuildUpdateParams(c));

    public Task UpdateAsync(UpdateHerdSizeCommand c, IDbConnection connection, IDbTransaction transaction)
        => ExecuteAsync("EditHerdSize", BuildUpdateParams(c), connection, transaction);

    public Task DeleteAsync(int id, byte[] rowStamp)
        => ExecuteAsync("DeleteHerdSize", new { ID = id, RowStamp = rowStamp });

    public Task DeleteAsync(int id, byte[] rowStamp, IDbConnection connection, IDbTransaction transaction)
        => ExecuteAsync("DeleteHerdSize", new { ID = id, RowStamp = rowStamp }, connection, transaction);

    private static object BuildAddParams(AddHerdSizeCommand c) => new
    {
        c.CPHH,
        c.HerdYear,
        c.TotalSize,
        c.Lactation1Size,
        c.Lactation2Size,
        c.Lactation3Size,
        c.Lactation4Size,
        c.Lactation5Size,
        c.Lactation6Size,
        c.Lactation7Size,
        c.Lactation8Size,
        c.Lactation9Size,
        c.Lactation10Size,
        c.Lactation10PlusSize
    };

    private static object BuildUpdateParams(UpdateHerdSizeCommand c) => new
    {
        c.ID,
        c.HerdYear,
        c.TotalSize,
        c.Lactation1Size,
        c.Lactation2Size,
        c.Lactation3Size,
        c.Lactation4Size,
        c.Lactation5Size,
        c.Lactation6Size,
        c.Lactation7Size,
        c.Lactation8Size,
        c.Lactation9Size,
        c.Lactation10Size,
        c.Lactation10PlusSize,
        c.RowStamp
    };
}
