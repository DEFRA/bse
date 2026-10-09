using System.Data;
using BSE.Infrastructure;
using BSE.Modules.CaseManagement.Commands;
using BSE.Modules.CaseManagement.Models;

namespace BSE.Modules.CaseManagement.Repositories;

public interface IFeedRepository
{
    Task<IReadOnlyList<CaseFeedRecord>> GetByRbseAsync(string rbse);
    Task AddAsync(AddFeedCommand command, IDbConnection connection, IDbTransaction transaction);
    /// <summary>Returns the number of rows affected — 0 means the row was changed by another user
    /// (stale RowStamp) since it was read; legacy's <c>OnFeedRowUpdated</c> callback treats this as a
    /// soft, per-row skip rather than aborting the whole save.</summary>
    Task<int> EditAsync(EditFeedCommand command, IDbConnection connection, IDbTransaction transaction);
    /// <summary>Returns the number of rows affected — 0 means the row was changed by another user
    /// (stale RowStamp) since it was read.</summary>
    Task<int> DeleteAsync(int id, byte[] rowStamp, IDbConnection connection, IDbTransaction transaction);
}

public sealed class FeedRepository : DapperRepository, IFeedRepository
{
    public FeedRepository(IDbConnectionFactory connectionFactory) : base(connectionFactory) { }

    public async Task<IReadOnlyList<CaseFeedRecord>> GetByRbseAsync(string rbse)
        => (await QueryAsync<CaseFeedRecord>("GetFeedByRBSE", new { RBSE = rbse })).ToList();

    public Task AddAsync(AddFeedCommand command, IDbConnection connection, IDbTransaction transaction)
        => ExecuteAsync("AddCaseFeed", new
        {
            RBSE = command.Rbse, YearFrom = command.YearFrom, YearTo = command.YearTo,
            RationType = command.RationType, SupplierID = command.SupplierId,
            RationName = command.RationName, IsPrePurchase = command.IsPrePurchase
        }, connection, transaction);

    public Task<int> EditAsync(EditFeedCommand command, IDbConnection connection, IDbTransaction transaction)
        => ExecuteWithRowCountAsync("EditCaseFeed", new
        {
            ID = command.Id, YearFrom = command.YearFrom, YearTo = command.YearTo,
            RationType = command.RationType, SupplierID = command.SupplierId,
            RationName = command.RationName, IsPrePurchase = command.IsPrePurchase,
            RowStamp = command.RowStamp
        }, connection, transaction);

    public Task<int> DeleteAsync(int id, byte[] rowStamp, IDbConnection connection, IDbTransaction transaction)
        => ExecuteWithRowCountAsync("DeleteCaseFeed", new { ID = id, RowStamp = rowStamp }, connection, transaction);
}
