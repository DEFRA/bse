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

    public async Task<MapReferenceEstimate> EstimateMapReferenceAsync(string cphh)
    {
        if (string.IsNullOrEmpty(cphh) || cphh.Length < 5)
            return new MapReferenceEstimate(null, null, null, "CPHH must be at least 5 characters to estimate a map reference.");

        var county = cphh[..2];
        var parish = cphh[2..5];

        var rows = await GetAllParishMapReferencesAsync(county, parish);
        if (rows.Count == 0)
            return new MapReferenceEstimate(null, null, null, "No map reference data found for the parish associated with this CPHH.");

        // Legacy picks the middle row (round count/2 up when odd) as the "centre" of the parish.
        double rowCount = rows.Count;
        double middleIdx = rowCount % 2 != 0 ? rowCount / 2 + 0.5 : rowCount / 2;
        middleIdx -= 1;
        var row = rows[(int)middleIdx];

        var xCoord = row.XReference1;
        var centreY = CentreCoordinate(row.YReference1, row.YReference2);

        var xPrefixCoord = xCoord[1].ToString();
        var yPrefixRaw = centreY[..2];
        var yPrefixCoord = yPrefixRaw[0] == '0' ? yPrefixRaw[1].ToString() : yPrefixRaw;

        var code = await GetPrefixCodeAsync(xPrefixCoord, yPrefixCoord);
        if (code is null)
            return new MapReferenceEstimate(null, null, null, "Could not determine OS grid square for this parish.");

        return new MapReferenceEstimate(code, xCoord[2..4] + "5", centreY[2..4] + "5", null);
    }

    public async Task<bool> IsMapReferenceWithinParishAsync(string cphh, string mapRef)
    {
        if (cphh.Length < 5 || mapRef.Length < 8) return true;

        var county = cphh[..2];
        var parish = cphh[2..5];

        var prefixData = await GetXYCoordsByPrefixCodeAsync(mapRef[..2].ToUpperInvariant());
        if (prefixData is null) return false;

        // Legacy ValidateMapReference falls through to False when GetMapReferenceByCountyParish
        // returns no rows (e.g. a non-GB placeholder county/parish like "00"/"999" — there's no
        // real OS grid data to check against), not True — a non-GB CPHH's Map Reference is
        // therefore never "valid", matching the warning star legacy always shows once entered.
        var rows = await GetAllParishMapReferencesAsync(county, parish);

        var sXCoord = prefixData.XCoordPrefix + mapRef[2..4];
        var sYCoord = prefixData.YCoordPrefix + mapRef[5..7];

        return rows.Any(r =>
            sXCoord == r.XReference1
            && string.Compare(sYCoord, r.YReference1, StringComparison.Ordinal) >= 0
            && string.Compare(sYCoord, r.YReference2, StringComparison.Ordinal) <= 0);
    }

    private static string CentreCoordinate(string start, string end)
    {
        if (!int.TryParse(start, out var s) || !int.TryParse(end, out var e)) return start;
        var middle = (int)Math.Round((s + e) / 2.0, MidpointRounding.AwayFromZero);
        return middle.ToString("0000");
    }

    private record PrefixCodeRow(string Code);
}
