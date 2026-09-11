using BSE.Modules.AnimalRelations.Models;
using BSE.Modules.AnimalRelations.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BSE.Host.Pages.Case;

/// <summary>
/// Migrated equivalent of legacy PickSireDam.aspx. Reached from the Relations tab's
/// Look Up action when the search returns no RBSE, or more than one match. Lets the
/// user select an existing pedigree/case match, or create a new unlinked pedigree
/// entry from the eartag/name/herdbook they typed (legacy btnNew_Click).
/// </summary>
[Authorize(Policy = "DataEntry")]
public class PickSireDamModel(IAnimalRelationsRepository relationsRepository) : PageModel
{
    /// <summary>Legacy DataGrid default page size (DataGridPager PageLinkCount=10).</summary>
    public const int PageSize = 10;

    [BindProperty(SupportsGet = true)] public string Rbse { get; set; } = string.Empty;

    /// <summary>"F" for dam, "M" for sire — matches legacy querystring semantics.</summary>
    [BindProperty(SupportsGet = true)] public string Sex { get; set; } = string.Empty;

    [BindProperty(SupportsGet = true)] public string? Eartag { get; set; }
    [BindProperty(SupportsGet = true)] public string? Name { get; set; }
    [BindProperty(SupportsGet = true)] public string? Herdbook { get; set; }
    [BindProperty(SupportsGet = true)] public string? ReturnTo { get; set; }

    [BindProperty(SupportsGet = true)] public string? SortColumn { get; set; }
    [BindProperty(SupportsGet = true)] public bool SortDesc { get; set; }
    [BindProperty(SupportsGet = true)] public int PageNumber { get; set; } = 1;

    public bool IsDam => string.Equals(Sex, "F", StringComparison.OrdinalIgnoreCase);

    public int TotalMatchCount { get; private set; }
    public int TotalPages { get; private set; } = 1;
    public IReadOnlyList<DamSireDetailRecord> PagedMatches { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync()
    {
        if (string.IsNullOrWhiteSpace(Rbse) || string.IsNullOrWhiteSpace(Sex))
        {
            return RedirectToPage("/Home");
        }

        var matches = await relationsRepository.GetDamSireDetailsMatchesAsync(
            NullIfBlank(Eartag), NullIfBlank(Name), null, NullIfBlank(Herdbook), IsDam ? "F" : "M");

        var sorted = Sort(matches);
        TotalMatchCount = sorted.Count;
        TotalPages = Math.Max(1, (int)Math.Ceiling(TotalMatchCount / (double)PageSize));
        PageNumber = Math.Clamp(PageNumber, 1, TotalPages);
        PagedMatches = sorted.Skip((PageNumber - 1) * PageSize).Take(PageSize).ToList();

        return Page();
    }

    /// <summary>Sorts on the columns legacy made sortable (Eartag, Name, Herdbook — not RBSE).</summary>
    private IReadOnlyList<DamSireDetailRecord> Sort(IReadOnlyList<DamSireDetailRecord> matches)
    {
        IEnumerable<DamSireDetailRecord> q = matches;
        q = SortColumn switch
        {
            "Eartag"   => SortDesc ? q.OrderByDescending(m => m.Eartag)   : q.OrderBy(m => m.Eartag),
            "Herdbook" => SortDesc ? q.OrderByDescending(m => m.Herdbook) : q.OrderBy(m => m.Herdbook),
            "Name"     => SortDesc ? q.OrderByDescending(m => m.Name)     : q.OrderBy(m => m.Name),
            _          => q.OrderBy(m => m.Name)
        };
        return q.ToList();
    }

    /// <summary>Legacy btnUseSelected_Click: carries the chosen pedigree row back via TempData.</summary>
    public IActionResult OnPostUseSelected(
        int id, string? rbse, string? eartag, string? name, string? herdbook,
        int? birthDay, int? birthMonth, int? birthYear, string rowStampBase64,
        string? fate, string? finalResult, int? childCount)
    {
        SetPendingParent(new PendingDamSire(
            id, NullIfBlank(rbse), eartag, name, herdbook, birthDay, birthMonth, birthYear, rowStampBase64,
            fate, finalResult, childCount));

        return RedirectToPage(GetReturnPage(), new { rbse = Rbse, sex = Sex });
    }

    /// <summary>Legacy btnNew_Click: creates an unlinked pedigree entry from the typed search values.</summary>
    public IActionResult OnPostNew()
    {
        SetPendingParent(new PendingDamSire(0, null, Eartag, Name, Herdbook, null, null, null, null, null, null, null));
        return RedirectToPage(GetReturnPage(), new { rbse = Rbse, sex = Sex });
    }

    public IActionResult OnPostExit() => RedirectToPage(GetReturnPage(), new { rbse = Rbse, sex = Sex });

    private void SetPendingParent(PendingDamSire pending)
    {
        var key = IsDam ? PendingDamSireKeys.Dam : PendingDamSireKeys.Sire;
        TempData[key] = System.Text.Json.JsonSerializer.Serialize(pending);
    }

    private static string? NullIfBlank(string? s) => string.IsNullOrWhiteSpace(s) ? null : s;

    private string GetReturnPage()
        => string.Equals(ReturnTo, "RelationParentAdd", StringComparison.OrdinalIgnoreCase)
            ? "/Case/RelationParentAdd"
            : "/Case/Relations";
}

/// <summary>TempData keys used to hand a picked/new pedigree row back to the Relations page.</summary>
public static class PendingDamSireKeys
{
    public const string Dam = "PendingDam";
    public const string Sire = "PendingSire";
}

/// <summary>Serialisable carrier for a pedigree row chosen on, or created by, PickSireDam.</summary>
public sealed record PendingDamSire(
    int Id, string? Rbse, string? Eartag, string? Name, string? Herdbook,
    int? BirthDay, int? BirthMonth, int? BirthYear, string? RowStampBase64,
    string? Fate, string? FinalResult, int? ChildCount);
