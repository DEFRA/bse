using BSE.Host.Authentication;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace BSE.Modules.UserManagement.Tests;

public sealed class AzureAdOidcConfigurationTests
{
    [Fact]
    public void Defaults_MatchExpectedOidcConventions_WhenNotOverridden()
    {
        var config = new AzureAdOidcConfiguration();

        config.Instance.Should().Be("https://login.microsoftonline.com/");
        config.CallbackPath.Should().Be("/signin-oidc");
        config.SignedOutCallbackPath.Should().Be("/signout-callback-oidc");
        config.TenantId.Should().BeEmpty();
        config.ClientId.Should().BeEmpty();
        config.ClientSecret.Should().BeNull();
        config.PublicOrigin.Should().BeNull();
        config.ForceAuthn.Should().BeFalse();
    }

    [Fact]
    public void SectionName_IsAzureAd()
    {
        AzureAdOidcConfiguration.SectionName.Should().Be("AzureAd");
    }

    [Fact]
    public void BindsFromConfigurationSection_WithAllValuesOverridden()
    {
        var settings = new Dictionary<string, string?>
        {
            ["AzureAd:Instance"] = "https://login.microsoftonline.com/",
            ["AzureAd:TenantId"] = "770a2450-0227-4c62-90c7-4e38537f1102",
            ["AzureAd:ClientId"] = "3eeeb482-eea0-4bb5-85a9-9a6fefb27a23",
            ["AzureAd:ClientSecret"] = "super-secret",
            ["AzureAd:CallbackPath"] = "/signin-oidc",
            ["AzureAd:SignedOutCallbackPath"] = "/signout-callback-oidc",
            ["AzureAd:PublicOrigin"] = "https://dev-bse.azure.defra.cloud",
            ["AzureAd:ForceAuthn"] = "true"
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(settings)
            .Build();

        var result = configuration
            .GetSection(AzureAdOidcConfiguration.SectionName)
            .Get<AzureAdOidcConfiguration>();

        result.Should().NotBeNull();
        result!.TenantId.Should().Be("770a2450-0227-4c62-90c7-4e38537f1102");
        result.ClientId.Should().Be("3eeeb482-eea0-4bb5-85a9-9a6fefb27a23");
        result.ClientSecret.Should().Be("super-secret");
        result.PublicOrigin.Should().Be("https://dev-bse.azure.defra.cloud");
        result.ForceAuthn.Should().BeTrue();
    }

    [Fact]
    public void GetSection_ReturnsNull_WhenAzureAdSectionIsAbsent()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        var result = configuration
            .GetSection(AzureAdOidcConfiguration.SectionName)
            .Get<AzureAdOidcConfiguration>();

        result.Should().BeNull();
    }
}
