using BSE.Modules.BsessIntegration.Configuration;
using BSE.Modules.BsessIntegration.Services;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace BSE.Modules.BsessIntegration.Tests;

public sealed class BsessEtlServiceValidationTests
{
    private static BsessEtlService CreateService(string? sourceConnectionString = "Server=source;Database=source;", string? targetConnectionString = "Server=target;Database=target;")
    {
        var options = Options.Create(new BsessEtlOptions
        {
            SourceConnectionString = sourceConnectionString ?? string.Empty
        });

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:BSE"] = targetConnectionString
            })
            .Build();

        return new BsessEtlService(options, configuration, Substitute.For<ILogger<BsessEtlService>>());
    }

    [Fact]
    public void Constructor_WhenTargetConnectionMissing_ThrowsConfigurationError()
    {
        var options = Options.Create(new BsessEtlOptions { SourceConnectionString = "Server=source;Database=source;" });
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>()).Build();

        var act = () => new BsessEtlService(options, configuration, Substitute.For<ILogger<BsessEtlService>>());

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*ConnectionStrings:BSE*");
    }

    [Fact]
    public async Task ImportAsync_WhenSourceConnectionMissing_ThrowsValidationError()
    {
        var service = CreateService(sourceConnectionString: string.Empty);

        var act = () => service.ImportAsync(TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*SourceConnectionString*");
    }
}
