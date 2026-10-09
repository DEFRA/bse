using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace BSE.Host.Pages.Case;

/// <summary>
/// Mirrors legacy CaseEntrySave.aspx's results screen: shown instead of auto-redirecting whenever
/// the save could not fully complete, listing every consolidated message (across all tabs) with a
/// single button whose text/target depends on why the screen is being shown.
/// </summary>
[Authorize]
public class SaveResultModel : PageModel
{
    public const string MessagesKey = "SaveResult_Messages";
    public const string ModeKey = "SaveResult_Mode";

    [BindProperty(SupportsGet = true)]
    public string Rbse { get; set; } = string.Empty;

    public string IntroText { get; private set; } = string.Empty;
    public IReadOnlyList<string> Messages { get; private set; } = [];
    public string ButtonText { get; private set; } = "OK";
    public string ReturnPage { get; private set; } = "/Home";

    /// <summary>Stages the messages/mode for <see cref="SaveResultModel"/> to pick up after a redirect.</summary>
    public static void Stage(ITempDataDictionary tempData, SaveResultMode mode, IReadOnlyList<string> messages)
    {
        tempData[MessagesKey] = JsonSerializer.Serialize(messages);
        tempData[ModeKey] = mode.ToString();
    }

    public IActionResult OnGet()
    {
        var messagesJson = TempData[MessagesKey] as string;
        var modeText = TempData[ModeKey] as string;
        if (string.IsNullOrEmpty(messagesJson)
            || !Enum.TryParse<SaveResultMode>(modeText, out var mode))
        {
            return RedirectToPage("/Home");
        }

        Messages = JsonSerializer.Deserialize<List<string>>(messagesJson) ?? [];

        (IntroText, ButtonText, ReturnPage) = mode switch
        {
            SaveResultMode.MissingMandatoryFields =>
                ("The case is missing the following items of data:", "Return", "/Case/Farm"),
            SaveResultMode.PartialSuccess =>
                ("The database has been updated but some errors were encountered:", "OK", "/Home"),
            _ =>
                ("The database has not been updated because the following error(s) occurred:", "OK", "/Home"),
        };

        return Page();
    }
}

public enum SaveResultMode
{
    MissingMandatoryFields,
    PartialSuccess,
    Failure
}
