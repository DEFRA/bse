using BSE.Infrastructure;
using BSE.Modules.AdnsExport.Configuration;
using BSE.Modules.AdnsExport.Email;
using BSE.Modules.AdnsExport.Exceptions;
using BSE.Modules.AdnsExport.Models;
using BSE.Modules.AdnsExport.Repositories;
using Microsoft.Extensions.Options;

namespace BSE.Modules.AdnsExport.Services;

/// <summary>
/// Stateless ADNS export service. Replaces the legacy <c>clsADNSReport</c> stateful class.
/// Intermediate state (preview payload) is held by the caller (UI layer / distributed cache)
/// rather than in a long-lived object, enabling horizontal scaling.
/// </summary>
public sealed class AdnsExportService : IAdnsExportService
{
    private readonly IAdnsRepository _repository;
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ISmtpClient _smtpClient;
    private readonly AdnsSmtpOptions _smtpOptions;

    public AdnsExportService(
        IAdnsRepository repository,
        IDbConnectionFactory connectionFactory,
        ISmtpClient smtpClient,
        IOptions<AdnsSmtpOptions> smtpOptions)
    {
        _repository = repository;
        _connectionFactory = connectionFactory;
        _smtpClient = smtpClient;
        _smtpOptions = smtpOptions.Value;
    }

    // ── Preview generation ─────────────────────────────────────────────────────

    public async Task<AdnsExportPreview> PreviewGbExportAsync(
        string emailReference, int adnsYear, int startAdnsNumber)
    {
        var (cases, missing) = await _repository.GetCasesForGbAsync(adnsYear, startAdnsNumber);
        return BuildPreview("GB", emailReference, cases, missing);
    }

    public AdnsExportPreview PreviewCiExport(
        string emailReference, int adnsYear, int startAdnsNumber,
        int jerseyCases, int guernseyCases, int isleOfManCases, DateTime confirmationDate)
    {
        var cases = BuildCiCases(adnsYear, startAdnsNumber, jerseyCases, guernseyCases, isleOfManCases, confirmationDate);
        return BuildPreview("CI", emailReference, cases, Array.Empty<MissingAdnsCaseRecord>());
    }

    public AdnsExportPreview PreviewNiExport(string emailReference, IReadOnlyList<NiCaseInput> niCases)
    {
        var cases = niCases.Select((c, i) => new AdnsCaseRecord
        {
            Id = i + 1,
            Rbse = c.Rbse,
            AdnsYear = c.AdnsYear,
            AdnsNumber = c.AdnsNumber,
            AdnsRegionId = c.AdnsRegionId,
            AdnsRegionName = c.AdnsRegionName,
            ConfirmationDate = c.ConfirmationDate,
            AdnsReference = $"{c.AdnsYear}/{c.AdnsNumber:00000}"
        }).ToList();
        return BuildPreview("NI", emailReference, cases, Array.Empty<MissingAdnsCaseRecord>());
    }

    // ── Reference helpers ──────────────────────────────────────────────────────

    public Task<LastAdnsReferenceRecord?> GetLastReferenceAsync(string area)
        => _repository.GetLastReferenceByAreaAsync(area);

    // ── Dispatch ───────────────────────────────────────────────────────────────

    public async Task DispatchAsync(DispatchAdnsCommand command)
    {
        var sentDate = DateTime.UtcNow;
        var subject = BuildSubject(command.EmailReference);
        var body = AdnsEmailBodyBuilder.Build(command.Cases);
        var endReference = command.Cases.OrderByDescending(c => c.AdnsNumber).FirstOrDefault()?.AdnsReference;

        using var connection = _connectionFactory.CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction();

        try
        {
            if (command.SaveAdnsData)
            {
                foreach (var c in command.Cases)
                {
                    var returnCode = await _repository.EditCaseAdnsAsync(
                        c.Rbse, sentDate, c.AdnsRegionId,
                        (short)c.AdnsYear, c.AdnsNumber, c.RowStamp!,
                        connection, transaction);

                    if (returnCode != 0)
                    {
                        throw new AdnsCaseUpdateException(c.Rbse, returnCode,
                            returnCode switch
                            {
                                1 => $"Case {c.Rbse}: details changed by another user (concurrency conflict).",
                                2 => $"Case {c.Rbse}: already reported to Brussels.",
                                3 => $"Case {c.Rbse}: ADNS region changed since export was generated.",
                                4 => $"Case {c.Rbse}: ADNS reference already assigned by another user.",
                                5 => $"Case {c.Rbse}: database update failed.",
                                _ => $"Case {c.Rbse}: unexpected error (code {returnCode})."
                            });
                    }
                }
            }

            // Update the last reference tracker
            if (endReference is not null && endReference.Length >= 5)
            {
                var year = (short)int.Parse(endReference[..4]);
                var number = int.Parse(endReference[5..].TrimStart('0').PadLeft(1, '0'));
                await _repository.EditLastAdnsReferenceAsync(
                    command.Area, command.EmailReference, year, number,
                    connection, transaction);
            }

            // Send emails — outside the transaction so a SMTP failure doesn't prevent commit,
            // but before commit so we don't commit without knowing email dispatch succeeded.
            await _smtpClient.SendAsync(_smtpOptions.FromAddress, command.UserEmailAddress, subject, body);
            await _smtpClient.SendAsync(_smtpOptions.FromAddress, _smtpOptions.ToAddress, subject, body);

            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    // ── Private helpers ────────────────────────────────────────────────────────

    private static AdnsExportPreview BuildPreview(
        string area,
        string emailReference,
        IReadOnlyList<AdnsCaseRecord> cases,
        IReadOnlyList<MissingAdnsCaseRecord> missing)
    {
        _ = area; // area is used only for documentation / future routing
        var subject = BuildSubject(emailReference);
        var body = cases.Count > 0 ? AdnsEmailBodyBuilder.Build(cases) : string.Empty;
        var summary = BuildSummary(cases);
        var start = cases.Count > 0 ? cases.Min(c => c.AdnsReference) : null;
        var end = cases.Count > 0 ? cases.Max(c => c.AdnsReference) : null;
        return new AdnsExportPreview(cases, missing, summary, subject, body, start, end);
    }

    private static string BuildSubject(string emailReference)
        => $"t=DATA;r={emailReference}";

    private static IReadOnlyList<AdnsRegionSummaryRecord> BuildSummary(IReadOnlyList<AdnsCaseRecord> cases)
        => cases
            .GroupBy(c => new { c.AdnsRegionId, c.AdnsRegionName })
            .Select(g => new AdnsRegionSummaryRecord(g.Key.AdnsRegionId, g.Key.AdnsRegionName ?? string.Empty, g.Count()))
            .ToList();

    private static IReadOnlyList<AdnsCaseRecord> BuildCiCases(
        int adnsYear, int startAdnsNumber,
        int jerseyCases, int guernseyCases, int isleOfManCases,
        DateTime confirmationDate)
    {
        var cases = new List<AdnsCaseRecord>();
        var counter = 1;

        void AddBlock(int count, int regionId, string regionName)
        {
            for (var i = 0; i < count; i++, counter++)
            {
                var number = startAdnsNumber + counter - 1;
                cases.Add(new AdnsCaseRecord
                {
                    Id = counter,
                    AdnsYear = adnsYear,
                    AdnsNumber = number,
                    AdnsRegionId = regionId,
                    AdnsRegionName = regionName,
                    ConfirmationDate = confirmationDate,
                    AdnsReference = $"{adnsYear}/{number:00000}"
                });
            }
        }

        AddBlock(jerseyCases, regionId: 6200, regionName: "Jersey");
        AddBlock(guernseyCases, regionId: 6100, regionName: "Guernsey");
        AddBlock(isleOfManCases, regionId: 6300, regionName: "Isle of Man");

        return cases;
    }
}
