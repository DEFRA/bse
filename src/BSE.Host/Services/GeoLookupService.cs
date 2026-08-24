using BSE.Infrastructure;

namespace BSE.Host.Services;

public sealed class GeoLookupService(IDbRepository db) : IGeoLookupService
{
    public async Task<IReadOnlyList<ParishMapReferenceRow>> GetAllParishMapReferencesAsync(string county, string parish)
    {
        var rows = await db.QueryAsync<ParishMapReferenceRow>(
            "GetMapReferenceByCountyParish",
            new { County = county, Parish = parish });
        return rows.ToList().AsReadOnly();
    }

    public async Task<string?> GetPrefixCodeAsync(string xCoordPrefix, string yCoordPrefix)
    {
        var row = await db.QuerySingleOrDefaultAsync<PrefixCodeRow>(
            "GetPrefixCodeByXYReference",
            new { XCoordPrefix = xCoordPrefix, YCoordPrefix = yCoordPrefix });
        return row?.Code;
    }

    public Task<MapPrefixXY?> GetXYCoordsByPrefixCodeAsync(string code) =>
        db.QuerySingleOrDefaultAsync<MapPrefixXY>("GetXYReferenceByPrefixCode", new { Code = code });

    private record PrefixCodeRow(string Code);
}
