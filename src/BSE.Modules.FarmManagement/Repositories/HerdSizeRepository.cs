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

    public Task AddAsync(AddHerdSizeCommand command)
        => ExecuteAsync("AddHerdSize", BuildAddParams(command));

    public Task AddAsync(AddHerdSizeCommand command, IDbConnection connection, IDbTransaction transaction)
        => ExecuteAsync("AddHerdSize", BuildAddParams(command), connection, transaction);

    public Task UpdateAsync(UpdateHerdSizeCommand command)
        => ExecuteAsync("EditHerdSize", BuildUpdateParams(command));

    public Task UpdateAsync(UpdateHerdSizeCommand command, IDbConnection connection, IDbTransaction transaction)
        => ExecuteAsync("EditHerdSize", BuildUpdateParams(command), connection, transaction);

    public Task DeleteAsync(int id, byte[] rowStamp)
        => ExecuteAsync("DeleteHerdSize", new { ID = id, RowStamp = rowStamp });

    public Task DeleteAsync(int id, byte[] rowStamp, IDbConnection connection, IDbTransaction transaction)
        => ExecuteAsync("DeleteHerdSize", new { ID = id, RowStamp = rowStamp }, connection, transaction);

    private static object BuildAddParams(AddHerdSizeCommand command) => new
    {
        command.CPHH,
        command.HerdYear,
        command.TotalSize,
        command.Lactation1Size,
        command.Lactation2Size,
        command.Lactation3Size,
        command.Lactation4Size,
        command.Lactation5Size,
        command.Lactation6Size,
        command.Lactation7Size,
        command.Lactation8Size,
        command.Lactation9Size,
        command.Lactation10Size,
        command.Lactation10PlusSize
    };

    private static object BuildUpdateParams(UpdateHerdSizeCommand command) => new
    {
        command.ID,
        command.HerdYear,
        command.TotalSize,
        command.Lactation1Size,
        command.Lactation2Size,
        command.Lactation3Size,
        command.Lactation4Size,
        command.Lactation5Size,
        command.Lactation6Size,
        command.Lactation7Size,
        command.Lactation8Size,
        command.Lactation9Size,
        command.Lactation10Size,
        command.Lactation10PlusSize,
        command.RowStamp
    };
}
