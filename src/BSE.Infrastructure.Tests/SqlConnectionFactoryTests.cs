using BSE.Infrastructure;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace BSE.Infrastructure.Tests;

public sealed class SqlConnectionFactoryTests
{
    [Fact]
    public void SqlConnectionFactory_CanBeInstantiated_WithValidConfiguration()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:BSE"] =
                    "Server=localhost;Database=BSESystem;Trusted_Connection=True;TrustServerCertificate=True;"
            })
            .Build();

        var factory = new SqlConnectionFactory(configuration);

        factory.Should().NotBeNull();
    }

    [Fact]
    public void SqlConnectionFactory_Throws_WhenConfigurationIsMissing()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        var act = () => new SqlConnectionFactory(configuration);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*ConnectionStrings:BSE is not configured*");
    }

    [Fact]
    public void CreateConnection_ReturnsConfiguredSqlConnection()
    {
        const string connectionString = "Server=localhost;Database=BSESystem;Trusted_Connection=True;TrustServerCertificate=True;";
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:BSE"] = connectionString
            })
            .Build();

        var factory = new SqlConnectionFactory(configuration);

        using var connection = factory.CreateConnection();

        connection.Should().NotBeNull();
        connection.ConnectionString.Should().Be(connectionString);
    }
}
