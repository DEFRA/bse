using System.Text.Json;
using BSE.Host.Services;
using BSE.Infrastructure;
using BSE.Modules.AnimalRelations.Commands;
using BSE.Modules.AnimalRelations.Models;
using BSE.Modules.AnimalRelations.Repositories;
using BSE.Modules.Batch.Models;
using BSE.Modules.Batch.Repositories;
using BSE.Modules.CaseManagement.Commands;
using BSE.Modules.CaseManagement.Repositories;
using BSE.Modules.CaseManagement.Services;
using BSE.Modules.ReferenceData.Models;
using BSE.Modules.ReferenceData.Services;
using BSE.SharedKernel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;

namespace BSE.Host.Pages.Case;

[Authorize]
public class RelationsModel(
    IAnimalRelationsRepository relationsRepository,
    IPedigreeRepository pedigreeRepository,
    ICaseService caseService,
    IFeedRepository feedRepository,
    ILookupDataService lookups,
    IBatchRepository batchRepository,
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
    public IReadOnlyList<CaseRelationRecord> PagedRelations { get; private set; } = [];

    [BindProperty]
    public DamSireViewModel DamSire { get; set; } = new();

    /// <summary>Legacy txtCaseHerdbook — the case's own herdbook, edited on this tab.</summary>
    [BindProperty]
    public string? CaseHerdbook { get; set; }

    public string? DamError { get; private set; }
    public string? SireError { get; private set; }

    public async Task<IActionResult> OnGetAsync()
    {
        SpolSiteUrl = configuration["SpolSiteUrl"] ?? string.Empty;
        await LoadAsync();
        PopulateDamSireFromDetails();
        ApplyPendingDamSire();
        var caseRecord = await caseService.GetCaseAsync(RbseHelper.ParseToRaw(Rbse));
        CaseHerdbook = caseRecord?.Herdbook;
        return Page();
    }

    /// <summary>Legacy btnDamLookUp_Click.</summary>
    public async Task<IActionResult> OnPostLookUpDamAsync()
    {
        await LoadAsync();
        await LookUpAsync(isDam: true);
        return await FinishLookUpAsync(isDam: true);
    }

    /// <summary>Legacy btnSireLookUp_Click.</summary>
    public async Task<IActionResult> OnPostLookUpSireAsync()
    {
        await LoadAsync();
        await LookUpAsync(isDam: false);
        return await FinishLookUpAsync(isDam: false);
    }

    /// <summary>Legacy btnSave_Click (Dam/Sire section): persists whatever is currently populated.</summary>
    public async Task<IActionResult> OnPostSaveDamSireAsync()
    {
        if (!User.IsInRole("DataEntry"))
            return Forbid();
        await LoadAsync();

        var caseRecord = await caseService.GetCaseAsync(RbseHelper.ParseToRaw(Rbse));

        var command = new AddEditDamSireCommand(
            Rbse: Rbse,
            DamId: DamSire.DamId, DamRbse: NullIfBlank(RbseHelper.Normalize(DamSire.DamRbse)),
            DamEartag: DamSire.DamEartag, DamName: DamSire.DamName, DamHerdbook: DamSire.DamHerdbook,
            DamBirthDay: DamSire.DamBirthDay, DamBirthMonth: DamSire.DamBirthMonth, DamBirthYear: DamSire.DamBirthYear,
            DamRowStamp: FromBase64(DamSire.DamRowStamp),
            SireId: DamSire.SireId, SireRbse: NullIfBlank(RbseHelper.Normalize(DamSire.SireRbse)),
            SireEartag: DamSire.SireEartag, SireName: DamSire.SireName, SireHerdbook: DamSire.SireHerdbook,
            SireBirthDay: DamSire.SireBirthDay, SireBirthMonth: DamSire.SireBirthMonth, SireBirthYear: DamSire.SireBirthYear,
            SireRowStamp: FromBase64(DamSire.SireRowStamp),
            CaseHerdbook: NullIfBlank(CaseHerdbook),
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
            logger.LogError(ex, "AddEditDamSireDetails stored procedure threw an exception");
            TempData["Warning"] = "Unable to save dam and sire details. The record may have changed — reload and try again.";
            return RedirectToPage(new { rbse = Rbse });
        }

        TempData["Success"] = "Dam and sire details saved.";
        return RedirectToPage(new { rbse = Rbse });
    }

    public async Task<IActionResult> OnPostDeleteRelationAsync(int relationId, string rowStampBase64)
    {
        if (!User.IsInRole("DataEntry"))
            return Forbid();
        var rowStamp = Convert.FromBase64String(rowStampBase64);
        var command = new DeleteCaseRelationCommand(relationId, rowStamp);

        using var conn = connectionFactory.CreateConnection();
        conn.Open();
        using var tx = conn.BeginTransaction();
        await relationsRepository.DeleteRelationAsync(command, conn, tx);
        tx.Commit();

        TempData["Success"] = "Relation deleted.";
        return RedirectToPage(new { rbse = Rbse });
    }

    /// <summary>Legacy RemoveDam: disassociates the dam without touching the sire.</summary>
    public async Task<IActionResult> OnPostRemoveDamAsync()
    {
        if (!User.IsInRole("DataEntry"))
            return Forbid();
        await LoadAsync();

        var sire = Details?.Sire;
        var caseRecord = await caseService.GetCaseAsync(RbseHelper.ParseToRaw(Rbse));
        var command = new AddEditDamSireCommand(
            Rbse: Rbse,
            DamId: null, DamRbse: null,
            DamEartag: null, DamName: null, DamHerdbook: null,
            DamBirthDay: null, DamBirthMonth: null, DamBirthYear: null, DamRowStamp: null,
            SireId: sire?.Id, SireRbse: sire?.Rbse,
            SireEartag: sire?.Eartag, SireName: sire?.Name, SireHerdbook: sire?.Herdbook,
            SireBirthDay: sire?.BirthDay, SireBirthMonth: sire?.BirthMonth, SireBirthYear: sire?.BirthYear,
            SireRowStamp: sire?.RowStamp,
            CaseHerdbook: caseRecord?.Herdbook, CaseRowStamp: caseRecord?.PedigreeRowStamp);

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
            logger.LogError(ex, "AddEditDamSireDetails stored procedure threw an exception removing the dam");
            TempData["Warning"] = "Unable to remove the dam. The record may have changed — reload and try again.";
            return RedirectToPage(new { rbse = Rbse });
        }

        TempData["Success"] = "Dam removed from case.";
        return RedirectToPage(new { rbse = Rbse });
    }

    /// <summary>Legacy RemoveSire: disassociates the sire without touching the dam.</summary>
    public async Task<IActionResult> OnPostRemoveSireAsync()
    {
        if (!User.IsInRole("DataEntry"))
            return Forbid();
        await LoadAsync();

        var dam = Details?.Dam;
        var caseRecord = await caseService.GetCaseAsync(RbseHelper.ParseToRaw(Rbse));
        var command = new AddEditDamSireCommand(
            Rbse: Rbse,
            DamId: dam?.Id, DamRbse: dam?.Rbse,
            DamEartag: dam?.Eartag, DamName: dam?.Name, DamHerdbook: dam?.Herdbook,
            DamBirthDay: dam?.BirthDay, DamBirthMonth: dam?.BirthMonth, DamBirthYear: dam?.BirthYear,
            DamRowStamp: dam?.RowStamp,
            SireId: null, SireRbse: null,
            SireEartag: null, SireName: null, SireHerdbook: null,
            SireBirthDay: null, SireBirthMonth: null, SireBirthYear: null, SireRowStamp: null,
            CaseHerdbook: caseRecord?.Herdbook, CaseRowStamp: caseRecord?.PedigreeRowStamp);

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
            logger.LogError(ex, "AddEditDamSireDetails stored procedure threw an exception removing the sire");
            TempData["Warning"] = "Unable to remove the sire. The record may have changed — reload and try again.";
            return RedirectToPage(new { rbse = Rbse });
        }

        TempData["Success"] = "Sire removed from case.";
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
            var alreadyRelation = searchRbse == RbseHelper.Normalize(otherParentRbse)
                || (Details?.Relations.Any(r => RbseHelper.Normalize(r.RelationRbse) == searchRbse) ?? false);
            if (alreadyRelation)
            {
                SetError(isDam, AlreadyARelation);
                return;
            }
        }

        var matches = await relationsRepository.GetDamSireDetailsMatchesAsync(
            NullIfBlank(searchEartag), NullIfBlank(searchName),
            searchRbse.Length > 0 ? searchRbse : null,
            NullIfBlank(searchHerdbook), isDam ? "F" : "M");

        if (matches.Count == 1)
        {
            ApplyMatch(isDam, matches[0]);
        }
        else if (matches.Count > 1 || searchRbse.Length == 0)
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

    private Task<IActionResult> FinishLookUpAsync(bool isDam)
    {
        var shouldOpenPicker = isDam ? DamShouldOpenPicker : SireShouldOpenPicker;
        if (shouldOpenPicker)
        {
            var eartag = isDam ? DamSire.DamSearchEartag : DamSire.SireSearchEartag;
            var name = isDam ? DamSire.DamSearchName : DamSire.SireSearchName;
            var herdbook = isDam ? DamSire.DamSearchHerdbook : DamSire.SireSearchHerdbook;

            return Task.FromResult<IActionResult>(RedirectToPage("/Case/PickSireDam", new
            {
                rbse = Rbse,
                sex = isDam ? "F" : "M",
                eartag,
                name,
                herdbook
            }));
        }

        return Task.FromResult<IActionResult>(Page());
    }

    private void ApplyMatch(bool isDam, DamSireDetailRecord match)
    {
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
        }
    }

    private void SetError(bool isDam, string message)
    {
        if (isDam) DamError = message; else SireError = message;
    }

    /// <summary>Picks up a row chosen or created on PickSireDam and reflects it in the form.</summary>
    private void ApplyPendingDamSire()
    {
        if (TempData[PendingDamSireKeys.Dam] is string damJson)
        {
            var pending = JsonSerializer.Deserialize<PendingDamSire>(damJson);
            if (pending is not null)
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
                DamSire.DamChildCount = pending.ChildCount;
            }
        }

        if (TempData[PendingDamSireKeys.Sire] is string sireJson)
        {
            var pending = JsonSerializer.Deserialize<PendingDamSire>(sireJson);
            if (pending is not null)
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
        }
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

        var sorted = SortRelations(Details?.Relations ?? []);
        TotalRelationsCount = sorted.Count;
        TotalPages = Math.Max(1, (int)Math.Ceiling(TotalRelationsCount / (double)PageSize));
        PageNumber = Math.Clamp(PageNumber, 1, TotalPages);
        PagedRelations = sorted.Skip((PageNumber - 1) * PageSize).Take(PageSize).ToList();

        // GET only: replace whatever default DamSire model binding produced with the
        // persisted values. POST handlers must not call this — it would overwrite the
        // values the user just posted (typed search criteria, edited fields, etc.).
    }

    /// <summary>Populates DamSire from the freshly-loaded Details. GET requests only.</summary>
    private void PopulateDamSireFromDetails()
    {
        var dam  = Details?.Dam;
        var sire = Details?.Sire;

        // Pedigree.ID = 0 is a placeholder row (legacy's own "(New)"/unlinked sentinel — see
        // CaseEntryRelations.aspx.vb LoadDamDetails), not a real saved dam/sire, even if the
        // query returns a row for it. Only a positive ID counts as "a dam/sire is recorded".
        DamSire = new DamSireViewModel
        {
            HasDam = dam is { Id: > 0 },
            DamId = dam?.Id ?? 0,              DamRbse        = dam?.Rbse,
            DamEartag      = dam?.Eartag,       DamName        = dam?.Name,
            DamHerdbook    = dam?.Herdbook,     DamBirthDay    = dam?.BirthDay,
            DamBirthMonth  = dam?.BirthMonth,   DamBirthYear   = dam?.BirthYear,
            DamRowStamp    = ToBase64(dam?.RowStamp),
            DamFate = dam?.Fate, DamFinalResult = dam?.FinalResult, DamChildCount = dam?.ChildCount,
            HasSire = sire is { Id: > 0 },
            SireId = sire?.Id ?? 0,            SireRbse       = sire?.Rbse,
            SireEartag     = sire?.Eartag,      SireName       = sire?.Name,
            SireHerdbook   = sire?.Herdbook,    SireBirthDay   = sire?.BirthDay,
            SireBirthMonth = sire?.BirthMonth,  SireBirthYear  = sire?.BirthYear,
            SireRowStamp   = ToBase64(sire?.RowStamp),
            SireFate = sire?.Fate, SireChildCount = sire?.ChildCount
        };
    }

    /// <summary>Sorts on every legacy grid column (RelationType, RBSE, Sex, Birth Date, Fate, Date Left, Eartag, Sire).</summary>
    private IReadOnlyList<CaseRelationRecord> SortRelations(IReadOnlyList<CaseRelationRecord> relations)
    {
        IEnumerable<CaseRelationRecord> q = relations;
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

    // ── View models ──────────────────────────────────────────────────────────

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
