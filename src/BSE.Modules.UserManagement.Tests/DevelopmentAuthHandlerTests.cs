using System.Security.Claims;
using System.Text.Encodings.Web;
using BSE.Host.Authentication;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace BSE.Modules.UserManagement.Tests;

public sealed class DevelopmentAuthHandlerTests
{
    private static async Task<AuthenticateResult> RunAsync(DevelopmentAuthOptions options)
    {
        var monitor = Substitute.For<IOptionsMonitor<DevelopmentAuthOptions>>();
        monitor.Get(DevelopmentAuthHandler.SchemeName).Returns(options);
        monitor.CurrentValue.Returns(options);

        var handler = new DevelopmentAuthHandler(monitor, NullLoggerFactory.Instance, UrlEncoder.Default);
        await handler.InitializeAsync(
            new AuthenticationScheme(DevelopmentAuthHandler.SchemeName, null, typeof(DevelopmentAuthHandler)),
            new DefaultHttpContext());
        return await handler.AuthenticateAsync();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Authenticate_EmitsConfiguredEmailClaims(bool useWindowsIdentity)
    {
        var result = await RunAsync(new DevelopmentAuthOptions { UseWindowsIdentity = useWindowsIdentity, Email = "dev@example.test" });

        result.Succeeded.Should().BeTrue();
        var principal = result.Principal!;
        principal.FindFirstValue("preferred_username").Should().Be("dev@example.test");
        principal.FindFirstValue(ClaimTypes.Email).Should().Be("dev@example.test");
        principal.FindFirstValue(ClaimTypes.NameIdentifier).Should().Be("dev@example.test");
        principal.Identity!.AuthenticationType.Should().Be(DevelopmentAuthHandler.SchemeName);
    }

    [Fact]
    public async Task Authenticate_BlankEmail_StillSucceedsWithEmptyClaims()
    {
        var result = await RunAsync(new DevelopmentAuthOptions { UseWindowsIdentity = false, Email = "" });

        result.Succeeded.Should().BeTrue();
        result.Principal!.FindFirstValue(ClaimTypes.Email).Should().NotBeNull();
    }

    [Fact]
    public async Task Authenticate_BlankEmail_FallsBackToNtLoginDerivedAddress()
    {
        var result = await RunAsync(new DevelopmentAuthOptions { UseWindowsIdentity = false, Email = "", NtLogin = "DS1" });

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public void Options_HaveSensibleDefaults()
    {
        var options = new DevelopmentAuthOptions();

        options.UseWindowsIdentity.Should().BeTrue();
        options.NtLogin.Should().Be("dev-user");
        options.Email.Should().Be("dev-user@defra.gov.uk");
    }
}
