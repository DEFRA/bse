using BSE.Modules.BsessIntegration.Configuration;
using BSE.Modules.BsessIntegration.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Retry;

namespace BSE.Modules.BsessIntegration.Jobs;

/// <summary>
/// Scheduled background job that drives <see cref="IBsessEtlService.ImportAsync"/> on a
/// configurable interval, replacing the legacy SSIS scheduled package execution.
/// Each cycle applies an exponential back-off retry policy before logging a permanent failure.
/// </summary>
public sealed class BsessImportJob : BackgroundService
{
    private readonly IBsessEtlService _etlService;
    private readonly BsessEtlOptions _options;
    private readonly ILogger<BsessImportJob> _logger;

    public BsessImportJob(
        IBsessEtlService etlService,
        IOptions<BsessEtlOptions> options,
        ILogger<BsessImportJob> logger)
    {
        _etlService = etlService;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        AsyncRetryPolicy retryPolicy = Policy
            .Handle<Exception>(ex => ex is not OperationCanceledException)
            .WaitAndRetryAsync(
                retryCount: _options.MaxRetryAttempts,
                sleepDurationProvider: attempt =>
                    TimeSpan.FromSeconds(_options.RetryDelaySeconds * Math.Pow(2, attempt - 1)),
                onRetry: (ex, delay, attempt, _) =>
                    _logger.LogWarning(
                        ex,
                        "BSESS import attempt {Attempt} failed, retrying in {DelaySeconds:F1}s",
                        attempt, delay.TotalSeconds));

        // Run immediately at startup, then on the configured interval.
        await RunCycleAsync(retryPolicy, stoppingToken);

        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(_options.ImportIntervalMinutes));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await RunCycleAsync(retryPolicy, stoppingToken);
        }
    }

    private async Task RunCycleAsync(AsyncRetryPolicy retryPolicy, CancellationToken stoppingToken)
    {
        try
        {
            await retryPolicy.ExecuteAsync(ct => _etlService.ImportAsync(ct), stoppingToken);
        }
        catch (OperationCanceledException)
        {
            // Graceful shutdown — do not log as error.
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "BSESS import failed after {MaxRetries} retries — will retry at next scheduled interval",
                _options.MaxRetryAttempts);
        }
    }
}
