namespace BSE.Host.Pages.Case;

/// <summary>What the shared herd size form partial needs from the herd size add and edit pages.</summary>
public interface IHerdSizeFormPage
{
    string Rbse { get; }
    FarmModel.HerdSizeFormViewModel HerdSize { get; }
}
