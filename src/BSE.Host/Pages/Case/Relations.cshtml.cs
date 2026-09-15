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
using Dapper;
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
    public IReadOnlyList<CaseRelationRecord> PagedRelations { get; private set; } = [];

    [BindProperty]
    public DamSireViewModel DamSire { get; set; } = new();

    /// <summary>Legacy txtCaseHerdbook — the case's own herdbook, edited on this tab.</summary>
    [BindProperty]
    public string? CaseHerdbook { get; set; }

    /// <summary>Legacy ddlDamStatus — Case.DamStatus resolved to its luAnimalStatus description.</summary>
    public string? DamStatusDescription { get; private set; }

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
        var caseRbse = RbseHelper.ParseToRaw(Rbse);

        // Legacy PartialDate rule: a day may only be entered alongside a month (year alone,
        // or month+year, are valid approximate dates; day without month is not).
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

        // Legacy locked Eartag/Herdbook/birth date once matched to an existing case RBSE;
        // re-derive them here so a tampered post can't override values that belong to that case.
        if (!string.IsNullOrWhiteSpace(DamSire.DamRbse))
        {
            var linkedDam = (await relationsRepository.GetDamSireDetailsMatchesAsync(
                null, null, RbseHelper.Normalize(DamSire.DamRbse), null, "F")).FirstOrDefault();
            if (linkedDam is not null)
            {
                DamSire.DamEartag = linkedDam.Eartag;
                DamSire.DamHerdbook = linkedDam.Herdbook;
                DamSire.DamBirthDay = linkedDam.BirthDay;
                DamSire.DamBirthMonth = linkedDam.BirthMonth;
                DamSire.DamBirthYear = linkedDam.BirthYear;
            }
        }
        if (!string.IsNullOrWhiteSpace(DamSire.SireRbse))
        {
            var linkedSire = (await relationsRepository.GetDamSireDetailsMatchesAsync(
                null, null, RbseHelper.Normalize(DamSire.SireRbse), null, "M")).FirstOrDefault();
            if (linkedSire is not null)
            {
                DamSire.SireEartag = linkedSire.Eartag;
                DamSire.SireHerdbook = linkedSire.Herdbook;
                DamSire.SireBirthDay = linkedSire.BirthDay;
                DamSire.SireBirthMonth = linkedSire.BirthMonth;
                DamSire.SireBirthYear = linkedSire.BirthYear;
            }
        }

        var caseRecord = await caseService.GetCaseAsync(caseRbse);

        // DamId/SireId of 0 means "no real dam/sire recorded" (view-model default); the SP
        // treats any non-null id as real and would insert a blank phantom pedigree row for it.
        var command = new AddEditDamSireCommand(
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

    public async Task<IActionResult> OnPostSaveCaseHerdbookAsync()
    {
        if (!User.IsInRole("DataEntry"))
            return Forbid();

        await LoadAsync();
        var caseRbse = RbseHelper.ParseToRaw(Rbse);
        var caseRecord = await caseService.GetCaseAsync(caseRbse);

        var dam = Details?.Dam is { Id: > 0 } d ? d : null;
        var sire = Details?.Sire is { Id: > 0 } s ? s : null;

        var command = new AddEditDamSireCommand(
            Rbse: caseRbse,
            DamId: dam?.Id, DamRbse: dam?.Rbse,
            DamEartag: dam?.Eartag, DamName: dam?.Name, DamHerdbook: dam?.Herdbook,
            DamBirthDay: dam?.BirthDay, DamBirthMonth: dam?.BirthMonth, DamBirthYear: dam?.BirthYear,
            DamRowStamp: dam?.RowStamp,
            SireId: sire?.Id, SireRbse: sire?.Rbse,
            SireEartag: sire?.Eartag, SireName: sire?.Name, SireHerdbook: sire?.Herdbook,
            SireBirthDay: sire?.BirthDay, SireBirthMonth: sire?.BirthMonth, SireBirthYear: sire?.BirthYear,
            SireRowStamp: sire?.RowStamp,
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
            await pedigreeRepository.AddEditDamSireAsync(command, conn, tx);
            tx.Commit();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to update case herdbook from Relations page");
            TempData["Warning"] = "Unable to update case herdbook. Please reload and try again.";
            return RedirectToPage(new { rbse = Rbse, editCaseHerdbook = true });
        }

        TempData["Success"] = "Case herdbook updated.";
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
        var caseRbse = RbseHelper.ParseToRaw(Rbse);

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
            var rowsAffected = await conn.ExecuteAsync(
                @"UPDATE [Pedigree]
                  SET [DamID] = NULL
                  WHERE REPLACE(LTRIM(RTRIM([RBSE])), '/', '') = @RBSE;",
                new { RBSE = RbseHelper.Normalize(caseRbse) },
                transaction: tx);
            if (rowsAffected <= 0)
            {
                tx.Rollback();
                TempData["Warning"] = "Unable to remove the dam from this case.";
                return RedirectToPage(new { rbse = Rbse });
            }
            tx.Commit();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to remove dam details from case pedigree links");
            TempData["Warning"] = "Unable to remove the dam. The record may have changed — reload and try again.";
            return RedirectToPage(new { rbse = Rbse });
        }

        var refreshed = await relationsRepository.GetRelationsDetailsByRbseAsync(caseRbse);
        if (refreshed.Dam is { Id: > 0 })
        {
            TempData["Warning"] = "Dam details could not be fully removed. Please reload and try again.";
            return RedirectToPage(new { rbse = Rbse });
        }

        TempData["Success"] = "Dam removed from case.";
        TempData.Remove(PendingDamSireKeys.Dam);
        return RedirectToPage(new { rbse = Rbse });
    }

    /// <summary>Legacy RemoveSire: disassociates the sire without touching the dam.</summary>
    public async Task<IActionResult> OnPostRemoveSireAsync()
    {
        if (!User.IsInRole("DataEntry"))
            return Forbid();
        var caseRbse = RbseHelper.ParseToRaw(Rbse);

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
            var rowsAffected = await conn.ExecuteAsync(
                @"UPDATE [Pedigree]
                  SET [SireID] = NULL
                  WHERE REPLACE(LTRIM(RTRIM([RBSE])), '/', '') = @RBSE;",
                new { RBSE = RbseHelper.Normalize(caseRbse) },
                transaction: tx);
            if (rowsAffected <= 0)
            {
                tx.Rollback();
                TempData["Warning"] = "Unable to remove the sire from this case.";
                return RedirectToPage(new { rbse = Rbse });
            }
            tx.Commit();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to remove sire details from case pedigree links");
            TempData["Warning"] = "Unable to remove the sire. The record may have changed — reload and try again.";
            return RedirectToPage(new { rbse = Rbse });
        }

        var refreshed = await relationsRepository.GetRelationsDetailsByRbseAsync(caseRbse);
        if (refreshed.Sire is { Id: > 0 })
        {
            TempData["Warning"] = "Sire details could not be fully removed. Please reload and try again.";
            return RedirectToPage(new { rbse = Rbse });
        }

        TempData["Success"] = "Sire removed from case.";
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

    private async Task<IActionResult> LoadRelationsPageAsync(bool editCaseHerdbook)
    {
        SpolSiteUrl = configuration["SpolSiteUrl"] ?? string.Empty;
        await LoadAsync();
        PopulateDamSireFromDetails();
        EditCaseHerdbook = editCaseHerdbook;

        TempData.Remove(PendingDamSireKeys.Dam);
        TempData.Remove(PendingDamSireKeys.Sire);

        var caseRecord = await caseService.GetCaseAsync(RbseHelper.ParseToRaw(Rbse));
        CaseHerdbook = caseRecord?.Herdbook;

        // Legacy ddlDamStatus — Case.DamStatus, distinct from the dam's own case Fate.
        if (!string.IsNullOrWhiteSpace(caseRecord?.DamStatus))
        {
            var statuses = await lookups.GetAnimalStatusesAsync();
            DamStatusDescription = statuses.FirstOrDefault(s => s.Code == caseRecord.DamStatus)?.Description
                ?? caseRecord.DamStatus;
        }

        return Page();
    }

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
