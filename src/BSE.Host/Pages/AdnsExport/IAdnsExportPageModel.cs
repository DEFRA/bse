using BSE.Modules.AdnsExport.Models;

namespace BSE.Host.Pages.AdnsExport;

/// <summary>What the shared ADNS export partials need from the CI, GB and NI page models.</summary>
public interface IAdnsExportPageModel
{
    string? ErrorMessage { get; }
    AdnsExportPreview? Preview { get; }
    string FromEmailAddress { get; }
    string UserEmailAddress { get; }
    string Message { get; }
}
