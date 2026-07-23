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
        => ExecuteAsync("AddHerdSize", new
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
        });

    public Task UpdateAsync(UpdateHerdSizeCommand c)
        => ExecuteAsync("EditHerdSize", new
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
        });

    public Task DeleteAsync(int id, byte[] rowStamp)
        => ExecuteAsync("DeleteHerdSize", new { ID = id, RowStamp = rowStamp });
}
