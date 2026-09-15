using BSE.Host.Models.ViewModels;
using BSE.Host.Services;
using BSE.Modules.AnimalRelations.Models;
using BSE.Modules.AnimalRelations.Repositories;
using BSE.Modules.CaseManagement.Commands;
using BSE.Modules.CaseManagement.Enums;
using BSE.Modules.CaseManagement.Repositories;
using BSE.Modules.CaseManagement.Services;
using BSE.Modules.ReferenceData.Models;
using BSE.Modules.ReferenceData.Services;
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
    ILookupDataService lookups,
    ICurrentUserService currentUser,
    IDbConnectionFactory connectionFactory,
    ILogger<RelationParentAddModel> logger) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public string Rbse { get; set; } = string.Empty;

    [BindProperty(SupportsGet = true)]
    public string Sex { get; set; } = string.Empty;

    [BindProperty(SupportsGet = true)]
    public bool StartNew { get; set; }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public string? ErrorMessage { get; private set; }

    public bool IsDam => string.Equals(Sex, "F", StringComparison.OrdinalIgnoreCase);

    public bool HasSelection => Input.Id > 0 || !string.IsNullOrWhiteSpace(Input.Eartag) || !string.IsNullOrWhiteSpace(Input.Name) || !string.IsNullOrWhiteSpace(Input.Herdbook);

    /// <summary>Legacy ddlDamStatus — Case.DamStatus, editable regardless of whether the dam is RBSE-linked.</summary>
    public IReadOnlyList<LookupItem> DamStatusOptions { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync()
    {
        if (string.IsNullOrWhiteSpace(Rbse) || !IsValidSex(Sex))
        {
            return RedirectToPage("/Home");
        }

        ApplyPendingParent();

        if (IsDam)
        {
            DamStatusOptions = (await lookups.GetAnimalStatusesAsync())
                .Select(x => new LookupItem(x.Id, x.Code, x.Description))
                .ToList();
            var caseRecord = await caseService.GetCaseAsync(RbseHelper.ParseToRaw(Rbse));
            Input.DamStatus = caseRecord?.DamStatus;
        }

        if (StartNew)
        {
            Input.ParentRbse = null;
            Input.RowStampBase64 = null;
            Input.BirthDay ??= null;
            Input.BirthMonth ??= null;
            Input.BirthYear ??= null;
        }

        if (!HasSelection && !StartNew)
        {
            var details = await relationsRepository.GetRelationsDetailsByRbseAsync(RbseHelper.ParseToRaw(Rbse));
            var parent = IsDam ? details.Dam : details.Sire;
            if (parent is { Id: > 0 })
            {
                ApplyMatch(parent);
            }
        }

        return Page();
    }

    public async Task<IActionResult> OnPostSaveAsync()
    {
        if (!HasSelection)
        {
            ErrorMessage = "Select or enter details before saving.";
            return Page();
        }

        // Legacy PartialDate rule: a day may only be entered alongside a month (year alone,
        // or month+year, are valid approximate dates; day without month is not).
        if (string.IsNullOrWhiteSpace(Input.ParentRbse) && Input.BirthDay.HasValue && !Input.BirthMonth.HasValue)
        {
            ErrorMessage = "Please enter a month, or remove the day.";
            return Page();
        }

        var caseRbse = RbseHelper.ParseToRaw(Rbse);
        var normalizedParentRbse = NullIfBlank(RbseHelper.Normalize(Input.ParentRbse));
        DamSireDetailRecord? linkedParent = null;

        // Legacy locked Eartag/Herdbook once matched to an existing case RBSE; re-derive them
        // here so a tampered post can't override values that belong to the linked case.
        if (!string.IsNullOrWhiteSpace(normalizedParentRbse))
        {
            var linked = await relationsRepository.GetDamSireDetailsMatchesAsync(
                null, null, normalizedParentRbse, null, IsDam ? "F" : "M");
            linkedParent = linked.FirstOrDefault();
            if (linkedParent is not null)
            {
                Input.Eartag = linkedParent.Eartag;
                Input.Herdbook = linkedParent.Herdbook;
            }
        }

        var details = await relationsRepository.GetRelationsDetailsByRbseAsync(caseRbse);
        var caseRecord = await caseService.GetCaseAsync(caseRbse);

        // Id 0 is a placeholder meaning "no real dam/sire recorded" — never forward it as a
        // real id, or the SP will insert a blank phantom pedigree row for the other side.
        var dam = details.Dam is { Id: > 0 } d ? d : null;
        var sire = details.Sire is { Id: > 0 } s ? s : null;

        // When linking to an RBSE, force INSERT semantics (Id=0) for the target parent.
        // This avoids updating legacy/manual pedigree rows whose shape can violate
        // CK_Pedigree_RBSEPresent when RBSE is set (e.g. non-null Sex on existing row).
        var targetId = normalizedParentRbse is null ? Input.Id : 0;
        var targetRowStamp = normalizedParentRbse is null ? FromBase64(Input.RowStampBase64) : null;

        var command = IsDam
            ? new AddEditDamSireCommand(
                Rbse: caseRbse,
                DamId: targetId,
                DamRbse: normalizedParentRbse,
                DamEartag: Input.Eartag,
                DamName: Input.Name,
                DamHerdbook: Input.Herdbook,
                DamBirthDay: Input.BirthDay,
                DamBirthMonth: Input.BirthMonth,
                DamBirthYear: Input.BirthYear,
                DamRowStamp: targetRowStamp,
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
                Rbse: caseRbse,
                DamId: dam?.Id,
                DamRbse: dam?.Rbse,
                DamEartag: dam?.Eartag,
                DamName: dam?.Name,
                DamHerdbook: dam?.Herdbook,
                DamBirthDay: dam?.BirthDay,
                DamBirthMonth: dam?.BirthMonth,
                DamBirthYear: dam?.BirthYear,
                DamRowStamp: dam?.RowStamp,
                SireId: targetId,
                SireRbse: normalizedParentRbse,
                SireEartag: Input.Eartag,
                SireName: Input.Name,
                SireHerdbook: Input.Herdbook,
                SireBirthDay: Input.BirthDay,
                SireBirthMonth: Input.BirthMonth,
                SireBirthYear: Input.BirthYear,
                SireRowStamp: targetRowStamp,
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

        // Legacy RemoveDam/ddlDamStatus: DamStatus lives on Case, not Pedigree, so it is
        // saved via EditCase rather than AddEditDamSireDetails.
        if (IsDam && caseRecord is not null)
        {
            try
            {
                var editVm = BSE.Host.Models.ViewModels.CaseEditViewModel.FromRecord(caseRecord);
                editVm.DamStatus = Input.DamStatus;
                var editCommand = new EditCaseDetailsCommand(
                    Case: editVm.ToEditCommand(caseRecord.RowStamp ?? []),
                    Clinical: null,
                    Bab: null,
                    DamSire: null);
                var userId = await currentUser.GetUserIdAsync();
                var result = await caseService.EditCaseAsync(editCommand, userId);
                if (result != EditCaseResult.Success)
                {
                    TempData["Warning"] = "Dam details were saved, but the status could not be updated — the case may have changed. Please try again.";
                    return RedirectToPage("/Case/Relations", new { rbse = Rbse });
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "EditCase failed while saving dam status");
                TempData["Warning"] = "Dam details were saved, but the status could not be updated. Please try again.";
                return RedirectToPage("/Case/Relations", new { rbse = Rbse });
            }
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
        public string? DamStatus { get; set; }
    }
}
