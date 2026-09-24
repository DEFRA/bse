using BSE.Host.Models.ViewModels;
using BSE.Host.Services;
using BSE.Infrastructure;
using BSE.Modules.Batch.Models;
using BSE.Modules.Batch.Repositories;
using BSE.Modules.CaseManagement.Commands;
using BSE.Modules.CaseManagement.Enums;
using BSE.Modules.CaseManagement.Models;
using BSE.Modules.CaseManagement.Repositories;
using BSE.Modules.CaseManagement.Services;
using BSE.Modules.ReferenceData.Services;
using BSE.SharedKernel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;

namespace BSE.Host.Pages.Case;

[Authorize]
public class VlaModel(
    ICaseService caseService,
    ICurrentUserService currentUserService,
    ICaseWizardStateService wizardState,
    ICaseEditDraftStateService caseEditDraftState,
    ILookupDataService lookups,
    IBatchRepository batchRepository,
    IOtherOwnerRepository ownerRepository,
    IDbConnectionFactory connectionFactory,
    IConfiguration configuration) : PageModel
{
    private const string RowStampKey    = "VlaEdit_RowStamp_{0}";
    private const int    OwnersPageSize = 10;

    [BindProperty(SupportsGet = true)]
    public string Rbse { get; set; } = string.Empty;

    [BindProperty(SupportsGet = true)] public int    OPage { get; set; } = 1;
    [BindProperty(SupportsGet = true)] public string OSort { get; set; } = "type";
    [BindProperty(SupportsGet = true)] public string ODir  { get; set; } = "asc";
    public int OtherOwnersTotalPages { get; private set; } = 1;
    public int OtherOwnersTotalCount { get; private set; }

    [BindProperty]
    public VlaEditViewModel Case { get; set; } = new();

    public string? ConcurrencyError { get; private set; }
    public IReadOnlyList<BatchNumberEntry> BatchNumbers { get; private set; } = [];
    public CaseWizardState? PendingBatch { get; private set; }
    public bool CanEditMainCase { get; private set; }

    public IEnumerable<ILookupItem> BirthDateSourceOptions { get; private set; } = [];
    public IEnumerable<ILookupItem> SexOptions            { get; private set; } = [];
    public IEnumerable<ILookupItem> BreedOptions          { get; private set; } = [];
    public IEnumerable<ILookupItem> OriginOptions         { get; private set; } = [];
    public IEnumerable<ILookupItem> PurchasedCountyOptions { get; private set; } = [];

    public IReadOnlyList<OtherOwnerRecord> OtherOwners { get; private set; } = [];
    public IEnumerable<ILookupItem> OwnerTypeOptions { get; private set; } = [];

    [BindProperty] public string? NewOwnerType { get; set; }
    [BindProperty] public string? NewOwnerName { get; set; }
    [BindProperty] public string? NewOwnerCphh { get; set; }
    [BindProperty] public int EditingOwnerId { get; set; }
    [BindProperty] public string? EditOwnerType { get; set; }
    [BindProperty] public string? EditOwnerName { get; set; }
    [BindProperty] public string? EditOwnerCphh { get; set; }
    [BindProperty] public string EditOwnerRowStampBase64 { get; set; } = string.Empty;
    public bool ShowAddOwnerRow { get; private set; }
    public int? ReopenEditOwnerId { get; private set; }
    public bool HasUnsavedOwnerChanges { get; private set; }
    private List<CaseEditDraftOtherOwnerItem> StagedOtherOwners { get; set; } = [];

    public string SpolSiteUrl { get; private set; } = string.Empty;

    public async Task<IActionResult> OnGetAsync()
    {
        var record = await caseService.GetCaseAsync(Rbse);
        if (record is null)
        {
            Case.Rbse = Rbse;
            SpolSiteUrl = configuration["SpolSiteUrl"] ?? string.Empty;
            await LoadLookupsAsync();
            TempData["Warning"] = $"Case '{Rbse}' is not saved yet. Complete Farm first.";
            return Page();
        }

        TempData[string.Format(RowStampKey, Rbse)] = Convert.ToBase64String(record.RowStamp ?? []);
        Case = VlaEditViewModel.FromRecord(record);
        SpolSiteUrl = configuration["SpolSiteUrl"] ?? string.Empty;

        var batchTask = batchRepository.GetBatchNumbersByRbseAsync(Rbse);
        var pendingBatchTask = wizardState.GetAsync();
        await Task.WhenAll(LoadLookupsAsync(), batchTask, pendingBatchTask);
        BatchNumbers = (await batchTask).ToList().AsReadOnly();
        PendingBatch = await pendingBatchTask;
        ApplyLegacyVlaEditPermissions();
        await LoadOrInitializeOwnersDraftAsync();

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!User.IsInRole("DataEntry"))
            return Forbid();

        var caseRbse = RbseHelper.ParseToRaw(Rbse);
        Rbse = caseRbse;

        var batchTask = batchRepository.GetBatchNumbersByRbseAsync(caseRbse);
        var pendingBatchTask = wizardState.GetAsync();
        await Task.WhenAll(batchTask, pendingBatchTask);
        BatchNumbers = (await batchTask).ToList().AsReadOnly();
        PendingBatch = await pendingBatchTask;
        ApplyLegacyVlaEditPermissions();
        if (!CanEditMainCase)
            return RedirectToPage(new { rbse = caseRbse });

        var persistedRecord = await caseService.GetCaseAsync(caseRbse);
        if (persistedRecord is null)
        {
            Case.Rbse = caseRbse;
            SpolSiteUrl = configuration["SpolSiteUrl"] ?? string.Empty;
            await LoadLookupsAsync();
            TempData["Warning"] = $"Case '{caseRbse}' is not saved yet. Complete Farm first.";
            return Page();
        }

        SpolSiteUrl = configuration["SpolSiteUrl"] ?? string.Empty;
        await LoadLookupsAsync();

        if (Case.Origin != "P")
        {
            Case.PurchaseDate = null;
            Case.PurchaseAgeInMonths = null;
            Case.PurchasedCounty = null;
        }

        if (Case.FormBDate.HasValue && !Case.SlaughterDate.HasValue)
            Case.SlaughterDate = Case.FormBDate;

        ValidateVlaDomainRules();

        if (!ModelState.IsValid)
        {
            await LoadOrInitializeOwnersDraftAsync();
            return Page();
        }

        var rowStampBase64 = TempData[string.Format(RowStampKey, Rbse)]?.ToString();
        if (string.IsNullOrEmpty(rowStampBase64))
        {
            ConcurrencyError = "Session expired — please reload the page and try again.";
            return Page();
        }

        var rowStamp = Convert.FromBase64String(rowStampBase64);
        var editCommand = Case.ToEditCommand(rowStamp);
        var command = new EditCaseDetailsCommand(editCommand, Clinical: null, Bab: null, DamSire: null);

        var userId = await currentUserService.GetUserIdAsync();
        var result = await caseService.EditCaseAsync(command, userId);

        if (result == EditCaseResult.ConcurrencyConflict)
        {
            ConcurrencyError = "Another user has modified this case since you loaded it. " +
                               "Please reload to get the latest version and apply your changes again.";
            var current = await caseService.GetCaseAsync(Rbse);
            if (current is not null)
                TempData[string.Format(RowStampKey, caseRbse)] = Convert.ToBase64String(current.RowStamp ?? []);
            await LoadOrInitializeOwnersDraftAsync();
            return Page();
        }

        if (result != EditCaseResult.Success)
        {
            var message = result switch
            {
                EditCaseResult.RbseNotFound    => $"Case '{Rbse}' not found.",
                EditCaseResult.AuditLogError   => "Audit log error during update.",
                EditCaseResult.PostUpdateError => "Database error after update.",
                _                              => $"Update failed: {result}"
            };
            ModelState.AddModelError("", message);
            await LoadOrInitializeOwnersDraftAsync();
            return Page();
        }

        await PersistStagedOwnersAsync(caseRbse);
        await caseEditDraftState.ClearAsync(caseRbse);

        TempData["Success"] = $"Case {caseRbse} has been updated.";
        return RedirectToPage(new { rbse = caseRbse });
    }

    public async Task<IActionResult> OnGetCancelVlaEditAsync()
    {
        await caseEditDraftState.ClearAsync(RbseHelper.ParseToRaw(Rbse));
        return RedirectToPage("/Home");
    }

    public async Task<IActionResult> OnPostBeginEditOwnerRowAsync(int ownerId)
    {
        if (!User.IsInRole("DataEntry"))
            return Forbid();

        var caseRbse = RbseHelper.ParseToRaw(Rbse);
        Rbse = caseRbse;
        SpolSiteUrl = configuration["SpolSiteUrl"] ?? string.Empty;
        await LoadLookupsAsync();
        var batchTask = batchRepository.GetBatchNumbersByRbseAsync(caseRbse);
        var pendingBatchTask = wizardState.GetAsync();
        await Task.WhenAll(batchTask, pendingBatchTask);
        BatchNumbers = (await batchTask).ToList().AsReadOnly();
        PendingBatch = await pendingBatchTask;
        ApplyLegacyVlaEditPermissions();
        if (!CanEditMainCase)
            return RedirectToPage(new { rbse = caseRbse, OSort, ODir, OPage });

        await LoadOrInitializeOwnersDraftAsync();

        var owner = StagedOtherOwners.FirstOrDefault(o => (o.Id ?? 0) == ownerId);
        if (owner is null)
            return RedirectToPage(new { rbse = caseRbse, OSort, ODir, OPage });

        ReopenEditOwnerId = ownerId;
        EditOwnerType = owner.Type;
        EditOwnerName = owner.Name;
        EditOwnerCphh = owner.Cphh;
        EditOwnerRowStampBase64 = owner.RowStampBase64;
        return Page();
    }

    public async Task<IActionResult> OnPostAddOwnerRowAsync()
    {
        if (!User.IsInRole("DataEntry"))
            return Forbid();

        var caseRbse = RbseHelper.ParseToRaw(Rbse);
        Rbse = caseRbse;
        SpolSiteUrl = configuration["SpolSiteUrl"] ?? string.Empty;
        await LoadLookupsAsync();
        var batchTask = batchRepository.GetBatchNumbersByRbseAsync(caseRbse);
        var pendingBatchTask = wizardState.GetAsync();
        await Task.WhenAll(batchTask, pendingBatchTask);
        BatchNumbers = (await batchTask).ToList().AsReadOnly();
        PendingBatch = await pendingBatchTask;
        ApplyLegacyVlaEditPermissions();
        if (!CanEditMainCase)
            return RedirectToPage(new { rbse = caseRbse, OSort, ODir, OPage });

        await LoadOrInitializeOwnersDraftAsync();

        var normalizedCphh = string.IsNullOrWhiteSpace(NewOwnerCphh) ? null : CphhNormalizer.Normalize(NewOwnerCphh);
        ValidateOwnerRow(NewOwnerType, NewOwnerName, normalizedCphh, null);

        if (!ModelState.IsValid)
        {
            ShowAddOwnerRow = true;
            return Page();
        }

        var draft = await GetOrCreateOwnersDraftAsync(caseRbse);
        draft.OtherOwners.Add(new CaseEditDraftOtherOwnerItem
        {
            Id = NextTemporaryOwnerId(draft),
            Type = NewOwnerType!,
            Name = string.IsNullOrWhiteSpace(NewOwnerName) ? null : NewOwnerName,
            Cphh = string.IsNullOrWhiteSpace(normalizedCphh) ? null : normalizedCphh,
            RowStampBase64 = string.Empty
        });
        draft.HasPendingChanges = true;
        await caseEditDraftState.SetAsync(draft);

        return RedirectToPage(new { rbse = caseRbse, OSort, ODir, OPage });
    }

    public async Task<IActionResult> OnPostUpdateOwnerRowAsync()
    {
        if (!User.IsInRole("DataEntry"))
            return Forbid();

        var caseRbse = RbseHelper.ParseToRaw(Rbse);
        Rbse = caseRbse;
        SpolSiteUrl = configuration["SpolSiteUrl"] ?? string.Empty;
        await LoadLookupsAsync();
        var batchTask = batchRepository.GetBatchNumbersByRbseAsync(caseRbse);
        var pendingBatchTask = wizardState.GetAsync();
        await Task.WhenAll(batchTask, pendingBatchTask);
        BatchNumbers = (await batchTask).ToList().AsReadOnly();
        PendingBatch = await pendingBatchTask;
        ApplyLegacyVlaEditPermissions();
        if (!CanEditMainCase)
            return RedirectToPage(new { rbse = caseRbse, OSort, ODir, OPage });

        await LoadOrInitializeOwnersDraftAsync();

        var normalizedCphh = string.IsNullOrWhiteSpace(EditOwnerCphh) ? null : CphhNormalizer.Normalize(EditOwnerCphh);
        ValidateOwnerRow(EditOwnerType, EditOwnerName, normalizedCphh, EditingOwnerId);

        if (!ModelState.IsValid)
        {
            ReopenEditOwnerId = EditingOwnerId;
            return Page();
        }

        var draft = await GetOrCreateOwnersDraftAsync(caseRbse);
        var owner = draft.OtherOwners.FirstOrDefault(o => (o.Id ?? 0) == EditingOwnerId);
        if (owner is null)
        {
            TempData["Warning"] = "Owner record not found.";
            return RedirectToPage(new { rbse = caseRbse, OSort, ODir, OPage });
        }

        owner.Type = EditOwnerType!;
        owner.Name = string.IsNullOrWhiteSpace(EditOwnerName) ? null : EditOwnerName;
        owner.Cphh = string.IsNullOrWhiteSpace(normalizedCphh) ? null : normalizedCphh;
        draft.HasPendingChanges = true;
        await caseEditDraftState.SetAsync(draft);

        return RedirectToPage(new { rbse = caseRbse, OSort, ODir, OPage });
    }

    public async Task<IActionResult> OnPostDeleteOwnerAsync(int ownerId, string rowStampBase64)
    {
        if (!User.IsInRole("DataEntry"))
            return Forbid();

        var caseRbse = RbseHelper.ParseToRaw(Rbse);
        var batchTask = batchRepository.GetBatchNumbersByRbseAsync(caseRbse);
        var pendingBatchTask = wizardState.GetAsync();
        await Task.WhenAll(batchTask, pendingBatchTask);
        BatchNumbers = (await batchTask).ToList().AsReadOnly();
        PendingBatch = await pendingBatchTask;
        ApplyLegacyVlaEditPermissions();
        if (!CanEditMainCase)
            return RedirectToPage(new { rbse = caseRbse, OSort, ODir, OPage });

        var draft = await GetOrCreateOwnersDraftAsync(caseRbse);
        var owner = draft.OtherOwners.FirstOrDefault(o => (o.Id ?? 0) == ownerId);
        if (owner is null)
        {
            TempData["Warning"] = "Owner record not found.";
            return RedirectToPage(new { rbse = caseRbse, OSort, ODir, OPage });
        }

        draft.OtherOwners.Remove(owner);
        draft.HasPendingChanges = true;
        await caseEditDraftState.SetAsync(draft);
        return RedirectToPage(new { rbse = caseRbse, OSort, ODir, OPage });
    }

    private async Task LoadOrInitializeOwnersDraftAsync()
    {
        var caseRbse = RbseHelper.ParseToRaw(Rbse);
        var draft = await GetOrCreateOwnersDraftAsync(caseRbse);
        StagedOtherOwners = draft.OtherOwners.ToList();
        HasUnsavedOwnerChanges = draft.HasPendingChanges;
        var allOwners = StagedOtherOwners.Select(ToOwnerRecord).ToList();
        OtherOwnersTotalCount = allOwners.Count;
        OtherOwnersTotalPages = Math.Max(1, (int)Math.Ceiling(allOwners.Count / (double)OwnersPageSize));
        OPage = Math.Clamp(OPage, 1, OtherOwnersTotalPages);
        IEnumerable<OtherOwnerRecord> sorted = OSort == "cphh"
            ? (ODir == "desc" ? allOwners.OrderByDescending(o => o.Cphh) : allOwners.OrderBy(o => o.Cphh))
            : OSort == "name"
                ? (ODir == "desc" ? allOwners.OrderByDescending(o => o.Name) : allOwners.OrderBy(o => o.Name))
                : (ODir == "desc" ? allOwners.OrderByDescending(o => o.Type) : allOwners.OrderBy(o => o.Type));
        OtherOwners = sorted.Skip((OPage - 1) * OwnersPageSize).Take(OwnersPageSize).ToList().AsReadOnly();
    }

    private void ApplyLegacyVlaEditPermissions()
    {
        // Legacy CaseEntryVLA uses IsVLAAllowedMainCaseEdit(Session):
        // selected batch present OR existing batch numbers linked to case.
        if (!User.IsInRole("DataEntry") || !User.IsInRole("VLAAccess"))
        {
            CanEditMainCase = false;
            return;
        }

        var hasCurrentBatchSelection = PendingBatch is not null
            && string.Equals(RbseHelper.ParseToRaw(PendingBatch.RbseNumber), RbseHelper.ParseToRaw(Rbse), StringComparison.OrdinalIgnoreCase)
            && !string.IsNullOrWhiteSpace(PendingBatch.BatchNumber);

        var hasAnyBatchHistory = BatchNumbers.Count > 0;
        CanEditMainCase = hasCurrentBatchSelection || hasAnyBatchHistory;
    }

    private async Task<CaseEditDraftState> GetOrCreateOwnersDraftAsync(string caseRbse)
    {
        var draft = await caseEditDraftState.GetAsync(caseRbse);
        if (draft is null)
        {
            var owners = (await ownerRepository.GetByRbseAsync(caseRbse)).ToList();
            draft = new CaseEditDraftState
            {
                Rbse = caseRbse,
                OtherOwners = owners.Select(o => new CaseEditDraftOtherOwnerItem
                {
                    Id = o.Id,
                    Type = o.Type ?? string.Empty,
                    Name = o.Name,
                    Cphh = o.Cphh,
                    RowStampBase64 = Convert.ToBase64String(o.RowStamp ?? [])
                }).ToList(),
                HasPendingChanges = false
            };
            await caseEditDraftState.SetAsync(draft);
        }

        draft.OtherOwners ??= [];
        return draft;
    }

    private static OtherOwnerRecord ToOwnerRecord(CaseEditDraftOtherOwnerItem owner)
    {
        var rowStamp = string.IsNullOrWhiteSpace(owner.RowStampBase64)
            ? []
            : Convert.FromBase64String(owner.RowStampBase64);

        return new OtherOwnerRecord(owner.Id ?? 0, string.Empty, owner.Type, owner.Name, owner.Cphh, rowStamp);
    }

    private static int NextTemporaryOwnerId(CaseEditDraftState draft)
    {
        var minId = draft.OtherOwners.Select(o => o.Id ?? 0).DefaultIfEmpty(0).Min();
        return minId <= 0 ? minId - 1 : -1;
    }

    private void ValidateOwnerRow(string? type, string? name, string? normalizedCphh, int? editingOwnerId)
    {
        if (string.IsNullOrWhiteSpace(type))
            ModelState.AddModelError(editingOwnerId.HasValue ? nameof(EditOwnerType) : nameof(NewOwnerType), "Owner type is required.");

        if (string.IsNullOrWhiteSpace(name) && string.IsNullOrWhiteSpace(normalizedCphh))
            ModelState.AddModelError(editingOwnerId.HasValue ? nameof(EditOwnerName) : nameof(NewOwnerName), "You must enter either an owner name or a CPHH.");

        if (!string.IsNullOrWhiteSpace(normalizedCphh) && normalizedCphh.Length != 11)
            ModelState.AddModelError(editingOwnerId.HasValue ? nameof(EditOwnerCphh) : nameof(NewOwnerCphh), "CPHH must be 11 digits.");

        if (!string.IsNullOrWhiteSpace(type))
        {
            var typeDesc = OwnerTypeOptions.FirstOrDefault(t => t.Code == type)?.Description ?? string.Empty;
            if (typeDesc.Contains("Previous", StringComparison.OrdinalIgnoreCase))
            {
                var duplicateExists = StagedOtherOwners
                    .Any(o => string.Equals(o.Type, type, StringComparison.OrdinalIgnoreCase)
                           && (!editingOwnerId.HasValue || (o.Id ?? 0) != editingOwnerId.Value));
                if (duplicateExists)
                    ModelState.AddModelError(editingOwnerId.HasValue ? nameof(EditOwnerType) : nameof(NewOwnerType), "You can only have one owner of type Previous.");
            }
        }
    }

    private async Task PersistStagedOwnersAsync(string caseRbse)
    {
        var draft = await caseEditDraftState.GetAsync(caseRbse);
        if (draft is null || !draft.HasPendingChanges)
            return;

        var staged = draft.OtherOwners ?? [];
        var persisted = (await ownerRepository.GetByRbseAsync(caseRbse)).ToList();
        var persistedById = persisted.ToDictionary(x => x.Id);
        var stagedExistingIds = staged.Where(x => x.Id is > 0).Select(x => x.Id!.Value).ToHashSet();

        using var conn = connectionFactory.CreateConnection();
        conn.Open();
        using var tx = conn.BeginTransaction();

        foreach (var existing in persisted)
        {
            if (!stagedExistingIds.Contains(existing.Id))
            {
                await ownerRepository.DeleteAsync(existing.Id, existing.RowStamp ?? [], conn, tx);
            }
        }

        foreach (var owner in staged)
        {
            var normalizedName = string.IsNullOrWhiteSpace(owner.Name) ? null : owner.Name;
            var normalizedCphh = string.IsNullOrWhiteSpace(owner.Cphh) ? null : owner.Cphh;

            if (owner.Id is > 0 && persistedById.TryGetValue(owner.Id.Value, out var persistedOwner))
            {
                var hasChanges = !string.Equals(owner.Type, persistedOwner.Type, StringComparison.Ordinal)
                                 || !string.Equals(normalizedName, persistedOwner.Name, StringComparison.Ordinal)
                                 || !string.Equals(normalizedCphh, persistedOwner.Cphh, StringComparison.Ordinal);

                if (!hasChanges)
                    continue;

                var rowStamp = string.IsNullOrWhiteSpace(owner.RowStampBase64)
                    ? persistedOwner.RowStamp ?? []
                    : Convert.FromBase64String(owner.RowStampBase64);

                await ownerRepository.EditAsync(new EditOtherOwnerCommand(
                    owner.Id.Value,
                    owner.Type,
                    normalizedName,
                    normalizedCphh,
                    rowStamp), conn, tx);
            }
            else
            {
                await ownerRepository.AddAsync(new AddOtherOwnerCommand(
                    caseRbse,
                    owner.Type,
                    normalizedName,
                    normalizedCphh), conn, tx);
            }
        }

        tx.Commit();
    }

    private void ValidateVlaDomainRules()
    {
        var today = DateTime.Today;
        var formADate = Case.FormADate?.Date;

        if (Case.BirthDate.HasValue)
        {
            if (Case.BirthDate.Value.Date < new DateTime(1970, 1, 1))
                ModelState.AddModelError("Case.BirthDate", "Birth date must be after 31/12/1969.");
            else
            {
                var limit = formADate ?? today;
                if (Case.BirthDate.Value.Date >= limit)
                    ModelState.AddModelError("Case.BirthDate",
                        formADate.HasValue ? "Birth date must be before the Form A date." : "Birth date must be a past date.");
            }
        }

        if (Case.PurchaseDate.HasValue)
        {
            if (Case.BirthDate.HasValue && Case.PurchaseDate.Value.Date <= Case.BirthDate.Value.Date)
                ModelState.AddModelError("Case.PurchaseDate", "Purchase date must be after the birth date and before the Form A date.");
            else
            {
                var limit = formADate ?? today;
                if (Case.PurchaseDate.Value.Date >= limit)
                    ModelState.AddModelError("Case.PurchaseDate",
                        formADate.HasValue ? "Purchase date must be before the Form A date." : "Purchase date must be a past date.");
            }
        }

        if (Case.HerdEntryDate.HasValue)
        {
            var limit = formADate ?? today;
            if (Case.HerdEntryDate.Value.Date > limit)
                ModelState.AddModelError("Case.HerdEntryDate",
                    formADate.HasValue ? "Herd entry date must be before the Form A date." : "Herd entry date must be a past date.");
        }

        if (Case.OnsetDate.HasValue)
        {
            if (Case.BirthDate.HasValue && Case.OnsetDate.Value.Date <= Case.BirthDate.Value.Date)
                ModelState.AddModelError("Case.OnsetDate", "Onset date must be after the date of birth and before the Form A date.");
            else
            {
                var limit = formADate ?? today;
                if (Case.OnsetDate.Value.Date > limit)
                    ModelState.AddModelError("Case.OnsetDate",
                        formADate.HasValue ? "Onset date must be before the Form A date." : "Onset date must be a past date.");
            }
        }

        if (Case.MonthsPregnant.HasValue && Case.MonthsPostCalving.HasValue)
            ModelState.AddModelError("Case.MonthsPostCalving", "You cannot enter values for both months pregnant and months post calving.");

        if (Case.MonthsPregnant.HasValue && (Case.MonthsPregnant.Value < 1 || Case.MonthsPregnant.Value > 9))
            ModelState.AddModelError("Case.MonthsPregnant", "Months pregnant must be between 1 and 9.");
        if (Case.MonthsPostCalving.HasValue && (Case.MonthsPostCalving.Value < 1 || Case.MonthsPostCalving.Value > 3))
            ModelState.AddModelError("Case.MonthsPostCalving", "Months post calving must be between 1 and 3.");

        if (Case.SlaughterDate.HasValue)
        {
            if (Case.SlaughterDate.Value.Date > today)
                ModelState.AddModelError("Case.SlaughterDate", "Slaughter date must not be in the future.");
            if (formADate.HasValue && Case.SlaughterDate.Value.Date < formADate.Value)
                ModelState.AddModelError("Case.SlaughterDate", "Slaughter date must be after the Form A date.");
            else if (!formADate.HasValue && Case.BirthDate.HasValue && Case.SlaughterDate.Value.Date < Case.BirthDate.Value.Date)
                ModelState.AddModelError("Case.SlaughterDate", "Slaughter date must be after the birth date.");
        }
    }

    public string OwnersSortUrl(string col)
    {
        var dir = string.Equals(OSort, col, StringComparison.OrdinalIgnoreCase) && ODir == "asc" ? "desc" : "asc";
        return $"?rbse={Uri.EscapeDataString(Rbse)}&OSort={col}&ODir={dir}&OPage=1";
    }

    public string OwnersPageUrl(int page) =>
        $"?rbse={Uri.EscapeDataString(Rbse)}&OPage={page}&OSort={OSort}&ODir={ODir}";

    private async Task LoadLookupsAsync()
    {
        var t1 = lookups.GetLookupAsync(LookupTableId.BirthDateSource);
        var t2 = lookups.GetLookupAsync(LookupTableId.Sex);
        var t3 = lookups.GetLookupAsync(LookupTableId.Breed);
        var t4 = lookups.GetLookupAsync(LookupTableId.AnimalOrigin);
        var t5 = lookups.GetLookupAsync(LookupTableId.BSECounty);
        var t6 = lookups.GetLookupAsync(LookupTableId.OwnerType);

        await Task.WhenAll(t1, t2, t3, t4, t5, t6);

        BirthDateSourceOptions  = await t1;
        SexOptions              = await t2;
        BreedOptions            = await t3;
        OriginOptions           = await t4;
        PurchasedCountyOptions  = await t5;
        OwnerTypeOptions        = await t6;
    }
}
