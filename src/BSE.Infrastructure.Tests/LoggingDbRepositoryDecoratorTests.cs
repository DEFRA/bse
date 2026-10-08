using System.Data;
using BSE.Infrastructure;
using Dapper;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace BSE.Infrastructure.Tests;

public sealed class LoggingDbRepositoryDecoratorTests
{
    private static readonly object ExampleParam = new { Id = 42 };

    [Fact]
    public async Task QueryAsync_WhenBelowThreshold_LogsInformationAndReturnsResults()
    {
        var inner = Substitute.For<IDbRepository>();
        var logger = new TestLogger<LoggingDbRepositoryDecorator>();
        var threshold = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Logging:DbSlowQueryThresholdMs"] = "1000"
            })
            .Build();

        var expected = new[] { "alpha", "beta" };
        inner.QueryAsync<string>("GetThing", ExampleParam).Returns(expected);

        var sut = new LoggingDbRepositoryDecorator(inner, logger, threshold);

        var result = await sut.QueryAsync<string>("GetThing", ExampleParam);

        result.Should().BeEquivalentTo(expected);
        await inner.Received(1).QueryAsync<string>("GetThing", ExampleParam);
        logger.Entries.Should().Contain(x => x.LogLevel == LogLevel.Information && x.Message.Contains("GetThing") && x.Message.Contains("Query"));
    }

    [Fact]
    public async Task QueryAsync_WhenThresholdIsZero_LogsWarningForSlowOperation()
    {
        var inner = Substitute.For<IDbRepository>();
        var logger = new TestLogger<LoggingDbRepositoryDecorator>();
        var threshold = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Logging:DbSlowQueryThresholdMs"] = "0"
            })
            .Build();

        inner.QueryAsync<string>("GetThing", ExampleParam).Returns(new[] { "alpha" });

        var sut = new LoggingDbRepositoryDecorator(inner, logger, threshold);

        var result = await sut.QueryAsync<string>("GetThing", ExampleParam);

        result.Should().ContainSingle().Which.Should().Be("alpha");
        logger.Entries.Should().Contain(x => x.LogLevel == LogLevel.Warning && x.Message.Contains("Slow database"));
    }

    [Fact]
    public async Task QueryAsync_WithTimeout_UsesTimeoutAndLogsSuccess()
    {
        var inner = Substitute.For<IDbRepository>();
        var logger = new TestLogger<LoggingDbRepositoryDecorator>();
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Logging:DbSlowQueryThresholdMs"] = "500"
        }).Build();

        inner.QueryAsync<string>("GetThing", ExampleParam, 15).Returns(new[] { "gamma" });

        var sut = new LoggingDbRepositoryDecorator(inner, logger, config);

        var result = await sut.QueryAsync<string>("GetThing", ExampleParam, 15);

        result.Should().ContainSingle().Which.Should().Be("gamma");
        await inner.Received(1).QueryAsync<string>("GetThing", ExampleParam, 15);
        logger.Entries.Should().Contain(x => x.LogLevel == LogLevel.Information && x.Message.Contains("Timeout=15"));
    }

    [Fact]
    public async Task ExecuteWithOutputAsync_WhenInnerThrows_LogsErrorAndRethrows()
    {
        var inner = Substitute.For<IDbRepository>();
        var logger = new TestLogger<LoggingDbRepositoryDecorator>();
        var config = BuildConfiguration(1000);
        var ex = new InvalidOperationException("database down");
        var param = new DynamicParameters();

        inner.ExecuteWithOutputAsync("SaveThing", param).Returns(_ => throw ex);

        var sut = new LoggingDbRepositoryDecorator(inner, logger, config);

        var act = () => sut.ExecuteWithOutputAsync("SaveThing", param);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("database down");
        logger.Entries.Should().Contain(x => x.LogLevel == LogLevel.Error && x.Message.Contains("SaveThing") && x.Message.Contains("ExecuteWithOutput"));
    }

    [Fact]
    public async Task ExecuteAsync_InTransaction_DelegatesAndLogsInformation()
    {
        var inner = Substitute.For<IDbRepository>();
        var logger = new TestLogger<LoggingDbRepositoryDecorator>();
        var config = BuildConfiguration(1000);
        var connection = Substitute.For<IDbConnection>();
        var transaction = Substitute.For<IDbTransaction>();

        inner.ExecuteAsync("UpdateThing", ExampleParam, connection, transaction).Returns(Task.CompletedTask);

        var sut = new LoggingDbRepositoryDecorator(inner, logger, config);

        await sut.ExecuteAsync("UpdateThing", ExampleParam, connection, transaction);

        await inner.Received(1).ExecuteAsync("UpdateThing", ExampleParam, connection, transaction);
        logger.Entries.Should().Contain(x => x.LogLevel == LogLevel.Information && x.Message.Contains("UpdateThing") && x.Message.Contains("ExecuteInTransaction"));
    }

    [Fact]
    public async Task ExecuteWithRowCountAsync_InTransaction_ReturnsRowCountAndLogsSuccess()
    {
        var inner = Substitute.For<IDbRepository>();
        var logger = new TestLogger<LoggingDbRepositoryDecorator>();
        var config = BuildConfiguration(1000);
        var connection = Substitute.For<IDbConnection>();
        var transaction = Substitute.For<IDbTransaction>();

        inner.ExecuteWithRowCountAsync("DeleteThing", ExampleParam, connection, transaction).Returns(3);

        var sut = new LoggingDbRepositoryDecorator(inner, logger, config);

        var result = await sut.ExecuteWithRowCountAsync("DeleteThing", ExampleParam, connection, transaction);

        result.Should().Be(3);
        await inner.Received(1).ExecuteWithRowCountAsync("DeleteThing", ExampleParam, connection, transaction);
        logger.Entries.Should().Contain(x => x.LogLevel == LogLevel.Information && x.Message.Contains("DeleteThing") && x.Message.Contains("ExecuteWithRowCountInTransaction"));
    }

    [Fact]
    public async Task QueryMultipleAsync_WhenSuccessful_ReturnsResultAndLogsSuccess()
    {
        var inner = Substitute.For<IDbRepository>();
        var logger = new TestLogger<LoggingDbRepositoryDecorator>();
        var config = BuildConfiguration(1000);
        var expected = new { Id = 7, Name = "x" };

        inner.QueryMultipleAsync(
            "GetBundle",
            ExampleParam,
            Arg.Any<Func<SqlMapper.GridReader, Task<object>>>())
            .Returns(Task.FromResult<object>(expected));

        var sut = new LoggingDbRepositoryDecorator(inner, logger, config);

        var result = await sut.QueryMultipleAsync(
            "GetBundle",
            ExampleParam,
            _ => Task.FromResult<object>(expected));

        result.Should().BeEquivalentTo(expected);
        await inner.Received(1).QueryMultipleAsync(
            "GetBundle",
            ExampleParam,
            Arg.Any<Func<SqlMapper.GridReader, Task<object>>>());
        logger.Entries.Should().Contain(x => x.LogLevel == LogLevel.Information && x.Message.Contains("GetBundle") && x.Message.Contains("QueryMultiple"));
    }

    private static IConfiguration BuildConfiguration(int thresholdMs)
        => new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Logging:DbSlowQueryThresholdMs"] = thresholdMs.ToString()
            })
            .Build();

    private sealed class TestLogger<T> : ILogger<T>
    {
        public List<(LogLevel LogLevel, string Message)> Entries { get; } = [];

        public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            => Entries.Add((logLevel, formatter(state, exception)));

        private sealed class NullScope : IDisposable
        {
            public static readonly NullScope Instance = new();

            public void Dispose()
            {
            }
        }
    }
}
