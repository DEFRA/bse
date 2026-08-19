using System.Data;
using BSE.Infrastructure;
using BSE.Modules.CaseManagement.Commands;
using BSE.Modules.CaseManagement.Models;
using Dapper;

namespace BSE.Modules.CaseManagement.Repositories;

public interface IBabRepository
{
    Task<CaseBabRecord?> GetByRbseAsync(string rbse);
    Task AddAsync(AddCaseBabCommand command, string? origin, IDbConnection connection, IDbTransaction transaction);
    Task EditAsync(EditCaseBabCommand command, string? origin, IDbConnection connection, IDbTransaction transaction);
}

public sealed class BabRepository : DapperRepository, IBabRepository
{
    public BabRepository(IDbConnectionFactory connectionFactory) : base(connectionFactory) { }

    public Task<CaseBabRecord?> GetByRbseAsync(string rbse)
        => QuerySingleOrDefaultAsync<CaseBabRecord>("GetBABByRBSE", new { RBSE = rbse });

    public async Task AddAsync(AddCaseBabCommand c, string? origin, IDbConnection conn, IDbTransaction tx)
    {
        await ExecuteAsync("AddCaseBAB", new
        {
            RBSE = c.Rbse, NatalCPHH = c.NatalCphh, Notes = c.Notes,
            TracedName = c.TracedName, TracedAddress1 = c.TracedAddress1,
            TracedAddress2 = c.TracedAddress2, TracedAddress3 = c.TracedAddress3,
            TracedPostcode = c.TracedPostcode, FeedRisk = c.FeedRisk,
            HorizontalRisk = c.HorizontalRisk, MaternalRisk = c.MaternalRisk
        }, conn, tx);

        await UpdateOriginAsync(c.Rbse, origin, conn, tx);
    }

    public async Task EditAsync(EditCaseBabCommand c, string? origin, IDbConnection conn, IDbTransaction tx)
    {
        await ExecuteAsync("EditCaseBAB", new
        {
            RBSE = c.Rbse, NatalCPHH = c.NatalCphh, Notes = c.Notes,
            TracedName = c.TracedName, TracedAddress1 = c.TracedAddress1,
            TracedAddress2 = c.TracedAddress2, TracedAddress3 = c.TracedAddress3,
            TracedPostcode = c.TracedPostcode, FeedRisk = c.FeedRisk,
            HorizontalRisk = c.HorizontalRisk, MaternalRisk = c.MaternalRisk,
            RowStamp = c.RowStamp
        }, conn, tx);

        await UpdateOriginAsync(c.Rbse, origin, conn, tx);
    }

    // Mirrors legacy EmptyPurchaseFields + Origin save in clsCase:
    // when Origin is not 'P', the CK_Case_PurchaseAgeInMonthsNullable and
    // CK_Case_PurchasedCounty constraints require purchase columns to be NULL.
    private static Task UpdateOriginAsync(string rbse, string? origin, IDbConnection conn, IDbTransaction tx)
    {
        string? normalisedOrigin = string.IsNullOrEmpty(origin) ? null : origin;
        bool isPurchased = normalisedOrigin == "P";

        return conn.ExecuteAsync(
            """
            UPDATE [Case]
            SET    [Origin]               = @Origin,
                   [PurchaseDate]         = CASE WHEN @IsPurchased = 1 THEN [PurchaseDate]         ELSE NULL END,
                   [PurchaseAgeInMonths]  = CASE WHEN @IsPurchased = 1 THEN [PurchaseAgeInMonths]  ELSE NULL END,
                   [PurchasedCounty]      = CASE WHEN @IsPurchased = 1 THEN [PurchasedCounty]      ELSE NULL END
            WHERE  [RBSE] = @Rbse
            """,
            new { Origin = normalisedOrigin, IsPurchased = isPurchased ? 1 : 0, Rbse = rbse },
            tx);
    }
}
