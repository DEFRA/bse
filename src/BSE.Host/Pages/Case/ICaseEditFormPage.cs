using BSE.Host.Models.ViewModels;
using BSE.SharedKernel;

namespace BSE.Host.Pages.Case;

/// <summary>What the shared case edit form partials need from the DEFRA tab page models.</summary>
public interface ICaseEditFormPage
{
    CaseEditViewModel Case { get; }
    IEnumerable<ILookupItem> SurveyOptions { get; }
    IEnumerable<ILookupItem> ValuationAgeOptions { get; }
    IEnumerable<ILookupItem> CaseTypeOptions { get; }
}
