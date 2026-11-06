using BSE.Host.Helpers;
using BSE.Host.ModelBinding;
using BSE.Modules.AnimalRelations.Repositories;
using BSE.Modules.ReferenceData.Models;
using BSE.Modules.ReferenceData.Services;
using BSE.SharedKernel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BSE.Host.Pages.Case;

/// <summary>State and helpers shared by the relation add and edit pages.</summary>
public abstract class RelationPageModelBase(
    IAnimalRelationsRepository relationsRepository,
    ILookupDataService lookups) : PageModel
{
    protected IAnimalRelationsRepository RelationsRepository { get; } = relationsRepository;

    [BindProperty(SupportsGet = true)] public string Rbse { get; set; } = string.Empty;

    [BindProperty] public string? RelationType { get; set; }
    [BindProperty] public string? RelationRbse { get; set; }
    [BindProperty] public string? EartagCountry { get; set; }
    [BindProperty] public string? EartagHerdmark { get; set; }
    [BindProperty] public string? Eartag { get; set; }
    [BindProperty] public string? Sex { get; set; }
    [BindProperty] public int? BirthDay { get; set; }
    [BindProperty] public int? BirthMonth { get; set; }
    [BindProperty] public int? BirthYear { get; set; }
    [BindProperty, ModelBinder(BinderType = typeof(DatePickerModelBinder))] public DateTime? LeftDate { get; set; }
    [BindProperty] public string? RelationFate { get; set; }
    [BindProperty] public string? Sire { get; set; }

    public IEnumerable<LookupItem> RelationTypes { get; private set; } = [];
    public IEnumerable<LuRelationFate> RelationFates { get; private set; } = [];
    public IEnumerable<LuSex> Sexes { get; private set; } = [];

    public IDictionary<string, string> FieldErrors { get; private set; } = new Dictionary<string, string>();
    public string? ErrorMessage { get; protected set; }

    protected async Task LoadLookupsAsync()
    {
        var rtTask = lookups.GetLookupAsync(LookupTableId.RelationType);
        var rfTask = lookups.GetLookupAsync(LookupTableId.RelationFate);
        var sxTask = lookups.GetSexesAsync();
        await Task.WhenAll(rtTask, rfTask, sxTask);

        RelationTypes = await rtTask;
        RelationFates = (await rfTask).Select(x => new LuRelationFate { Id = x.Id, Code = x.Code, Description = x.Description });
        Sexes = await sxTask;
    }

    /// <summary>
    /// Validates the posted relation and, when valid, overwrites the related-case fields from that case.
    /// Returns false (with <see cref="FieldErrors"/> copied to ModelState) when the page should be redisplayed.
    /// </summary>
    protected async Task<bool> ValidateAndApplyRelatedCaseAsync(
        string caseRbse,
        IEnumerable<string?> existingRelationRbses,
        string? damRbse,
        string? sireRbse)
    {
        // Mirrors legacy RBSE.ascx auto-padding on postback: redisplay the short form
        // entered (e.g. "16/01") as the full zero-padded value.
        RelationRbse = RbseHelper.Format(RbseHelper.ParseToRaw(RelationRbse));
        ModelState.Remove(nameof(RelationRbse));

        FieldErrors = RelationValidation.Validate(
            new RelationValidation.Input(
                caseRbse, RelationRbse, RelationType, Sex,
                EartagCountry, EartagHerdmark, Eartag,
                BirthDay, BirthMonth, BirthYear, LeftDate),
            existingRelationRbses,
            damRbse,
            sireRbse);

        if (FieldErrors.Count == 0)
        {
            // Legacy ctlRelationRBSE_RBSEChanged: once a relation RBSE is supplied, Sex, Fate,
            // Eartag, birth date, left date and Sire are always taken live from that case —
            // the corresponding form fields are disabled there and must not be trusted here.
            var normalizedRbse = RbseHelper.ParseToRaw(RelationRbse);
            if (normalizedRbse.Length > 0)
            {
                var related = await RelationsRepository.GetRelationDetailsOfRelatedCaseAsync(normalizedRbse);
                if (related is null)
                {
                    FieldErrors = new Dictionary<string, string> { ["RelationRbse"] = RelationValidation.RbseNotFound };
                }
                else
                {
                    Sex = related.Sex;
                    RelationFate = related.Fate;
                    EartagCountry = related.EartagCountry;
                    EartagHerdmark = related.EartagHerdmark;
                    Eartag = related.Eartag;
                    BirthDay = related.BirthDay;
                    BirthMonth = related.BirthMonth;
                    BirthYear = related.BirthYear;
                    LeftDate = DateTime.TryParse(related.LeftDate, System.Globalization.CultureInfo.InvariantCulture, out var leftDate) ? leftDate : null;
                    Sire = related.Name;
                }
            }
        }

        if (FieldErrors.Count == 0)
            return true;

        foreach (var error in FieldErrors)
        {
            ModelState.AddModelError(error.Key, error.Value);
        }

        return false;
    }

    protected static string? NullIfBlank(string? s) => string.IsNullOrWhiteSpace(s) ? null : s;
    protected static byte? ToByte(int? v) => v is > 0 and <= 255 ? (byte)v.Value : null;
    protected static short? ToShort(int? v) => v.HasValue ? (short?)v.Value : null;
}
