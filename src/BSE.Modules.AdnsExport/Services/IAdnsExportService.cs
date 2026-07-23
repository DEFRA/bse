using BSE.Modules.AdnsExport.Models;

namespace BSE.Modules.AdnsExport.Services;

public interface IAdnsExportService
{
    // ── Preview generation ─────────────────────────────────────────────────────

    /// <summary>
    /// Generates the GB export preview without touching the database.
    /// Returns the full preview payload for display before the user confirms dispatch.
    /// </summary>
    Task<AdnsExportPreview> PreviewGbExportAsync(string emailReference, int adnsYear, int startAdnsNumber);

    /// <summary>
    /// Generates the Channel Islands export preview (no DB query — user inputs case counts).
    /// </summary>
    AdnsExportPreview PreviewCiExport(string emailReference, int adnsYear, int startAdnsNumber,
        int jerseyCases, int guernseyCases, int isleOfManCases, DateTime confirmationDate);

    /// <summary>Generates the NI export preview from the user-built case list.</summary>
    AdnsExportPreview PreviewNiExport(string emailReference, IReadOnlyList<NiCaseInput> cases);

    // ── Reference helpers ──────────────────────────────────────────────────────

    /// <summary>Returns the last ADNS reference for an area (used to pre-populate the UI form).</summary>
    Task<LastAdnsReferenceRecord?> GetLastReferenceAsync(string area);

    // ── Dispatch ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Dispatches a previously generated export:
    /// <list type="number">
    ///   <item>Opens a DB connection and begins a transaction.</item>
    ///   <item>For each case (if <c>SaveAdnsData</c> is true): calls <c>EditCaseADNS</c> — throws <see cref="Exceptions.AdnsCaseUpdateException"/> on any non-zero return.</item>
    ///   <item>Calls <c>EditLastADNSReference</c>.</item>
    ///   <item>Sends email to Brussels (ToAddress) and to the user's address via <see cref="Email.ISmtpClient"/>.</item>
    ///   <item>Commits on success; rolls back on any failure.</item>
    /// </list>
    /// </summary>
    Task DispatchAsync(DispatchAdnsCommand command);
}
