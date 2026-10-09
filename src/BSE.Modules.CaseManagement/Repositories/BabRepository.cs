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
    /// <summary>Returns null on success, or a legacy-parity warning message if the EditCaseBAB SP
    /// reports the row couldn't be updated (RowStamp mismatch or otherwise) — legacy wraps this whole
    /// update in a blanket try/catch and always treats failures here as soft/non-fatal, so this method
    /// never throws for an SP-level failure.</summary>
    Task<string?> EditAsync(EditCaseBabCommand command, string? origin, IDbConnection connection, IDbTransaction transaction);
}

public sealed class BabRepository : DapperRepository, IBabRepository
{
    public BabRepository(IDbConnectionFactory connectionFactory) : base(connectionFactory) { }

    public Task<CaseBabRecord?> GetByRbseAsync(string rbse)
        => QuerySingleOrDefaultAsync<CaseBabRecord>("GetBABByRBSE", new { RBSE = rbse });

    public async Task AddAsync(AddCaseBabCommand command, string? origin, IDbConnection connection, IDbTransaction transaction)
    {
        await ExecuteAsync("AddCaseBAB", new
        {
            RBSE = command.Rbse, NatalCPHH = command.NatalCphh, Notes = command.Notes,
            TracedName = command.TracedName, TracedAddress1 = command.TracedAddress1,
            TracedAddress2 = command.TracedAddress2, TracedAddress3 = command.TracedAddress3,
            TracedPostcode = command.TracedPostcode, FeedRisk = command.FeedRisk,
            HorizontalRisk = command.HorizontalRisk, MaternalRisk = command.MaternalRisk
        }, connection, transaction);

        await UpdateOriginAsync(command.Rbse, origin, connection, transaction);
    }

    public async Task<string?> EditAsync(EditCaseBabCommand command, string? origin, IDbConnection connection, IDbTransaction transaction)
    {
        string? warning = null;
        try
        {
            var p = new DynamicParameters(new
            {
                RBSE = command.Rbse, NatalCPHH = command.NatalCphh, Notes = command.Notes,
                TracedName = command.TracedName, TracedAddress1 = command.TracedAddress1,
                TracedAddress2 = command.TracedAddress2, TracedAddress3 = command.TracedAddress3,
                TracedPostcode = command.TracedPostcode, FeedRisk = command.FeedRisk,
                HorizontalRisk = command.HorizontalRisk, MaternalRisk = command.MaternalRisk,
                RowStamp = command.RowStamp
            });
            p.Add("RETURN_VALUE", dbType: DbType.Int32, direction: ParameterDirection.ReturnValue);
            await ExecuteWithOutputAsync("EditCaseBAB", p, connection, transaction);
            warning = p.Get<int>("RETURN_VALUE") switch
            {
                0 => null,
                1 => "Failed to update the BAB table.  The data may have been changed by another user.",
                2 => "Failed to update the BAB table.",
                var code => $"Failed to update the BAB table (code {code})."
            };
        }
        catch (Exception ex)
        {
            // Legacy parity: clsCase.UpdateBABRecord wraps the whole update in a try/catch and
            // treats any failure here as soft/non-fatal — never rolls back the rest of the save.
            warning = ex.Message;
        }

        await UpdateOriginAsync(command.Rbse, origin, connection, transaction);
        return warning;
    }

    // Mirrors legacy EmptyPurchaseFields + Origin save in clsCase:
    // when Origin is not 'P', the CK_Case_PurchaseAgeInMonthsNullable and
    // CK_Case_PurchasedCounty constraints require purchase columns to be NULL.
    private static Task<int> UpdateOriginAsync(string rbse, string? origin, IDbConnection connection, IDbTransaction transaction)
    {
        string? normalisedOrigin = string.IsNullOrEmpty(origin) ? null : origin;
        bool isPurchased = normalisedOrigin == "P";

        return connection.ExecuteAsync(
            """
            UPDATE [Case]
            SET    [Origin]               = @Origin,
                   [PurchaseDate]         = CASE WHEN @IsPurchased = 1 THEN [PurchaseDate]         ELSE NULL END,
                   [PurchaseAgeInMonths]  = CASE WHEN @IsPurchased = 1 THEN [PurchaseAgeInMonths]  ELSE NULL END,
                   [PurchasedCounty]      = CASE WHEN @IsPurchased = 1 THEN [PurchasedCounty]      ELSE NULL END
            WHERE  [RBSE] = @Rbse
            """,
            new { Origin = normalisedOrigin, IsPurchased = isPurchased ? 1 : 0, Rbse = rbse },
            transaction);
    }
}
