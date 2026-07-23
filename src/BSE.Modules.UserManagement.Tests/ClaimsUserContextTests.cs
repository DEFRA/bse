using System.Security.Claims;
using BSE.Modules.UserManagement.Identity;
using BSE.SharedKernel;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using NSubstitute;
using Xunit;

namespace BSE.Modules.UserManagement.Tests;

public sealed class ClaimsUserContextTests
{
    private static ClaimsUserContext BuildContext(params Claim[] claims)
    {
        var identity = new ClaimsIdentity(claims, "test");
        var principal = new ClaimsPrincipal(identity);

        var httpContext = new DefaultHttpContext { User = principal };
        var accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns(httpContext);

        return new ClaimsUserContext(accessor);
    }

    // ── Upn resolution ─────────────────────────────────────────────────────────

    [Fact]
    public void Upn_ReadsPreferredUsername_First()
    {
        var ctx = BuildContext(
            new Claim("preferred_username", "alice@test.domain"),
            new Claim(ClaimTypes.Upn, "other@test.domain"));

        ctx.Upn.Should().Be("alice@test.domain");
    }

    [Fact]
    public void Upn_FallsBackToClaimTypesUpn_WhenPreferredUsernameAbsent()
    {
        var ctx = BuildContext(new Claim(ClaimTypes.Upn, "bob@test.domain"));
        ctx.Upn.Should().Be("bob@test.domain");
    }

    [Fact]
    public void Upn_ReturnsEmptyString_WhenNeitherClaimPresent()
    {
        var ctx = BuildContext();
        ctx.Upn.Should().BeEmpty();
    }

    // ── DisplayName resolution ─────────────────────────────────────────────────

    [Fact]
    public void DisplayName_ReadsClaimTypesName()
    {
        var ctx = BuildContext(new Claim(ClaimTypes.Name, "Alice Smith"));
        ctx.DisplayName.Should().Be("Alice Smith");
    }

    [Fact]
    public void DisplayName_FallsBackTo_NameClaim()
    {
        var ctx = BuildContext(new Claim("name", "Bob Jones"));
        ctx.DisplayName.Should().Be("Bob Jones");
    }

    [Fact]
    public void DisplayName_FallsBackToUpn_WhenNoNameClaim()
    {
        var ctx = BuildContext(new Claim("preferred_username", "charlie@domain"));
        ctx.DisplayName.Should().Be("charlie@domain");
    }

    // ── Group resolution ───────────────────────────────────────────────────────

    [Theory]
    [InlineData("Admin",            UserGroup.Admin)]
    [InlineData("DataEntry",        UserGroup.DataEntry)]
    [InlineData("ReadOnly",         UserGroup.ReadOnly)]
    [InlineData("DEFRAMaintenance", UserGroup.DEFRAMaintenance)]
    [InlineData("Supervisor",       UserGroup.Supervisor)]
    public void Group_ParsesBseGroupClaim_Correctly(string claimValue, UserGroup expected)
    {
        var ctx = BuildContext(new Claim(ClaimsUserContext.BseGroupClaimType, claimValue));
        ctx.Group.Should().Be(expected);
    }

    [Fact]
    public void Group_ReturnsNone_WhenClaimAbsent()
    {
        var ctx = BuildContext();
        ctx.Group.Should().Be(UserGroup.None);
    }

    [Fact]
    public void Group_ReturnsNone_WhenClaimValueUnrecognised()
    {
        var ctx = BuildContext(new Claim(ClaimsUserContext.BseGroupClaimType, "UnknownGroup"));
        ctx.Group.Should().Be(UserGroup.None);
    }

    // ── IsInGroup ─────────────────────────────────────────────────────────────

    [Fact]
    public void IsInGroup_ReturnsTrue_WhenGroupMatches()
    {
        var ctx = BuildContext(new Claim(ClaimsUserContext.BseGroupClaimType, "Admin"));
        ctx.IsInGroup(UserGroup.Admin).Should().BeTrue();
    }

    [Fact]
    public void IsInGroup_ReturnsFalse_WhenGroupDoesNotMatch()
    {
        var ctx = BuildContext(new Claim(ClaimsUserContext.BseGroupClaimType, "Admin"));
        ctx.IsInGroup(UserGroup.ReadOnly).Should().BeFalse();
    }

    [Fact]
    public void IsInGroup_ReturnsFalse_WhenNoGroupClaim()
    {
        var ctx = BuildContext();
        ctx.IsInGroup(UserGroup.DataEntry).Should().BeFalse();
    }
}
