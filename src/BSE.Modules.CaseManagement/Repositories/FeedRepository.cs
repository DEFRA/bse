using System.Data;
using BSE.Infrastructure;
using BSE.Modules.CaseManagement.Commands;
using BSE.Modules.CaseManagement.Models;

namespace BSE.Modules.CaseManagement.Repositories;

public interface IFeedRepository
{
    Task<IReadOnlyList<CaseFeedRecord>> GetByRbseAsync(string rbse);
    Task AddAsync(AddFeedCommand command, IDbConnection connection, IDbTransaction transaction);
    Task EditAsync(EditFeedCommand command, IDbConnection connection, IDbTransaction transaction);
    Task DeleteAsync(int id, IDbConnection connection, IDbTransaction transaction);
}

public sealed class FeedRepository : DapperRepository, IFeedRepository
{
    public FeedRepository(IDbConnectionFactory connectionFactory) : base(connectionFactory) { }

    public async Task<IReadOnlyList<CaseFeedRecord>> GetByRbseAsync(string rbse)
        => (await QueryAsync<CaseFeedRecord>("GetFeedByRBSE", new { RBSE = rbse })).ToList();

    public Task AddAsync(AddFeedCommand c, IDbConnection conn, IDbTransaction tx)
        => ExecuteAsync("AddCaseFeed", new
        {
            RBSE = c.Rbse, YearFrom = c.YearFrom, YearTo = c.YearTo,
            RationType = c.RationType, SupplierID = c.SupplierId,
            RationName = c.RationName, IsPrePurchase = c.IsPrePurchase
        }, conn, tx);

    public Task EditAsync(EditFeedCommand c, IDbConnection conn, IDbTransaction tx)
        => ExecuteAsync("EditCaseFeed", new
        {
            ID = c.Id, RBSE = c.Rbse, YearFrom = c.YearFrom, YearTo = c.YearTo,
            RationType = c.RationType, SupplierID = c.SupplierId,
            RationName = c.RationName, IsPrePurchase = c.IsPrePurchase,
            RowStamp = c.RowStamp
        }, conn, tx);

    public Task DeleteAsync(int id, IDbConnection conn, IDbTransaction tx)
        => ExecuteAsync("DeleteCaseFeed", new { ID = id }, conn, tx);
}
