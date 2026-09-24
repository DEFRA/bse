using BSE.Host.Helpers;
using BSE.Host.Services;
using BSE.Infrastructure;
using BSE.Modules.AnimalRelations.Commands;
using BSE.Modules.AnimalRelations.Models;
using BSE.Modules.AnimalRelations.Repositories;
using BSE.Modules.Batch.Models;
using BSE.Modules.Batch.Repositories;
using BSE.Modules.CaseManagement.Commands;
using BSE.Modules.CaseManagement.Enums;
using BSE.Modules.CaseManagement.Repositories;
using BSE.Modules.CaseManagement.Services;
using BSE.Modules.ReferenceData.Models;
using BSE.Modules.ReferenceData.Services;
using BSE.SharedKernel;
using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using System.Text.Json;

namespace BSE.Host.Pages.Case;

[Authorize]
public class RelationsModel(
    IAnimalRelationsRepository relationsRepository,
    IPedigreeRepository pedigreeRepository,
    ICaseService caseService,
    IFeedRepository feedRepository,
    ILookupDataService lookups,
    IBatchRepository batchRepository,
    ICaseRelationsDraftStateService relationsDraftState,
    IDbConnectionFactory connectionFactory,
    ICurrentUserService currentUser,
    ILogger<RelationsModel> logger,
    IConfiguration configuration) : PageModel
{
    public const string SameAsCaseRbse = "This RBSE is the same as the case RBSE";
    public const string AlreadyARelation = "This RBSE is already a twin, sister or offspring";
    public const string DamNotFound = "This RBSE does not exist or is not a female animal";
    public const string SireNotFound = "This RBSE does not exist or is not a male animal";

    [BindProperty(SupportsGet = true)]
    public string Rbse { get; set; } = string.Empty;

    [BindProperty(SupportsGet = true)]
    public string? SortColumn { get; set; }

    [BindProperty(SupportsGet = true)]
    public bool SortDesc { get; set; }

    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;

    [BindProperty(SupportsGet = true)]
    public bool EditCaseHerdbook { get; set; }

    /// <summary>Legacy DataGrid default page size.</summary>
    public const int PageSize = 10;

    public RelationDetailsRecord? Details { get; private set; }
    public string SpolSiteUrl { get; private set; } = string.Empty;
    public IReadOnlyList<BatchNumberEntry> BatchNumbers { get; private set; } = [];

    public IEnumerable<LookupItem> RelationTypes { get; private set; } = [];
    public IEnumerable<LuRelationFate> RelationFates { get; private set; } = [];
    public IEnumerable<LuSex> Sexes { get; private set; } = [];

    public int TotalRelationsCount { get; private set; }
    public int TotalPages { get; private set; } = 1;
    public IReadOnlyList<StagedRelationItem> PagedRelations { get; private set; } = [];
    private List<StagedRelationItem> _stagedRelations = [];
    private CaseRelationsDraftState? _draft;

    // ── Shared add/edit field panel state (mirrors legacy's single panel used for both
    // "Add As New" and "Update Selected"; the related-animals list is staged and only
    // committed to the database when Save is selected — matches /Case/Farm) ──
    [BindProperty] public string? EditingClientKey { get; set; }
    [BindProperty] public string? RelationType { get; set; }
    [BindProperty] public string? RelationRbse { get; set; }
    [BindProperty] public string? EartagCountry { get; set; }
    [BindProperty] public string? EartagHerdmark { get; set; }
    [BindProperty] public string? Eartag { get; set; }
    [BindProperty] public string? Sex { get; set; }

    /// <summary>Posted single calendar date (legacy ctlRelationBirthDate is a CalendarDate
    /// control, not a day/month/year PartialDate). Decomposed into BirthDay/Month/Year, the
    /// storage shape CaseRelation actually persists, before validation.</summary>
    [BindProperty] public DateTime? BirthDate { get; set; }
    public int? BirthDay { get; set; }
    public int? BirthMonth { get; set; }
    public int? BirthYear { get; set; }
    [BindProperty] public DateTime? LeftDate { get; set; }
    [BindProperty] public string? RelationFate { get; set; }
    [BindProperty] public string? Sire { get; set; }

    public IDictionary<string, string> RelationFieldErrors { get; private set; } = new Dictionary<string, string>();
    public bool ShowAddRelationRow { get; private set; }
    public string? ReopenEditClientKey { get; private set; }

    /// <summary>True when the draft has staged relation changes not yet committed by Save.</summary>
    public bool HasUnsavedChanges { get; private set; }

    [BindProperty]
    public DamSireViewModel DamSire { get; set; } = new();

    /// <summary>Legacy txtCaseHerdbook — the case's own herdbook, edited on this tab.</summary>
    [BindProperty]
    public string? CaseHerdbook { get; set; }

    /// <summary>Legacy ddlDamStatus — Case.DamStatus resolved to its luAnimalStatus description.</summary>
    public string? DamStatusDescription { get; private set; }

    /// <summary>Legacy ddlDamStatus values for the same-page dam editor.</summary>
    public IReadOnlyList<LookupItem> DamStatusOptions { get; private set; } = [];

    public string? DamError { get; private set; }
    public string? SireError { get; private set; }

    public async Task<IActionResult> OnGetAsync()
    {
        return await LoadRelationsPageAsync(editCaseHerdbook: false);
    }

    public async Task<IActionResult> OnGetEditCaseHerdbookAsync()
    {
        return await LoadRelationsPageAsync(editCaseHerdbook: true);
    }

    /// <summary>Legacy btnDamLookUp_Click.</summary>
    public async Task<IActionResult> OnPostLookUpDamAsync()
    {
        await LoadAsync();
        await LoadOrInitializeRelationsDraftAsync();
        await LookUpAsync(isDam: true);
        return await FinishLookUpAsync(isDam: true);
    }

    /// <summary>Legacy btnSireLookUp_Click.</summary>
    public async Task<IActionResult> OnPostLookUpSireAsync()
    {
        await LoadAsync();
        await LoadOrInitializeRelationsDraftAsync();
        await LookUpAsync(isDam: false);
        return await FinishLookUpAsync(isDam: false);
    }

    /// <summary>Legacy btnSave_Click (Dam/Sire section): persists whatever is currently populated.</summary>
    public async Task<IActionResult> OnPostSaveDamSireAsync()
    {
        if (!User.IsInRole("DataEntry"))
            return Forbid();
        await LoadAsync();
        var draft = await LoadOrInitializeRelationsDraftAsync();
        draft.HasPendingChanges = true;
        await relationsDraftState.SetAsync(draft);
        TempData.Remove("Success");
        return RedirectToPage(new { rbse = Rbse });
    }

    public async Task<IActionResult> OnPostSaveCaseHerdbookAsync()
    {
        if (!User.IsInRole("DataEntry"))
            return Forbid();

        await LoadAsync();
        var draft = await LoadOrInitializeRelationsDraftAsync();
        draft.HasPendingChanges = true;
        await relationsDraftState.SetAsync(draft);
        TempData.Remove("Success");
        return RedirectToPage(new { rbse = Rbse });
    }

    public IActionResult OnPostDeleteRelationAsync(int relationId, string rowStampBase64)
    {
        // Superseded by OnPostDeleteRelationRowAsync (staged delete, committed on Save).
        // Kept only so any stale bookmarked form post still redirects safely.
        return RedirectToPage(new { rbse = Rbse });
    }

    /// <summary>Legacy RemoveDam: disassociates the dam without touching the sire.</summary>
    public async Task<IActionResult> OnPostRemoveDamAsync()
    {
        if (!User.IsInRole("DataEntry"))
            return Forbid();
        await LoadAsync();
        var draft = await LoadOrInitializeRelationsDraftAsync();

        draft.RemoveDamPending = true;
        draft.PendingDam = null;
        draft.HasPendingChanges = true;
        await relationsDraftState.SetAsync(draft);

        TempData.Remove("Success");
        TempData.Remove(PendingDamSireKeys.Dam);
        return RedirectToPage(new { rbse = Rbse });
    }

    /// <summary>Legacy RemoveSire: disassociates the sire without touching the dam.</summary>
    public async Task<IActionResult> OnPostRemoveSireAsync()
    {
        if (!User.IsInRole("DataEntry"))
            return Forbid();
        await LoadAsync();
        var draft = await LoadOrInitializeRelationsDraftAsync();

        draft.RemoveSirePending = true;
        draft.PendingSire = null;
        draft.HasPendingChanges = true;
        await relationsDraftState.SetAsync(draft);

        TempData.Remove("Success");
        TempData.Remove(PendingDamSireKeys.Sire);
        return RedirectToPage(new { rbse = Rbse });
    }

    private async Task LookUpAsync(bool isDam)
    {
        var caseRbse = RbseHelper.ParseToRaw(Rbse);
        var searchRbse = RbseHelper.Normalize(isDam ? DamSire.DamSearchRbse : DamSire.SireSearchRbse);
        var searchEartag = isDam ? DamSire.DamSearchEartag : DamSire.SireSearchEartag;
        var searchName = isDam ? DamSire.DamSearchName : DamSire.SireSearchName;
        var searchHerdbook = isDam ? DamSire.DamSearchHerdbook : DamSire.SireSearchHerdbook;

        if (searchRbse.Length > 0)
        {
            if (searchRbse == caseRbse)
            {
                SetError(isDam, SameAsCaseRbse);
                return;
            }

            var otherParentRbse = isDam ? Details?.Sire?.Rbse : Details?.Dam?.Rbse;
            // Checked against the staged relations list (not just the DB-persisted one) so a
            // relation added but not yet saved is still caught — matches legacy's RBSEIsRelation,
            // which checked the session-held working table rather than the database.
            var alreadyRelation = searchRbse == RbseHelper.Normalize(otherParentRbse)
                || _stagedRelations.Any(r => RbseHelper.Normalize(r.RelationRbse) == searchRbse);
            if (alreadyRelation)
            {
                SetError(isDam, AlreadyARelation);
                return;
            }
        }

        // Legacy navigation behavior: if RBSE is not supplied, open PickSireDam.aspx
        // (even when exactly one candidate exists) rather than auto-selecting.
        if (searchRbse.Length == 0)
        {
            if (isDam) DamShouldOpenPicker = true; else SireShouldOpenPicker = true;
            return;
        }

        var matches = await relationsRepository.GetDamSireDetailsMatchesAsync(
            NullIfBlank(searchEartag), NullIfBlank(searchName),
            searchRbse,
            NullIfBlank(searchHerdbook), isDam ? "F" : "M");

        if (matches.Count == 1)
        {
            await ApplyMatchAsync(isDam, matches[0]);
        }
        else if (matches.Count > 1)
        {
            // Legacy redirected to PickSireDam.aspx here; ShouldOpenPicker is read by the
            // handler to decide whether to redirect there instead of re-rendering this page.
            if (isDam) DamShouldOpenPicker = true; else SireShouldOpenPicker = true;
        }
        else
        {
            SetError(isDam, isDam ? DamNotFound : SireNotFound);
        }
    }

    private bool DamShouldOpenPicker { get; set; }
    private bool SireShouldOpenPicker { get; set; }
    private bool RemoveDamPending { get; set; }
    private bool RemoveSirePending { get; set; }

    private async Task<IActionResult> FinishLookUpAsync(bool isDam)
    {
        var shouldOpenPicker = isDam ? DamShouldOpenPicker : SireShouldOpenPicker;
        if (shouldOpenPicker)
        {
            var eartag = isDam ? DamSire.DamSearchEartag : DamSire.SireSearchEartag;
            var name = isDam ? DamSire.DamSearchName : DamSire.SireSearchName;
            var herdbook = isDam ? DamSire.DamSearchHerdbook : DamSire.SireSearchHerdbook;
            var searchRbse = RbseHelper.Normalize(isDam ? DamSire.DamSearchRbse : DamSire.SireSearchRbse);

            TempData[PickSireDamContextKeys.Rbse] = Rbse;
            TempData[PickSireDamContextKeys.ReturnTo] = "Relations";
            TempData[PickSireDamContextKeys.Eartag] = eartag;
            TempData[PickSireDamContextKeys.Name] = name;
            TempData[PickSireDamContextKeys.Herdbook] = herdbook;

            if (searchRbse.Length == 0)
            {
                // Legacy URL shape for blank RBSE lookup:
                // PickSireDam.aspx?eartag=&name=&herdbook=&sex=M
                // (case RBSE context is carried outside query string)
                return RedirectToPage("/Case/PickSireDam", new
                {
                    sex = isDam ? "F" : "M",
                    eartag = string.Empty,
                    name = string.Empty,
                    herdbook = string.Empty
                });
            }

            return RedirectToPage("/Case/PickSireDam", new
            {
                rbse = Rbse,
                sex = isDam ? "F" : "M",
                eartag,
                name,
                herdbook,
                returnTo = "Relations"
            });
        }

        // Re-rendering this page directly (no redirect) — hydrate the rest of the page state
        // (case herdbook, dam status options, SPOL link) that LoadAsync/LoadOrInitializeRelationsDraftAsync
        // don't cover, so the page doesn't render with those sections blank after a Look Up.
        await PopulateCaseAncillaryStateAsync();

        // The Look Up form only posts search criteria for the side being looked up, so the
        // *other* parent's fields are still at their model-binding defaults here. Without this,
        // an already-linked dam/sire silently disappears from this render's hidden fields and
        // gets wiped from the case's pedigree link on the next Save.
        PopulateOppositeParentFromDetails(isDam);

        return Page();
    }

    private void PopulateOppositeParentFromDetails(bool justLookedUpDam)
    {
        // justLookedUpDam=true means the Sire is the untouched side here, and vice versa.
        if (justLookedUpDam)
            ApplyStagedOrDbParent(isDam: false);
        else
            ApplyStagedOrDbParent(isDam: true);
    }

    /// <summary>
    /// Hydrates one side of DamSire from a staged-but-unsaved Look Up match if one is on
    /// the draft, otherwise from the persisted DB record. Needed because Look Up for the dam
    /// and the sire post to two separate forms, so a match found for one parent would
    /// otherwise be lost as soon as the other parent's Look Up (or a page reload) runs.
    /// </summary>
    private void ApplyStagedOrDbParent(bool isDam)
    {
        var pending = isDam ? _draft?.PendingDam : _draft?.PendingSire;
        var removePending = isDam ? (_draft?.RemoveDamPending ?? false) : (_draft?.RemoveSirePending ?? false);

        if (removePending)
        {
            if (isDam)
            {
                DamSire.HasDam = false;
                DamSire.DamId = 0;
                DamSire.DamRbse = null;
            }
            else
            {
                DamSire.HasSire = false;
                DamSire.SireId = 0;
                DamSire.SireRbse = null;
            }
            return;
        }

        if (pending is not null)
        {
            if (isDam)
            {
                DamSire.HasDam = true;
                DamSire.DamId = pending.Id;
                DamSire.DamRbse = pending.Rbse;
                DamSire.DamEartag = pending.Eartag;
                DamSire.DamName = pending.Name;
                DamSire.DamHerdbook = pending.Herdbook;
                DamSire.DamBirthDay = pending.BirthDay;
                DamSire.DamBirthMonth = pending.BirthMonth;
                DamSire.DamBirthYear = pending.BirthYear;
                DamSire.DamRowStamp = pending.RowStampBase64;
                DamSire.DamFate = pending.Fate;
                DamSire.DamFinalResult = pending.FinalResult;
                DamSire.DamChildCount = pending.ChildCount;
            }
            else
            {
                DamSire.HasSire = true;
                DamSire.SireId = pending.Id;
                DamSire.SireRbse = pending.Rbse;
                DamSire.SireEartag = pending.Eartag;
                DamSire.SireName = pending.Name;
                DamSire.SireHerdbook = pending.Herdbook;
                DamSire.SireBirthDay = pending.BirthDay;
                DamSire.SireBirthMonth = pending.BirthMonth;
                DamSire.SireBirthYear = pending.BirthYear;
                DamSire.SireRowStamp = pending.RowStampBase64;
                DamSire.SireFate = pending.Fate;
                DamSire.SireChildCount = pending.ChildCount;
            }
            return;
        }

        if (isDam)
        {
            var dam = Details?.Dam;
            DamSire.HasDam = dam is { Id: > 0 };
            DamSire.DamId = dam?.Id ?? 0;
            DamSire.DamRbse = dam?.Rbse;
            DamSire.DamEartag = dam?.Eartag;
            DamSire.DamName = dam?.Name;
            DamSire.DamHerdbook = dam?.Herdbook;
            DamSire.DamBirthDay = dam?.BirthDay;
            DamSire.DamBirthMonth = dam?.BirthMonth;
            DamSire.DamBirthYear = dam?.BirthYear;
            DamSire.DamRowStamp = ToBase64(dam?.RowStamp);
            DamSire.DamFate = dam?.Fate;
            DamSire.DamFinalResult = dam?.FinalResult;
            DamSire.DamChildCount = dam?.ChildCount;
        }
        else
        {
            var sire = Details?.Sire;
            DamSire.HasSire = sire is { Id: > 0 };
            DamSire.SireId = sire?.Id ?? 0;
            DamSire.SireRbse = sire?.Rbse;
            DamSire.SireEartag = sire?.Eartag;
            DamSire.SireName = sire?.Name;
            DamSire.SireHerdbook = sire?.Herdbook;
            DamSire.SireBirthDay = sire?.BirthDay;
            DamSire.SireBirthMonth = sire?.BirthMonth;
            DamSire.SireBirthYear = sire?.BirthYear;
            DamSire.SireRowStamp = ToBase64(sire?.RowStamp);
            DamSire.SireFate = sire?.Fate;
            DamSire.SireChildCount = sire?.ChildCount;
        }
    }

    private async Task ApplyMatchAsync(bool isDam, DamSireDetailRecord match)
    {
        var pending = new PendingParentDraft
        {
            Id = match.Id,
            Rbse = match.Rbse,
            Eartag = match.Eartag,
            Name = match.Name,
            Herdbook = match.Herdbook,
            BirthDay = match.BirthDay,
            BirthMonth = match.BirthMonth,
            BirthYear = match.BirthYear,
            RowStampBase64 = ToBase64(match.RowStamp),
            Fate = match.Fate,
            FinalResult = isDam ? match.FinalResult : null,
            ChildCount = match.ChildCount
        };

        if (isDam)
        {
            DamSire.HasDam = true;
            DamSire.DamId = match.Id;
            DamSire.DamRbse = match.Rbse;
            DamSire.DamEartag = match.Eartag;
            DamSire.DamName = match.Name;
            DamSire.DamHerdbook = match.Herdbook;
            DamSire.DamBirthDay = match.BirthDay;
            DamSire.DamBirthMonth = match.BirthMonth;
            DamSire.DamBirthYear = match.BirthYear;
            DamSire.DamRowStamp = ToBase64(match.RowStamp);
            DamSire.DamFate = match.Fate;
            DamSire.DamFinalResult = match.FinalResult;
            DamSire.DamChildCount = match.ChildCount;

            if (_draft is not null)
            {
                _draft.PendingDam = pending;
                _draft.RemoveDamPending = false;
                await relationsDraftState.SetAsync(_draft);
            }
        }
        else
        {
            DamSire.HasSire = true;
            DamSire.SireId = match.Id;
            DamSire.SireRbse = match.Rbse;
            DamSire.SireEartag = match.Eartag;
            DamSire.SireName = match.Name;
            DamSire.SireHerdbook = match.Herdbook;
            DamSire.SireBirthDay = match.BirthDay;
            DamSire.SireBirthMonth = match.BirthMonth;
            DamSire.SireBirthYear = match.BirthYear;
            DamSire.SireRowStamp = ToBase64(match.RowStamp);
            DamSire.SireFate = match.Fate;
            DamSire.SireChildCount = match.ChildCount;

            if (_draft is not null)
            {
                _draft.PendingSire = pending;
                _draft.RemoveSirePending = false;
                await relationsDraftState.SetAsync(_draft);
            }
        }
    }

    private void SetError(bool isDam, string message)
    {
        if (isDam) DamError = message; else SireError = message;
    }

    private async Task LoadAsync()
    {
        var detailsTask  = relationsRepository.GetRelationsDetailsByRbseAsync(RbseHelper.ParseToRaw(Rbse));
        var batchTask    = batchRepository.GetBatchNumbersByRbseAsync(RbseHelper.ParseToRaw(Rbse));
        var rtTask       = lookups.GetLookupAsync(BSE.SharedKernel.LookupTableId.RelationType);
        var rfTask       = lookups.GetLookupAsync(BSE.SharedKernel.LookupTableId.RelationFate);
        var sxTask       = lookups.GetSexesAsync();

        await Task.WhenAll(detailsTask, batchTask, rtTask, rfTask, sxTask);

        Details = await detailsTask;
        BatchNumbers = (await batchTask).ToList().AsReadOnly();
        RelationTypes = await rtTask;
        RelationFates = (await rfTask)
            .Select(x => new LuRelationFate { Id = x.Id, Code = x.Code, Description = x.Description });
        Sexes = await sxTask;

        // GET only: replace whatever default DamSire model binding produced with the
        // persisted values. POST handlers must not call this — it would overwrite the
        // values the user just posted (typed search criteria, edited fields, etc.).
    }

    /// <summary>Loads (or initialises from the DB on first visit) the staged relations draft.</summary>
    private async Task<CaseRelationsDraftState> LoadOrInitializeRelationsDraftAsync()
    {
        var rbse = RbseHelper.ParseToRaw(Rbse);
        var draft = await relationsDraftState.GetAsync(rbse);
        if (draft is null)
        {
            draft = new CaseRelationsDraftState
            {
                Rbse = rbse,
                Relations = (Details?.Relations ?? []).Select(r => new CaseRelationsDraftItem
                {
                    ClientKey = Guid.NewGuid().ToString("N"),
                    Id = r.Id,
                    RelationType = r.RelationType ?? string.Empty,
                    RelationTypeDesc = r.RelationTypeDesc,
                    RelationRbse = r.RelationRbse,
                    Sex = r.Sex,
                    SexDesc = r.SexDesc,
                    BirthDay = r.BirthDay,
                    BirthMonth = r.BirthMonth,
                    BirthYear = r.BirthYear,
                    RelationFate = r.RelationFate,
                    RelationFateDesc = r.RelationFateDesc,
                    LeftDate = r.LeftDate,
                    EartagCountry = r.EartagCountry,
                    EartagHerdmark = r.EartagHerdmark,
                    Eartag = r.Eartag,
                    Sire = r.Sire,
                    RowStampBase64 = r.RowStamp is null ? string.Empty : Convert.ToBase64String(r.RowStamp)
                }).ToList(),
                RemoveDamPending = false,
                RemoveSirePending = false,
                HasPendingChanges = false
            };
            await relationsDraftState.SetAsync(draft);
        }

        RemoveDamPending = draft.RemoveDamPending;
        RemoveSirePending = draft.RemoveSirePending;

        _stagedRelations = draft.Relations.Select(r => new StagedRelationItem
        {
            ClientKey = r.ClientKey,
            Id = r.Id,
            RelationType = r.RelationType,
            RelationTypeDesc = r.RelationTypeDesc,
            RelationRbse = r.RelationRbse,
            Sex = r.Sex,
            SexDesc = r.SexDesc,
            BirthDay = r.BirthDay,
            BirthMonth = r.BirthMonth,
            BirthYear = r.BirthYear,
            RelationFate = r.RelationFate,
            RelationFateDesc = r.RelationFateDesc,
            LeftDate = r.LeftDate,
            EartagCountry = r.EartagCountry,
            EartagHerdmark = r.EartagHerdmark,
            Eartag = r.Eartag,
            Sire = r.Sire,
            RowStampBase64 = r.RowStampBase64
        }).ToList();

        HasUnsavedChanges = draft.HasPendingChanges;

        var sorted = SortStagedRelations(_stagedRelations);
        TotalRelationsCount = sorted.Count;
        TotalPages = Math.Max(1, (int)Math.Ceiling(TotalRelationsCount / (double)PageSize));
        PageNumber = Math.Clamp(PageNumber, 1, TotalPages);
        PagedRelations = sorted.Skip((PageNumber - 1) * PageSize).Take(PageSize).ToList();

        _draft = draft;
        return draft;
    }

    /// <summary>Adds a related-animal row to the draft only. Not persisted until Save.</summary>
    public async Task<IActionResult> OnPostAddRelationRowAsync()
    {
        if (!User.IsInRole("DataEntry"))
            return Forbid();

        await LoadAsync();
        var draft = await LoadOrInitializeRelationsDraftAsync();

        await ValidateAndDeriveRelationFieldsAsync(excludeClientKey: null);

        if (RelationFieldErrors.Count > 0)
        {
            ShowAddRelationRow = true;
            return Page();
        }

        draft.Relations.Add(new CaseRelationsDraftItem
        {
            ClientKey = Guid.NewGuid().ToString("N"),
            Id = null,
            RelationType = RelationType!,
            RelationTypeDesc = RelationTypes.FirstOrDefault(t => t.Code == RelationType)?.Description,
            RelationRbse = NullIfBlank(RbseHelper.Normalize(RelationRbse)),
            Sex = Sex,
            SexDesc = Sexes.FirstOrDefault(s => s.Code == Sex)?.Description,
            BirthDay = BirthDay,
            BirthMonth = BirthMonth,
            BirthYear = BirthYear,
            RelationFate = RelationFate,
            RelationFateDesc = RelationFates.FirstOrDefault(f => f.Code == RelationFate)?.Description,
            LeftDate = LeftDate,
            EartagCountry = EartagCountry,
            EartagHerdmark = EartagHerdmark,
            Eartag = Eartag,
            Sire = Sire,
            RowStampBase64 = string.Empty
        });
        draft.HasPendingChanges = true;
        await relationsDraftState.SetAsync(draft);
        TempData.Remove("Success");

        return RedirectToPage(new { rbse = Rbse });
    }

    /// <summary>Populates the shared field panel from a staged row for editing (no changes saved yet).</summary>
    public async Task<IActionResult> OnPostBeginEditRelationRowAsync(string clientKey)
    {
        if (!User.IsInRole("DataEntry"))
            return Forbid();

        await LoadAsync();
        var draft = await LoadOrInitializeRelationsDraftAsync();

        var item = draft.Relations.FirstOrDefault(r => r.ClientKey == clientKey);
        if (item is not null)
        {
            EditingClientKey = clientKey;
            RelationType = item.RelationType;
            RelationRbse = item.RelationRbse;
            Sex = item.Sex;
            BirthDay = item.BirthDay;
            BirthMonth = item.BirthMonth;
            BirthYear = item.BirthYear;
            BirthDate = item.BirthDay > 0 && item.BirthMonth > 0 && item.BirthYear > 0
                ? new DateTime(item.BirthYear.Value, item.BirthMonth.Value, item.BirthDay.Value)
                : null;
            RelationFate = item.RelationFate;
            LeftDate = item.LeftDate;
            EartagCountry = item.EartagCountry;
            EartagHerdmark = item.EartagHerdmark;
            Eartag = item.Eartag;
            Sire = item.Sire;
        }

        return Page();
    }

    /// <summary>Updates the currently selected staged row from the shared field panel. Not persisted until Save.</summary>
    public async Task<IActionResult> OnPostUpdateRelationRowAsync()
    {
        if (!User.IsInRole("DataEntry"))
            return Forbid();

        var clientKey = EditingClientKey;

        await LoadAsync();
        var draft = await LoadOrInitializeRelationsDraftAsync();

        var item = draft.Relations.FirstOrDefault(r => r.ClientKey == clientKey);
        if (item is null)
        {
            TempData["ErrorMessage"] = "The relation record being edited no longer exists.";
            return RedirectToPage(new { rbse = Rbse });
        }

        await ValidateAndDeriveRelationFieldsAsync(excludeClientKey: clientKey);

        if (RelationFieldErrors.Count > 0)
        {
            EditingClientKey = clientKey;
            ReopenEditClientKey = clientKey;
            return Page();
        }

        item.RelationType = RelationType!;
        item.RelationTypeDesc = RelationTypes.FirstOrDefault(t => t.Code == RelationType)?.Description;
        item.RelationRbse = NullIfBlank(RbseHelper.Normalize(RelationRbse));
        item.Sex = Sex;
        item.SexDesc = Sexes.FirstOrDefault(s => s.Code == Sex)?.Description;
        item.BirthDay = BirthDay;
        item.BirthMonth = BirthMonth;
        item.BirthYear = BirthYear;
        item.RelationFate = RelationFate;
        item.RelationFateDesc = RelationFates.FirstOrDefault(f => f.Code == RelationFate)?.Description;
        item.LeftDate = LeftDate;
        item.EartagCountry = EartagCountry;
        item.EartagHerdmark = EartagHerdmark;
        item.Eartag = Eartag;
        item.Sire = Sire;
        draft.HasPendingChanges = true;
        await relationsDraftState.SetAsync(draft);
        TempData.Remove("Success");

        return RedirectToPage(new { rbse = Rbse });
    }

    /// <summary>Removes a staged related-animal row. Not persisted until Save.</summary>
    public async Task<IActionResult> OnPostDeleteRelationRowAsync(string clientKey)
    {
        if (!User.IsInRole("DataEntry"))
            return Forbid();

        await LoadAsync();
        var draft = await LoadOrInitializeRelationsDraftAsync();

        var item = draft.Relations.FirstOrDefault(r => r.ClientKey == clientKey);
        if (item is not null)
        {
            draft.Relations.Remove(item);
            draft.HasPendingChanges = true;
            await relationsDraftState.SetAsync(draft);
            TempData.Remove("Success");
        }

        return RedirectToPage(new { rbse = Rbse });
    }

    /// <summary>Commits all staged related-animal changes to the database in one transaction.</summary>
    public async Task<IActionResult> OnPostSaveRelationsAsync()
    {
        if (!User.IsInRole("DataEntry"))
            return Forbid();

        TempData.Remove("RelationsWarning");

        await LoadAsync();
        var draft = await LoadOrInitializeRelationsDraftAsync();

        if (draft.RemoveDamPending && !HasDamInputStaged())
        {
            DamSire.HasDam = false;
            DamSire.DamId = 0;
            DamSire.DamRbse = null;
            DamSire.DamEartag = null;
            DamSire.DamName = null;
            DamSire.DamHerdbook = null;
            DamSire.DamBirthDay = null;
            DamSire.DamBirthMonth = null;
            DamSire.DamBirthYear = null;
            DamSire.DamRowStamp = null;
            DamSire.DamFate = null;
            DamSire.DamFinalResult = null;
            DamSire.DamChildCount = null;
        }

        if (draft.RemoveSirePending && !HasSireInputStaged())
        {
            DamSire.HasSire = false;
            DamSire.SireId = 0;
            DamSire.SireRbse = null;
            DamSire.SireEartag = null;
            DamSire.SireName = null;
            DamSire.SireHerdbook = null;
            DamSire.SireBirthDay = null;
            DamSire.SireBirthMonth = null;
            DamSire.SireBirthYear = null;
            DamSire.SireRowStamp = null;
            DamSire.SireFate = null;
            DamSire.SireChildCount = null;
        }

        var caseRbse = RbseHelper.ParseToRaw(Rbse);
        var caseRecord = await caseService.GetCaseAsync(caseRbse);

        if (caseRecord is null)
        {
            TempData["Warning"] = $"Case '{caseRbse}' is not saved yet. Complete Farm first.";
            return RedirectToPage(new { rbse = Rbse });
        }

        // Keep explicit parent intent even if HasDam/HasSire hidden flags are stale in the post.
        if (!DamSire.HasDam && (DamSire.DamId > 0 || !string.IsNullOrWhiteSpace(DamSire.DamRbse)))
            DamSire.HasDam = true;
        if (!DamSire.HasSire && (DamSire.SireId > 0 || !string.IsNullOrWhiteSpace(DamSire.SireRbse)))
            DamSire.HasSire = true;

        if (!string.IsNullOrWhiteSpace(DamSire.DamRbse)
            && RbseHelper.Normalize(DamSire.DamRbse) == caseRbse)
        {
            DamError = SameAsCaseRbse;
            return Page();
        }

        if (!string.IsNullOrWhiteSpace(DamSire.SireRbse)
            && RbseHelper.Normalize(DamSire.SireRbse) == caseRbse)
        {
            SireError = SameAsCaseRbse;
            return Page();
        }

        if (DamSire.DamBirthDay.HasValue && !DamSire.DamBirthMonth.HasValue)
        {
            DamError = "Please enter a month, or remove the day.";
            return Page();
        }

        if (DamSire.SireBirthDay.HasValue && !DamSire.SireBirthMonth.HasValue)
        {
            SireError = "Please enter a month, or remove the day.";
            return Page();
        }

        if (!string.IsNullOrWhiteSpace(DamSire.DamRbse)
            && (DamSire.DamId <= 0 || string.IsNullOrWhiteSpace(DamSire.DamRowStamp)))
        {
            var damMatches = await relationsRepository.GetDamSireDetailsMatchesAsync(
                null, null, RbseHelper.Normalize(DamSire.DamRbse), null, "F");
            if (damMatches.Count > 1)
            {
                DamError = "Multiple dam matches were found. Please use Look Up and select a single record.";
                return Page();
            }

            var linkedDam = damMatches.FirstOrDefault();
            if (linkedDam is not null)
            {
                DamSire.HasDam = true;
                DamSire.DamId = linkedDam.Id;
                DamSire.DamEartag = linkedDam.Eartag;
                DamSire.DamName = linkedDam.Name;
                DamSire.DamHerdbook = linkedDam.Herdbook;
                DamSire.DamBirthDay = linkedDam.BirthDay;
                DamSire.DamBirthMonth = linkedDam.BirthMonth;
                DamSire.DamBirthYear = linkedDam.BirthYear;
                DamSire.DamRowStamp = ToBase64(linkedDam.RowStamp);
            }
        }

        if (!string.IsNullOrWhiteSpace(DamSire.SireRbse)
            && (DamSire.SireId <= 0 || string.IsNullOrWhiteSpace(DamSire.SireRowStamp)))
        {
            var sireMatches = await relationsRepository.GetDamSireDetailsMatchesAsync(
                null, null, RbseHelper.Normalize(DamSire.SireRbse), null, "M");
            if (sireMatches.Count > 1)
            {
                SireError = "Multiple sire matches were found. Please use Look Up and select a single record.";
                return Page();
            }

            var linkedSire = sireMatches.FirstOrDefault();
            if (linkedSire is not null)
            {
                DamSire.HasSire = true;
                DamSire.SireId = linkedSire.Id;
                DamSire.SireEartag = linkedSire.Eartag;
                DamSire.SireName = linkedSire.Name;
                DamSire.SireHerdbook = linkedSire.Herdbook;
                DamSire.SireBirthDay = linkedSire.BirthDay;
                DamSire.SireBirthMonth = linkedSire.BirthMonth;
                DamSire.SireBirthYear = linkedSire.BirthYear;
                DamSire.SireRowStamp = ToBase64(linkedSire.RowStamp);
            }
        }

        // If a looked-up/linked parent is selected but the posted RowStamp is missing,
        // reload it before calling AddEditDamSireDetails to avoid false concurrency failures.
        if (DamSire.HasDam && DamSire.DamId > 0 && string.IsNullOrWhiteSpace(DamSire.DamRowStamp) && !string.IsNullOrWhiteSpace(DamSire.DamRbse))
        {
            var damMatches = await relationsRepository.GetDamSireDetailsMatchesAsync(
                null, null, RbseHelper.Normalize(DamSire.DamRbse), null, "F");
            var exactDam = damMatches.FirstOrDefault(m => m.Id == DamSire.DamId) ?? damMatches.FirstOrDefault();
            if (exactDam?.RowStamp is { Length: > 0 })
                DamSire.DamRowStamp = ToBase64(exactDam.RowStamp);
        }

        if (DamSire.HasSire && DamSire.SireId > 0 && string.IsNullOrWhiteSpace(DamSire.SireRowStamp) && !string.IsNullOrWhiteSpace(DamSire.SireRbse))
        {
            var sireMatches = await relationsRepository.GetDamSireDetailsMatchesAsync(
                null, null, RbseHelper.Normalize(DamSire.SireRbse), null, "M");
            var exactSire = sireMatches.FirstOrDefault(m => m.Id == DamSire.SireId) ?? sireMatches.FirstOrDefault();
            if (exactSire?.RowStamp is { Length: > 0 })
                DamSire.SireRowStamp = ToBase64(exactSire.RowStamp);
        }

        if (DamSire.HasDam && DamSire.DamId > 0 && string.IsNullOrWhiteSpace(DamSire.DamRowStamp))
        {
            DamError = "Dam details could not be refreshed. Please look up and select the dam again.";
            return Page();
        }

        if (DamSire.HasSire && DamSire.SireId > 0 && string.IsNullOrWhiteSpace(DamSire.SireRowStamp))
        {
            SireError = "Sire details could not be refreshed. Please look up and select the sire again.";
            return Page();
        }

        if (caseRecord is not null)
        {
            var editVm = BSE.Host.Models.ViewModels.CaseEditViewModel.FromRecord(caseRecord);
            editVm.DamStatus = DamSire.DamStatus;
            var editCommand = new EditCaseDetailsCommand(
                Case: editVm.ToEditCommand(caseRecord.RowStamp ?? []),
                Clinical: null,
                Bab: null,
                DamSire: null);
            var userId = await currentUser.GetUserIdAsync();
            var result = await caseService.EditCaseAsync(editCommand, userId);
            if (result != EditCaseResult.Success)
            {
                TempData["RelationsWarning"] = "Dam details were saved, but the status could not be updated — the case may have changed. Please try again.";
                return RedirectToPage(new { rbse = Rbse });
            }
        }

        caseRecord = await caseService.GetCaseAsync(caseRbse);
        CaseHerdbook = string.IsNullOrWhiteSpace(CaseHerdbook) ? caseRecord?.Herdbook : CaseHerdbook;

        // For linked case parents (RBSE present), ignore editable posted fields and refresh
        // from DB so final save only updates the case linkage, not the linked pedigree row.
        if (DamSire.HasDam && DamSire.DamId > 0 && !string.IsNullOrWhiteSpace(DamSire.DamRbse))
        {
            var linkedDam = await GetPedigreeSnapshotByIdAsync(DamSire.DamId);
            if (linkedDam is not null)
            {
                DamSire.DamEartag = linkedDam.Eartag;
                DamSire.DamName = linkedDam.Name;
                DamSire.DamHerdbook = linkedDam.Herdbook;
                DamSire.DamBirthDay = linkedDam.BirthDay;
                DamSire.DamBirthMonth = linkedDam.BirthMonth;
                DamSire.DamBirthYear = linkedDam.BirthYear;
                DamSire.DamRowStamp = ToBase64(linkedDam.RowStamp);
            }
        }

        if (DamSire.HasSire && DamSire.SireId > 0 && !string.IsNullOrWhiteSpace(DamSire.SireRbse))
        {
            var linkedSire = await GetPedigreeSnapshotByIdAsync(DamSire.SireId);
            if (linkedSire is not null)
            {
                DamSire.SireEartag = linkedSire.Eartag;
                DamSire.SireName = linkedSire.Name;
                DamSire.SireHerdbook = linkedSire.Herdbook;
                DamSire.SireBirthDay = linkedSire.BirthDay;
                DamSire.SireBirthMonth = linkedSire.BirthMonth;
                DamSire.SireBirthYear = linkedSire.BirthYear;
                DamSire.SireRowStamp = ToBase64(linkedSire.RowStamp);
            }
        }

        var herdbookCommand = new AddEditDamSireCommand(
            Rbse: caseRbse,
            DamId: DamSire.HasDam ? DamSire.DamId : null, DamRbse: NullIfBlank(RbseHelper.Normalize(DamSire.DamRbse)),
            DamEartag: DamSire.DamEartag, DamName: DamSire.DamName, DamHerdbook: DamSire.DamHerdbook,
            DamBirthDay: DamSire.DamBirthDay, DamBirthMonth: DamSire.DamBirthMonth, DamBirthYear: DamSire.DamBirthYear,
            DamRowStamp: FromBase64(DamSire.DamRowStamp),
            SireId: DamSire.HasSire ? DamSire.SireId : null, SireRbse: NullIfBlank(RbseHelper.Normalize(DamSire.SireRbse)),
            SireEartag: DamSire.SireEartag, SireName: DamSire.SireName, SireHerdbook: DamSire.SireHerdbook,
            SireBirthDay: DamSire.SireBirthDay, SireBirthMonth: DamSire.SireBirthMonth, SireBirthYear: DamSire.SireBirthYear,
            SireRowStamp: FromBase64(DamSire.SireRowStamp),
            CaseHerdbook: NullIfBlank(CaseHerdbook),
            CaseRowStamp: caseRecord?.PedigreeRowStamp);

        try
        {
            using var conn = connectionFactory.CreateConnection();
            conn.Open();
            using (var setCmd = conn.CreateCommand())
            {
                setCmd.CommandText = "SET ARITHABORT ON";
                setCmd.ExecuteNonQuery();
            }
            using var tx = conn.BeginTransaction();
            await pedigreeRepository.AddEditDamSireAsync(herdbookCommand, conn, tx);
            tx.Commit();
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Failed to update case herdbook from Relations Save action. rbse={Rbse} damId={DamId} sireId={SireId} damRbse={DamRbse} sireRbse={SireRbse}",
                caseRbse,
                DamSire.HasDam ? DamSire.DamId : null,
                DamSire.HasSire ? DamSire.SireId : null,
                DamSire.DamRbse,
                DamSire.SireRbse);

            // Legacy UpdateDamSireRecords mapped the SP's return code to one of these four
            // specific messages instead of a single generic one.
            TempData["RelationsWarning"] = TryGetDamSireReturnCode(ex, out var returnCode)
                ? returnCode switch
                {
                    1 => "Failed to create or update a dam record. The record may have been changed by another user.",
                    2 => "Failed to create or update a sire record. The record may have been changed by another user.",
                    3 => "Failed to create a pedigree record for the case.",
                    4 => "Failed to update the case's pedigree record with pointers to the dam and sire information. The record may have been changed by another user.",
                    _ => "Unable to update case herdbook. Please reload and try again."
                }
                : "Unable to update case herdbook. Please reload and try again.";
            return RedirectToPage(new { rbse = Rbse });
        }

        var relationsError = await PersistStagedRelationsAsync();
        if (relationsError is not null)
        {
            TempData["RelationsWarning"] = relationsError;
            return RedirectToPage(new { rbse = Rbse });
        }

        await relationsDraftState.ClearAsync(RbseHelper.ParseToRaw(Rbse));

        TempData.Remove("RelationsWarning");
        TempData["Success"] = "Related animal changes saved.";
        return RedirectToPage(new { rbse = Rbse });
    }

    /// <summary>Discards all staged related-animal changes without persisting them.</summary>
    public async Task<IActionResult> OnPostCancelRelationsEditAsync()
    {
        await relationsDraftState.ClearAsync(RbseHelper.ParseToRaw(Rbse));
        return RedirectToPage("/Home");
    }

    public async Task<IActionResult> OnGetCancelRelationsEditAsync()
    {
        await relationsDraftState.ClearAsync(RbseHelper.ParseToRaw(Rbse));
        return RedirectToPage("/Home");
    }

    /// <summary>
    /// Shared validation for the add/edit relation panel. Mirrors legacy
    /// ctlRelationRBSE_RBSEChanged: once a relation RBSE is supplied, Sex, Fate, Eartag,
    /// birth date, left date and Sire are always taken live from that case.
    /// </summary>
    private async Task ValidateAndDeriveRelationFieldsAsync(string? excludeClientKey)
    {
        BirthDay = BirthDate?.Day;
        BirthMonth = BirthDate?.Month;
        BirthYear = BirthDate?.Year;

        var otherRelationRbses = _stagedRelations
            .Where(r => r.ClientKey != excludeClientKey)
            .Select(r => r.RelationRbse);

        RelationFieldErrors = RelationValidation.Validate(
            new RelationValidation.Input(
                RbseHelper.ParseToRaw(Rbse), RelationRbse, RelationType, Sex,
                EartagCountry, EartagHerdmark, Eartag,
                BirthDay, BirthMonth, BirthYear, LeftDate),
            otherRelationRbses,
            Details?.Dam?.Rbse,
            Details?.Sire?.Rbse);

        if (RelationFieldErrors.Count > 0)
            return;

        var normalizedRbse = RbseHelper.Normalize(RelationRbse);
        if (normalizedRbse.Length > 0)
        {
            var related = await relationsRepository.GetRelationDetailsOfRelatedCaseAsync(normalizedRbse);
            if (related is null)
            {
                RelationFieldErrors = new Dictionary<string, string> { ["RelationRbse"] = RelationValidation.RbseNotFound };
                return;
            }

            Sex = related.Sex;
            RelationFate = related.Fate;
            EartagCountry = related.EartagCountry;
            EartagHerdmark = related.EartagHerdmark;
            Eartag = related.Eartag;
            BirthDay = related.BirthDay;
            BirthMonth = related.BirthMonth;
            BirthYear = related.BirthYear;
            BirthDate = related.BirthDay > 0 && related.BirthMonth > 0 && related.BirthYear > 0
                ? new DateTime(related.BirthYear.Value, related.BirthMonth.Value, related.BirthDay.Value)
                : null;
            LeftDate = DateTime.TryParse(related.LeftDate, out var leftDate) ? leftDate : null;
            Sire = related.Name;
        }
    }

    /// <summary>Persists staged relation changes. Returns an error message if a row was
    /// changed or deleted by someone else since it was staged (legacy's
    /// OnRelationRowUpdated/"Data was changed by another user"), otherwise null.</summary>
    private async Task<string?> PersistStagedRelationsAsync()
    {
        var persisted = Details?.Relations ?? [];
        var persistedById = persisted.ToDictionary(r => r.Id);
        var stagedByExistingId = _stagedRelations.Where(r => r.Id is > 0).ToDictionary(r => r.Id!.Value);

        using var conn = connectionFactory.CreateConnection();
        conn.Open();
        using var tx = conn.BeginTransaction();

        foreach (var removed in persisted.Where(r => !stagedByExistingId.ContainsKey(r.Id)))
        {
            if (removed.RowStamp is null)
                continue;

            var deleted = await relationsRepository.DeleteRelationAsync(new DeleteCaseRelationCommand(removed.Id, removed.RowStamp), conn, tx);
            if (deleted == 0)
            {
                tx.Rollback();
                return "A related animal record was changed by another user. Please reload and try again.";
            }
        }

        var caseRbse = RbseHelper.ParseToRaw(Rbse);
        foreach (var staged in _stagedRelations)
        {
            if (staged.Id is null or <= 0)
            {
                await relationsRepository.AddRelationAsync(new AddCaseRelationCommand(
                    caseRbse, staged.RelationType, staged.RelationRbse, staged.Sex,
                    ToByte(staged.BirthDay), ToByte(staged.BirthMonth), ToShort(staged.BirthYear),
                    staged.RelationFate, staged.LeftDate,
                    staged.EartagCountry, staged.EartagHerdmark, staged.Eartag, staged.Sire), conn, tx);
                continue;
            }

            if (!persistedById.TryGetValue(staged.Id.Value, out var current))
                continue;

            var changed = !string.Equals(current.RelationType, staged.RelationType, StringComparison.OrdinalIgnoreCase)
                          || !string.Equals(current.RelationRbse ?? "", staged.RelationRbse ?? "", StringComparison.OrdinalIgnoreCase)
                          || !string.Equals(current.Sex ?? "", staged.Sex ?? "", StringComparison.OrdinalIgnoreCase)
                          || current.BirthDay != staged.BirthDay || current.BirthMonth != staged.BirthMonth || current.BirthYear != staged.BirthYear
                          || !string.Equals(current.RelationFate ?? "", staged.RelationFate ?? "", StringComparison.OrdinalIgnoreCase)
                          || current.LeftDate != staged.LeftDate
                          || !string.Equals(current.Eartag ?? "", staged.Eartag ?? "", StringComparison.OrdinalIgnoreCase);

            if (!changed)
                continue;

            var rowStamp = string.IsNullOrWhiteSpace(staged.RowStampBase64)
                ? current.RowStamp ?? []
                : Convert.FromBase64String(staged.RowStampBase64);

            var updated = await relationsRepository.EditRelationAsync(new EditCaseRelationCommand(
                staged.Id.Value, staged.RelationType, staged.RelationRbse, staged.Sex,
                ToByte(staged.BirthDay), ToByte(staged.BirthMonth), ToShort(staged.BirthYear),
                staged.RelationFate, staged.LeftDate,
                staged.EartagCountry, staged.EartagHerdmark, staged.Eartag, staged.Sire, rowStamp), conn, tx);
            if (updated == 0)
            {
                tx.Rollback();
                return "A related animal record was changed by another user. Please reload and try again.";
            }
        }

        tx.Commit();
        return null;
    }

    private static byte? ToByte(int? v) => v is > 0 and <= 255 ? (byte)v.Value : null;
    private static short? ToShort(int? v) => v.HasValue ? (short?)v.Value : null;

    private static bool TryGetDamSireReturnCode(Exception ex, out int returnCode)
    {
        returnCode = 0;
        if (ex is not InvalidOperationException || string.IsNullOrWhiteSpace(ex.Message))
            return false;

        const string marker = "returned code ";
        var markerIndex = ex.Message.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (markerIndex < 0)
            return false;

        var start = markerIndex + marker.Length;
        var end = start;
        while (end < ex.Message.Length && char.IsDigit(ex.Message[end]))
            end++;

        return end > start && int.TryParse(ex.Message[start..end], out returnCode);
    }

    private async Task<PedigreeSnapshot?> GetPedigreeSnapshotByIdAsync(int pedigreeId)
    {
        using var conn = connectionFactory.CreateConnection();
        conn.Open();
        using (var setCmd = conn.CreateCommand())
        {
            setCmd.CommandText = "SET ARITHABORT ON";
            setCmd.ExecuteNonQuery();
        }

        return await conn.QuerySingleOrDefaultAsync<PedigreeSnapshot>(
            @"SELECT [ID] AS [Id], [RBSE], [Eartag], [Name], [Herdbook], [BirthDay], [BirthMonth], [BirthYear], [RowStamp]
              FROM [Pedigree]
              WHERE [ID] = @ID",
            new { ID = pedigreeId });
    }

    private sealed record PedigreeSnapshot
    {
        public int Id { get; init; }
        public string? Rbse { get; init; }
        public string? Eartag { get; init; }
        public string? Name { get; init; }
        public string? Herdbook { get; init; }
        public int? BirthDay { get; init; }
        public int? BirthMonth { get; init; }
        public int? BirthYear { get; init; }
        public byte[]? RowStamp { get; init; }
    }

    /// <summary>Populates DamSire from the freshly-loaded Details. GET requests only.</summary>
    private void PopulateDamSireFromDetails()
    {
        if (RemoveDamPending)
        {
            Details = Details is null
                ? null
                : Details with { Dam = null };
        }

        if (RemoveSirePending)
        {
            Details = Details is null
                ? null
                : Details with { Sire = null };
        }

        DamSire = new DamSireViewModel();
        ApplyStagedOrDbParent(isDam: true);
        ApplyStagedOrDbParent(isDam: false);
    }

    /// <summary>Sorts on every legacy grid column (RelationType, RBSE, Sex, Birth Date, Fate, Date Left, Eartag, Sire).</summary>
    private IReadOnlyList<StagedRelationItem> SortStagedRelations(IReadOnlyList<StagedRelationItem> relations)
    {
        IEnumerable<StagedRelationItem> q = relations;
        q = SortColumn switch
        {
            "RelationType" => SortDesc ? q.OrderByDescending(r => r.RelationTypeDesc ?? r.RelationType) : q.OrderBy(r => r.RelationTypeDesc ?? r.RelationType),
            "RelationRbse" => SortDesc ? q.OrderByDescending(r => r.RelationRbse)                        : q.OrderBy(r => r.RelationRbse),
            "Sex"          => SortDesc ? q.OrderByDescending(r => r.SexDesc ?? r.Sex)                     : q.OrderBy(r => r.SexDesc ?? r.Sex),
            "BirthDate"    => SortDesc ? q.OrderByDescending(r => r.BirthYear).ThenByDescending(r => r.BirthMonth).ThenByDescending(r => r.BirthDay)
                                       : q.OrderBy(r => r.BirthYear).ThenBy(r => r.BirthMonth).ThenBy(r => r.BirthDay),
            "RelationFate" => SortDesc ? q.OrderByDescending(r => r.RelationFateDesc ?? r.RelationFate)   : q.OrderBy(r => r.RelationFateDesc ?? r.RelationFate),
            "LeftDate"     => SortDesc ? q.OrderByDescending(r => r.LeftDate)                             : q.OrderBy(r => r.LeftDate),
            "Eartag"       => SortDesc ? q.OrderByDescending(r => r.Eartag)                               : q.OrderBy(r => r.Eartag),
            "Sire"         => SortDesc ? q.OrderByDescending(r => r.Sire)                                 : q.OrderBy(r => r.Sire),
            _              => q
        };
        return q.ToList();
    }

    private static string? NullIfBlank(string? s) => string.IsNullOrWhiteSpace(s) ? null : s;
    private static string? ToBase64(byte[]? b) => b is { Length: > 0 } ? Convert.ToBase64String(b) : null;
    private static byte[]? FromBase64(string? s) => string.IsNullOrEmpty(s) ? null : Convert.FromBase64String(s);

    private async Task<bool> ApplyPendingParentFromTempData(bool isDam)
    {
        var key = isDam ? PendingDamSireKeys.Dam : PendingDamSireKeys.Sire;
        if (TempData[key] is not string json || string.IsNullOrWhiteSpace(json))
            return false;

        PendingDamSire? pending;
        try
        {
            pending = JsonSerializer.Deserialize<PendingDamSire>(json);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to parse pending {ParentType} selection from PickSireDam", isDam ? "dam" : "sire");
            return false;
        }

        if (pending is null)
            return false;

        if (!string.IsNullOrWhiteSpace(pending.Rbse)
            && RbseHelper.Normalize(pending.Rbse) == RbseHelper.ParseToRaw(Rbse))
        {
            SetError(isDam, SameAsCaseRbse);
            return false;
        }

        var pendingDraft = new PendingParentDraft
        {
            Id = pending.Id,
            Rbse = pending.Rbse,
            Eartag = pending.Eartag,
            Name = pending.Name,
            Herdbook = pending.Herdbook,
            BirthDay = pending.BirthDay,
            BirthMonth = pending.BirthMonth,
            BirthYear = pending.BirthYear,
            RowStampBase64 = pending.RowStampBase64,
            Fate = pending.Fate,
            FinalResult = pending.FinalResult,
            ChildCount = pending.ChildCount
        };

        if (isDam)
        {
            DamSire.HasDam = true;
            DamSire.DamId = pending.Id;
            DamSire.DamRbse = pending.Rbse;
            DamSire.DamEartag = pending.Eartag;
            DamSire.DamName = pending.Name;
            DamSire.DamHerdbook = pending.Herdbook;
            DamSire.DamBirthDay = pending.BirthDay;
            DamSire.DamBirthMonth = pending.BirthMonth;
            DamSire.DamBirthYear = pending.BirthYear;
            DamSire.DamRowStamp = pending.RowStampBase64;
            DamSire.DamFate = pending.Fate;
            DamSire.DamFinalResult = pending.FinalResult;
            DamSire.DamChildCount = pending.ChildCount;

            // Persisted here too (not just this render's DamSire) so a subsequent Look Up
            // for the other parent — a separate request/form — doesn't lose this selection.
            if (_draft is not null)
            {
                _draft.PendingDam = pendingDraft;
                _draft.RemoveDamPending = false;
                await relationsDraftState.SetAsync(_draft);
            }
            return true;
        }

        DamSire.HasSire = true;
        DamSire.SireId = pending.Id;
        DamSire.SireRbse = pending.Rbse;
        DamSire.SireEartag = pending.Eartag;
        DamSire.SireName = pending.Name;
        DamSire.SireHerdbook = pending.Herdbook;
        DamSire.SireBirthDay = pending.BirthDay;
        DamSire.SireBirthMonth = pending.BirthMonth;
        DamSire.SireBirthYear = pending.BirthYear;
        DamSire.SireRowStamp = pending.RowStampBase64;
        DamSire.SireFate = pending.Fate;
        DamSire.SireChildCount = pending.ChildCount;

        if (_draft is not null)
        {
            _draft.PendingSire = pendingDraft;
            _draft.RemoveSirePending = false;
            await relationsDraftState.SetAsync(_draft);
        }
        return true;
    }

    private async Task<IActionResult> LoadRelationsPageAsync(bool editCaseHerdbook)
    {
        SpolSiteUrl = configuration["SpolSiteUrl"] ?? string.Empty;
        await LoadAsync();
        var draft = await LoadOrInitializeRelationsDraftAsync();
        PopulateDamSireFromDetails();
        var appliedDam = await ApplyPendingParentFromTempData(isDam: true);
        var appliedSire = await ApplyPendingParentFromTempData(isDam: false);

        var draftChanged = false;
        if (appliedDam && draft.RemoveDamPending)
        {
            draft.RemoveDamPending = false;
            draftChanged = true;
        }

        if (appliedSire && draft.RemoveSirePending)
        {
            draft.RemoveSirePending = false;
            draftChanged = true;
        }

        if (draftChanged)
        {
            draft.HasPendingChanges = true;
            await relationsDraftState.SetAsync(draft);
            RemoveDamPending = draft.RemoveDamPending;
            RemoveSirePending = draft.RemoveSirePending;
        }

        EditCaseHerdbook = editCaseHerdbook;

        await PopulateCaseAncillaryStateAsync();

        return Page();
    }

    /// <summary>Case herdbook, dam status options/description, and SPOL link — shared by the
    /// full page load and by the Look Up handlers when they re-render this page directly.</summary>
    private async Task PopulateCaseAncillaryStateAsync()
    {
        SpolSiteUrl = configuration["SpolSiteUrl"] ?? string.Empty;

        var caseRecord = await caseService.GetCaseAsync(RbseHelper.ParseToRaw(Rbse));
        CaseHerdbook = caseRecord?.Herdbook;

        // Legacy ddlDamStatus — Case.DamStatus, distinct from the dam's own case Fate.
        var statuses = await lookups.GetAnimalStatusesAsync();
        DamStatusOptions = statuses
            .Select(x => new LookupItem(x.Id, x.Code, x.Description))
            .ToList();

        DamStatusDescription = !string.IsNullOrWhiteSpace(caseRecord?.DamStatus)
            ? DamStatusOptions.FirstOrDefault(s => s.Code == caseRecord.DamStatus)?.Description ?? caseRecord.DamStatus
            : null;

        DamSire.DamStatus = caseRecord?.DamStatus;
    }

    private bool HasDamInputStaged()
        => DamSire.HasDam
           || DamSire.DamId > 0
           || !string.IsNullOrWhiteSpace(DamSire.DamRbse)
           || !string.IsNullOrWhiteSpace(DamSire.DamEartag)
           || !string.IsNullOrWhiteSpace(DamSire.DamName)
           || !string.IsNullOrWhiteSpace(DamSire.DamHerdbook)
           || DamSire.DamBirthDay.HasValue
           || DamSire.DamBirthMonth.HasValue
           || DamSire.DamBirthYear.HasValue;

    private bool HasSireInputStaged()
        => DamSire.HasSire
           || DamSire.SireId > 0
           || !string.IsNullOrWhiteSpace(DamSire.SireRbse)
           || !string.IsNullOrWhiteSpace(DamSire.SireEartag)
           || !string.IsNullOrWhiteSpace(DamSire.SireName)
           || !string.IsNullOrWhiteSpace(DamSire.SireHerdbook)
           || DamSire.SireBirthDay.HasValue
           || DamSire.SireBirthMonth.HasValue
           || DamSire.SireBirthYear.HasValue;

    // ── View models ──────────────────────────────────────────────────────────

    /// <summary>A staged (possibly unsaved) related-animal row shown in the grid.</summary>
    public class StagedRelationItem
    {
        public string ClientKey { get; set; } = string.Empty;
        public int? Id { get; set; }
        public string RelationType { get; set; } = string.Empty;
        public string? RelationTypeDesc { get; set; }
        public string? RelationRbse { get; set; }
        public string? Sex { get; set; }
        public string? SexDesc { get; set; }
        public int? BirthDay { get; set; }
        public int? BirthMonth { get; set; }
        public int? BirthYear { get; set; }
        public string? RelationFate { get; set; }
        public string? RelationFateDesc { get; set; }
        public DateTime? LeftDate { get; set; }
        public string? EartagCountry { get; set; }
        public string? EartagHerdmark { get; set; }
        public string? Eartag { get; set; }
        public string? Sire { get; set; }
        public string RowStampBase64 { get; set; } = string.Empty;

        /// <summary>True for rows that only exist in the draft (never persisted).</summary>
        public bool IsUnsaved => Id is null or <= 0;
    }

    public class DamSireViewModel
    {
        /// <summary>
        /// True once a dam is recorded against the case, whether linked to a real RBSE,
        /// looked up as an unlinked pedigree entry, or freshly created via "New" (DamId
        /// stays 0 for a brand new entry, so this must not be inferred from DamId).
        /// </summary>
        public bool    HasDam         { get; set; }
        public int     DamId          { get; set; }
        public string? DamRbse        { get; set; }
        public string? DamEartag { get; set; }
        public string? DamName { get; set; }
        public string? DamHerdbook { get; set; }
        public int? DamBirthDay { get; set; }
        public int? DamBirthMonth { get; set; }
        public int? DamBirthYear { get; set; }
        public string? DamRowStamp    { get; set; }
        public string? DamFate        { get; set; }
        public string? DamFinalResult { get; set; }
        public string? DamStatus      { get; set; }
        public int?    DamChildCount  { get; set; }

        /// <summary>Same reasoning as <see cref="HasDam"/>, for the sire.</summary>
        public bool    HasSire        { get; set; }
        public int     SireId          { get; set; }
        public string? SireRbse        { get; set; }
        public string? SireEartag { get; set; }
        public string? SireName { get; set; }
        public string? SireHerdbook { get; set; }
        public int? SireBirthDay { get; set; }
        public int? SireBirthMonth { get; set; }
        public int? SireBirthYear { get; set; }
        public string? SireRowStamp    { get; set; }
        public string? SireFate        { get; set; }
        public int?    SireChildCount  { get; set; }

        // ── Look-up search criteria ──────────────────────────────────────────
        public string? DamSearchRbse { get; set; }
        public string? DamSearchEartag { get; set; }
        public string? DamSearchName { get; set; }
        public string? DamSearchHerdbook { get; set; }
        public string? SireSearchRbse { get; set; }
        public string? SireSearchEartag { get; set; }
        public string? SireSearchName { get; set; }
        public string? SireSearchHerdbook { get; set; }
    }

}
