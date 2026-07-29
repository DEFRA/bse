using System.Data;
using BSE.Infrastructure;
using BSE.Modules.CaseManagement.Commands;
using BSE.Modules.CaseManagement.Models;
using Dapper;

namespace BSE.Modules.CaseManagement.Repositories;

public interface IBabRepository
{
    Task<CaseBabRecord?> GetByRbseAsync(string rbse);
    Task AddAsync(AddCaseBabCommand command, IDbConnection connection, IDbTransaction transaction);
    Task EditAsync(EditCaseBabCommand command, IDbConnection connection, IDbTransaction transaction);
}

public sealed class BabRepository : DapperRepository, IBabRepository
{
    public BabRepository(IDbConnectionFactory connectionFactory) : base(connectionFactory) { }

    public Task<CaseBabRecord?> GetByRbseAsync(string rbse)
        => QuerySingleOrDefaultAsync<CaseBabRecord>("GetBABByRBSE", new { RBSE = rbse });

    public Task AddAsync(AddCaseBabCommand c, IDbConnection conn, IDbTransaction tx)
        => ExecuteAsync("AddCaseBAB", new
        {
            RBSE = c.Rbse, NatalCPHH = c.NatalCphh, Notes = c.Notes,
            TracedName = c.TracedName, TracedAddress1 = c.TracedAddress1,
            TracedAddress2 = c.TracedAddress2, TracedAddress3 = c.TracedAddress3,
            TracedPostcode = c.TracedPostcode, FeedRisk = c.FeedRisk,
            HorizontalRisk = c.HorizontalRisk, MaternalRisk = c.MaternalRisk
        }, conn, tx);

    public Task EditAsync(EditCaseBabCommand c, IDbConnection conn, IDbTransaction tx)
        => ExecuteAsync("EditCaseBAB", new
        {
            RBSE = c.Rbse, NatalCPHH = c.NatalCphh, Notes = c.Notes,
            TracedName = c.TracedName, TracedAddress1 = c.TracedAddress1,
            TracedAddress2 = c.TracedAddress2, TracedAddress3 = c.TracedAddress3,
            TracedPostcode = c.TracedPostcode, FeedRisk = c.FeedRisk,
            HorizontalRisk = c.HorizontalRisk, MaternalRisk = c.MaternalRisk,
            RowStamp = c.RowStamp
        }, conn, tx);
}
