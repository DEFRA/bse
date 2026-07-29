using System.Security.Claims;
using BSE.Modules.UserManagement.Identity;
using BSE.Modules.UserManagement.Models;
using BSE.Modules.UserManagement.Repositories;
using BSE.SharedKernel;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace BSE.Modules.UserManagement.Tests;

public sealed class GroupClaimsTransformationTests
{
    private readonly IUserRepository _repo = Substitute.For<IUserRepository>();
    private readonly GroupClaimsTransformation _sut;

    public GroupClaimsTransformationTests()
    {
        _sut = new GroupClaimsTransformation(_repo);
    }

    // ── Helper ─────────────────────────────────────────────────────────────────

    private static ClaimsPrincipal AuthenticatedPrincipal(params Claim[] claims)
    {
        var identity = new ClaimsIdentity(claims, "test");
        return new ClaimsPrincipal(identity);
    }

    private static User MakeUser(UserGroup group) =>
        new(1, "ntlogin", "upn@domain", "Test User", null, true, (int)group, group);

    // ── Tests ──────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Unauthenticated_Principal_IsReturnedUnchanged()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity()); // no authentication type → IsAuthenticated = false

        var result = await _sut.TransformAsync(principal);

        result.Should().BeSameAs(principal);
        await _repo.DidNotReceive().GetByUpnAsync(Arg.Any<string>());
    }

    [Fact]
    public async Task Already_Transformed_Principal_IsReturnedUnchanged()
    {
        // Principal already has bse:group → should not hit the repository again.
        var principal = AuthenticatedPrincipal(
            new Claim("preferred_username", "user@domain.com"),
            new Claim(ClaimsUserContext.BseGroupClaimType, "DataEntry"));

        var result = await _sut.TransformAsync(principal);

        result.Should().BeSameAs(principal);
        await _repo.DidNotReceive().GetByUpnAsync(Arg.Any<string>());
    }

    [Fact]
    public async Task No_Upn_Claim_ReturnsUnchanged()
    {
        var principal = AuthenticatedPrincipal(new Claim(ClaimTypes.Name, "No UPN"));

        var result = await _sut.TransformAsync(principal);

        result.Should().BeSameAs(principal);
    }

    [Fact]
    public async Task UpnLookup_Succeeds_AddsGroupAndRoleClaims()
    {
        const string upn = "alice@test.domain";
        var user = MakeUser(UserGroup.DataEntry);
        _repo.GetByUpnAsync(upn).Returns(user);

        var principal = AuthenticatedPrincipal(new Claim("preferred_username", upn));
        var result = await _sut.TransformAsync(principal);

        result.FindFirst(ClaimsUserContext.BseGroupClaimType)!.Value
              .Should().Be("DataEntry");
        result.FindFirst(ClaimTypes.Role)!.Value
              .Should().Be("DataEntry");

        await _repo.DidNotReceive().GetByNtLoginAsync(Arg.Any<string>());
    }

    [Fact]
    public async Task UpnLookup_Fails_FallsBackToNtLogin_UsingLocalPart()
    {
        const string upn = "bob.smith@test.domain";
        var user = MakeUser(UserGroup.Admin);

        _repo.GetByUpnAsync(upn).Returns((User?)null);
        _repo.GetByNtLoginAsync("bob.smith").Returns(user);

        var principal = AuthenticatedPrincipal(new Claim("preferred_username", upn));
        var result = await _sut.TransformAsync(principal);

        result.FindFirst(ClaimsUserContext.BseGroupClaimType)!.Value
              .Should().Be("Admin");
        await _repo.Received(1).GetByNtLoginAsync("bob.smith");
    }

    [Fact]
    public async Task BothLookups_Fail_ClaimsUnchanged()
    {
        const string upn = "unknown@test.domain";
        _repo.GetByUpnAsync(upn).Returns((User?)null);
        _repo.GetByNtLoginAsync("unknown").Returns((User?)null);

        var principal = AuthenticatedPrincipal(new Claim("preferred_username", upn));
        var result = await _sut.TransformAsync(principal);

        result.HasClaim(c => c.Type == ClaimsUserContext.BseGroupClaimType)
              .Should().BeFalse();
    }

    [Fact]
    public async Task ClaimTypes_Upn_UsedAsFallback_WhenPreferredUsernameAbsent()
    {
        const string upn = "charlie@legacy.domain";
        var user = MakeUser(UserGroup.ReadOnly);
        _repo.GetByUpnAsync(upn).Returns(user);

        // No preferred_username — only ClaimTypes.Upn
        var principal = AuthenticatedPrincipal(new Claim(ClaimTypes.Upn, upn));
        var result = await _sut.TransformAsync(principal);

        result.FindFirst(ClaimsUserContext.BseGroupClaimType)!.Value
              .Should().Be("ReadOnly");
    }

    [Theory]
    [InlineData(UserGroup.Admin)]
    [InlineData(UserGroup.DataEntry)]
    [InlineData(UserGroup.ReadOnly)]
    [InlineData(UserGroup.DEFRAMaintenance)]
    [InlineData(UserGroup.Supervisor)]
    public async Task AllGroups_AreIssuedCorrectly(UserGroup group)
    {
        const string upn = "grouptest@domain.com";
        _repo.GetByUpnAsync(upn).Returns(MakeUser(group));

        var principal = AuthenticatedPrincipal(new Claim("preferred_username", upn));
        var result = await _sut.TransformAsync(principal);

        result.FindFirst(ClaimsUserContext.BseGroupClaimType)!.Value
              .Should().Be(group.ToString());
    }
}
