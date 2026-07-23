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
}
