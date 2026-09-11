using BSE.Modules.AnimalRelations.Models;
using BSE.Modules.AnimalRelations.Repositories;
using BSE.Modules.CaseManagement.Commands;
using BSE.Modules.CaseManagement.Repositories;
using BSE.Modules.CaseManagement.Services;
using BSE.Infrastructure;
using BSE.SharedKernel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BSE.Host.Pages.Case;

[Authorize(Policy = "DataEntry")]
public class RelationParentAddModel(
    IAnimalRelationsRepository relationsRepository,
    IPedigreeRepository pedigreeRepository,
    ICaseService caseService,
    IDbConnectionFactory connectionFactory,
    ILogger<RelationParentAddModel> logger) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public string Rbse { get; set; } = string.Empty;

    [BindProperty(SupportsGet = true)]
    public string Sex { get; set; } = string.Empty;

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public string? ErrorMessage { get; private set; }

    public bool IsDam => string.Equals(Sex, "F", StringComparison.OrdinalIgnoreCase);

    public bool HasSelection => Input.Id > 0 || !string.IsNullOrWhiteSpace(Input.Eartag) || !string.IsNullOrWhiteSpace(Input.Name) || !string.IsNullOrWhiteSpace(Input.Herdbook);

    public IActionResult OnGet()
    {
        if (string.IsNullOrWhiteSpace(Rbse) || !IsValidSex(Sex))
        {
            return RedirectToPage("/Home");
        }

        ApplyPendingParent();
        return Page();
    }

    public async Task<IActionResult> OnPostLookUpAsync()
    {
        if (string.IsNullOrWhiteSpace(Rbse) || !IsValidSex(Sex))
        {
            return RedirectToPage("/Home");
        }

        var details = await relationsRepository.GetRelationsDetailsByRbseAsync(RbseHelper.ParseToRaw(Rbse));
        var searchRbse = RbseHelper.Normalize(Input.SearchRbse);

        if (!string.IsNullOrWhiteSpace(searchRbse) && string.Equals(searchRbse, RbseHelper.Normalize(Rbse), StringComparison.OrdinalIgnoreCase))
        {
            ErrorMessage = RelationsModel.SameAsCaseRbse;
            return Page();
        }

        if (!string.IsNullOrWhiteSpace(searchRbse) && details.Relations.Any(r => string.Equals(r.RelationRbse, searchRbse, StringComparison.OrdinalIgnoreCase)))
        {
            ErrorMessage = RelationsModel.AlreadyARelation;
            return Page();
        }

        var matches = await relationsRepository.GetDamSireDetailsMatchesAsync(
            NullIfBlank(Input.SearchEartag),
            NullIfBlank(Input.SearchName),
            string.IsNullOrWhiteSpace(searchRbse) ? null : searchRbse,
            NullIfBlank(Input.SearchHerdbook),
            IsDam ? "F" : "M");

        if (matches.Count == 1)
        {
            ApplyMatch(matches[0]);
            return Page();
        }

        if (matches.Count > 1 || string.IsNullOrWhiteSpace(searchRbse))
        {
            return RedirectToPage("/Case/PickSireDam", new
            {
                rbse = Rbse,
                sex = IsDam ? "F" : "M",
                eartag = Input.SearchEartag,
                name = Input.SearchName,
                herdbook = Input.SearchHerdbook,
                returnTo = "RelationParentAdd"
            });
        }

        ErrorMessage = IsDam ? RelationsModel.DamNotFound : RelationsModel.SireNotFound;
        return Page();
    }

    public async Task<IActionResult> OnPostSaveAsync()
    {
        if (!HasSelection)
        {
            ErrorMessage = "Select or enter details before saving.";
            return Page();
        }

        var details = await relationsRepository.GetRelationsDetailsByRbseAsync(RbseHelper.ParseToRaw(Rbse));
        var caseRecord = await caseService.GetCaseAsync(RbseHelper.ParseToRaw(Rbse));

        var dam = details.Dam;
        var sire = details.Sire;

        var command = IsDam
            ? new AddEditDamSireCommand(
                Rbse: Rbse,
                DamId: Input.Id,
                DamRbse: NullIfBlank(RbseHelper.Normalize(Input.ParentRbse)),
                DamEartag: Input.Eartag,
                DamName: Input.Name,
                DamHerdbook: Input.Herdbook,
                DamBirthDay: Input.BirthDay,
                DamBirthMonth: Input.BirthMonth,
                DamBirthYear: Input.BirthYear,
                DamRowStamp: FromBase64(Input.RowStampBase64),
                SireId: sire?.Id,
                SireRbse: sire?.Rbse,
                SireEartag: sire?.Eartag,
                SireName: sire?.Name,
                SireHerdbook: sire?.Herdbook,
                SireBirthDay: sire?.BirthDay,
                SireBirthMonth: sire?.BirthMonth,
                SireBirthYear: sire?.BirthYear,
                SireRowStamp: sire?.RowStamp,
                CaseHerdbook: caseRecord?.Herdbook,
                CaseRowStamp: caseRecord?.PedigreeRowStamp)
            : new AddEditDamSireCommand(
                Rbse: Rbse,
                DamId: dam?.Id,
                DamRbse: dam?.Rbse,
                DamEartag: dam?.Eartag,
                DamName: dam?.Name,
                DamHerdbook: dam?.Herdbook,
                DamBirthDay: dam?.BirthDay,
                DamBirthMonth: dam?.BirthMonth,
                DamBirthYear: dam?.BirthYear,
                DamRowStamp: dam?.RowStamp,
                SireId: Input.Id,
                SireRbse: NullIfBlank(RbseHelper.Normalize(Input.ParentRbse)),
                SireEartag: Input.Eartag,
                SireName: Input.Name,
                SireHerdbook: Input.Herdbook,
                SireBirthDay: Input.BirthDay,
                SireBirthMonth: Input.BirthMonth,
                SireBirthYear: Input.BirthYear,
                SireRowStamp: FromBase64(Input.RowStampBase64),
                CaseHerdbook: caseRecord?.Herdbook,
                CaseRowStamp: caseRecord?.PedigreeRowStamp);

        try
        {
            using var conn = connectionFactory.CreateConnection();
            conn.Open();
            using var tx = conn.BeginTransaction();
            await pedigreeRepository.AddEditDamSireAsync(command, conn, tx);
            tx.Commit();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "AddEditDamSireDetails failed while adding parent details");
            ErrorMessage = "Unable to save parent details. The record may have changed — reload and try again.";
            return Page();
        }

        TempData["Success"] = IsDam ? "Dam details saved." : "Sire details saved.";
        return RedirectToPage("/Case/Relations", new { rbse = Rbse });
    }

    private void ApplyPendingParent()
    {
        var key = IsDam ? PendingDamSireKeys.Dam : PendingDamSireKeys.Sire;
        if (TempData[key] is not string json)
        {
            return;
        }

        var pending = System.Text.Json.JsonSerializer.Deserialize<PendingDamSire>(json);
        if (pending is null)
        {
            return;
        }

        Input.Id = pending.Id;
        Input.ParentRbse = pending.Rbse;
        Input.Eartag = pending.Eartag;
        Input.Name = pending.Name;
        Input.Herdbook = pending.Herdbook;
        Input.BirthDay = pending.BirthDay;
        Input.BirthMonth = pending.BirthMonth;
        Input.BirthYear = pending.BirthYear;
        Input.RowStampBase64 = pending.RowStampBase64;
        Input.Fate = pending.Fate;
        Input.FinalResult = pending.FinalResult;
        Input.ChildCount = pending.ChildCount;
    }

    private void ApplyMatch(DamSireDetailRecord match)
    {
        Input.Id = match.Id;
        Input.ParentRbse = match.Rbse;
        Input.Eartag = match.Eartag;
        Input.Name = match.Name;
        Input.Herdbook = match.Herdbook;
        Input.BirthDay = match.BirthDay;
        Input.BirthMonth = match.BirthMonth;
        Input.BirthYear = match.BirthYear;
        Input.RowStampBase64 = ToBase64(match.RowStamp);
        Input.Fate = match.Fate;
        Input.FinalResult = match.FinalResult;
        Input.ChildCount = match.ChildCount;
    }

    private static string? NullIfBlank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value;

    private static bool IsValidSex(string value)
        => string.Equals(value, "F", StringComparison.OrdinalIgnoreCase)
        || string.Equals(value, "M", StringComparison.OrdinalIgnoreCase);

    private static string? ToBase64(byte[]? value) => value is { Length: > 0 } ? Convert.ToBase64String(value) : null;

    private static byte[]? FromBase64(string? value) => string.IsNullOrWhiteSpace(value) ? null : Convert.FromBase64String(value);

    public class InputModel
    {
        public string? SearchRbse { get; set; }
        public string? SearchEartag { get; set; }
        public string? SearchName { get; set; }
        public string? SearchHerdbook { get; set; }

        public int Id { get; set; }
        public string? ParentRbse { get; set; }
        public string? Eartag { get; set; }
        public string? Name { get; set; }
        public string? Herdbook { get; set; }
        public int? BirthDay { get; set; }
        public int? BirthMonth { get; set; }
        public int? BirthYear { get; set; }
        public string? RowStampBase64 { get; set; }
        public string? Fate { get; set; }
        public string? FinalResult { get; set; }
        public int? ChildCount { get; set; }
    }
}
