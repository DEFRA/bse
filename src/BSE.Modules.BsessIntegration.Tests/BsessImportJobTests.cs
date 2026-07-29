using BSE.Modules.BsessIntegration.Configuration;
using BSE.Modules.BsessIntegration.Jobs;
using BSE.Modules.BsessIntegration.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BSE.Modules.BsessIntegration.Tests;

public class BsessImportJobTests
{
    private static BsessImportJob CreateJob(
        IBsessEtlService etlService,
        int intervalMinutes = 10_000,
        int maxRetryAttempts = 1,
        int retryDelaySeconds = 0)
    {
        var options = Options.Create(new BsessEtlOptions
        {
            ImportIntervalMinutes = intervalMinutes,
            MaxRetryAttempts = maxRetryAttempts,
            RetryDelaySeconds = retryDelaySeconds
        });
        var logger = Substitute.For<ILogger<BsessImportJob>>();
        return new BsessImportJob(etlService, options, logger);
    }

    [Fact]
    public async Task ExecuteAsync_CallsImportAsyncOnStartup()
    {
        var etlService = Substitute.For<IBsessEtlService>();
        etlService.ImportAsync(Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);

        var job = CreateJob(etlService);
        using var cts = new CancellationTokenSource();

        await job.StartAsync(cts.Token);
        await Task.Delay(200); // allow initial import cycle to complete
        await job.StopAsync(CancellationToken.None);

        await etlService.Received().ImportAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task StopAsync_WhenCancelled_DoesNotThrow()
    {
        var etlService = Substitute.For<IBsessEtlService>();
        etlService.ImportAsync(Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);

        var job = CreateJob(etlService);
        using var cts = new CancellationTokenSource();

        await job.StartAsync(cts.Token);
        cts.Cancel();

        var act = () => job.StopAsync(CancellationToken.None);
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task ExecuteAsync_WhenImportThrows_LogsErrorAndContinues()
    {
        var callCount = 0;
        var etlService = Substitute.For<IBsessEtlService>();
        etlService
            .ImportAsync(Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                callCount++;
                throw new InvalidOperationException("Source unavailable");
            });

        var job = CreateJob(etlService, maxRetryAttempts: 1, retryDelaySeconds: 0);
        using var cts = new CancellationTokenSource();

        await job.StartAsync(cts.Token);
        await Task.Delay(200);
        await job.StopAsync(CancellationToken.None);

        // Job should have survived the exception and not re-thrown it.
        callCount.Should().BeGreaterThan(0);
    }
}
