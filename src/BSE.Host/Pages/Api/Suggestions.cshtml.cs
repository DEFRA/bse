using BSE.Host.Helpers;
using BSE.Modules.Search.Models;
using BSE.Modules.Search.Services;
using BSE.SharedKernel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BSE.Host.Pages.Api;

[Authorize]
public class SuggestionsModel(ICaseSearchService caseSearchService, IFarmSearchService farmSearchService) : PageModel
{
    public async Task<IActionResult> OnGetAsync(string field, string? query = null, int limit = 20)
    {
        limit = Math.Clamp(limit, 1, 50);

        if (string.Equals(field, "rbse", StringComparison.OrdinalIgnoreCase))
        {
            var rbse = RbseHelper.ParseToRaw(query);
            var rows = await caseSearchService.SearchCasesAsync(new CaseSearchQuery(Rbse: rbse));
            var values = rows
                .Select(r => RbseHelper.Format(r.Rbse))
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(limit)
                .ToList();

            return new JsonResult(new { values });
        }

        if (string.Equals(field, "cphh", StringComparison.OrdinalIgnoreCase))
        {
            var cphh = CphhNormalizer.Normalize(query);
            var rows = await farmSearchService.SearchFarmsAsync(new FarmSearchQuery(Cphh: cphh));
            var values = rows
                .Select(r => BseFormat.FormatCphh(r.Cphh))
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(limit)
                .ToList();

            return new JsonResult(new { values });
        }

        return new JsonResult(new { values = Array.Empty<string>() });
    }
}
