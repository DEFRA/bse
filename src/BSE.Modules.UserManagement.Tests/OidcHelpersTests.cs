using System.Security.Claims;
using BSE.Host.Authentication;
using FluentAssertions;
using Xunit;

namespace BSE.Modules.UserManagement.Tests;

public sealed class OidcHelpersTests
{
    // ── ResolveEmailClaim ───────────────────────────────────────────────

    [Fact]
    public void ResolveEmailClaim_PrefersClaimTypesEmail_WhenPresent()
    {
        var identity = new ClaimsIdentity(
        [
            new Claim(ClaimTypes.Email, "email-claim@domain.com"),
            new Claim("preferred_username", "preferred@domain.com"),
            new Claim(ClaimTypes.Upn, "upn@domain.com")
        ]);

        var result = OidcHelpers.ResolveEmailClaim(identity);

        result.Should().Be("email-claim@domain.com");
    }

    [Fact]
    public void ResolveEmailClaim_FallsBackToPreferredUsername_WhenEmailClaimMissing()
    {
        var identity = new ClaimsIdentity(
        [
            new Claim("preferred_username", "preferred@domain.com"),
            new Claim(ClaimTypes.Upn, "upn@domain.com")
        ]);

        var result = OidcHelpers.ResolveEmailClaim(identity);

        result.Should().Be("preferred@domain.com");
    }

    [Fact]
    public void ResolveEmailClaim_FallsBackToUpn_WhenEmailAndPreferredUsernameMissing()
    {
        var identity = new ClaimsIdentity(
        [
            new Claim(ClaimTypes.Upn, "upn@domain.com")
        ]);

        var result = OidcHelpers.ResolveEmailClaim(identity);

        result.Should().Be("upn@domain.com");
    }

    [Fact]
    public void ResolveEmailClaim_ReturnsNull_WhenNoRecognisedClaimsPresent()
    {
        var identity = new ClaimsIdentity(
        [
            new Claim(ClaimTypes.Name, "Some Name")
        ]);

        var result = OidcHelpers.ResolveEmailClaim(identity);

        result.Should().BeNull();
    }

    [Fact]
    public void ResolveEmailClaim_ThrowsArgumentNullException_WhenIdentityIsNull()
    {
        var act = () => OidcHelpers.ResolveEmailClaim(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    // ── RewriteRedirectUriHost ──────────────────────────────────────────

    [Fact]
    public void RewriteRedirectUriHost_ReturnsOriginal_WhenPublicOriginIsNull()
    {
        const string redirectUri = "https://internal-azurewebsites.net/signin-oidc?code=abc";

        var result = OidcHelpers.RewriteRedirectUriHost(redirectUri, publicOrigin: null);

        result.Should().Be(redirectUri);
    }

    [Fact]
    public void RewriteRedirectUriHost_ReturnsOriginal_WhenHostsAlreadyMatch()
    {
        const string redirectUri = "https://dev-bse.azure.defra.cloud/signin-oidc?code=abc";
        var publicOrigin = new Uri("https://dev-bse.azure.defra.cloud");

        var result = OidcHelpers.RewriteRedirectUriHost(redirectUri, publicOrigin);

        result.Should().Be(redirectUri);
    }

    [Fact]
    public void RewriteRedirectUriHost_RewritesHostSchemeAndPort_WhenHostsDiffer()
    {
        const string redirectUri = "http://internal-app.azurewebsites.net/signin-oidc?code=abc";
        var publicOrigin = new Uri("https://dev-bse.azure.defra.cloud");

        var result = OidcHelpers.RewriteRedirectUriHost(redirectUri, publicOrigin);

        result.Should().StartWith("https://dev-bse.azure.defra.cloud/signin-oidc");
        result.Should().Contain("code=abc");
    }

    [Fact]
    public void RewriteRedirectUriHost_PreservesNonDefaultPort_FromPublicOrigin()
    {
        const string redirectUri = "https://internal-app.azurewebsites.net/signin-oidc?code=abc";
        var publicOrigin = new Uri("https://localhost:53642");

        var result = OidcHelpers.RewriteRedirectUriHost(redirectUri, publicOrigin);

        result.Should().StartWith("https://localhost:53642/signin-oidc");
    }

    [Fact]
    public void RewriteRedirectUriHost_ReturnsOriginal_WhenRedirectUriIsNotAbsolute()
    {
        const string redirectUri = "/signin-oidc?code=abc";
        var publicOrigin = new Uri("https://dev-bse.azure.defra.cloud");

        var result = OidcHelpers.RewriteRedirectUriHost(redirectUri, publicOrigin);

        result.Should().Be(redirectUri);
    }

    [Fact]
    public void RewriteRedirectUriHost_ThrowsArgumentNullException_WhenRedirectUriIsNull()
    {
        var act = () => OidcHelpers.RewriteRedirectUriHost(null!, new Uri("https://dev-bse.azure.defra.cloud"));

        act.Should().Throw<ArgumentNullException>();
    }
}
