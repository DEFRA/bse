using System.Net;
using BSE.Modules.UserManagement.Models;
using BSE.Modules.UserManagement.Tests.TestAuth;
using BSE.SharedKernel;
using FluentAssertions;
using NSubstitute;

namespace BSE.Modules.UserManagement.Tests.Integration;

[Trait("Category", "Integration")]
[Collection("Integration")]
public sealed class HelpNavigationIntegrationTests : IClassFixture<HomeNavigationWebFactory>
{
    private const string DefaultUpn = "testuser@placeholder.domain";

    private readonly HomeNavigationWebFactory _factory;

    public HelpNavigationIntegrationTests(HomeNavigationWebFactory factory)
        => _factory = factory;

    [Theory]
    [InlineData(UserGroup.Admin,            "Admin")]
    [InlineData(UserGroup.DataEntry,        "DEFRA Data Entry")]
    [InlineData(UserGroup.ReadOnly,         "DEFRA Viewer")]
    [InlineData(UserGroup.DEFRAMaintenance, "DEFRA Maintenance")]
    [InlineData(UserGroup.Supervisor,       "Supervisor")]
    public async Task GetHelp_AllUserGroups_Returns200(UserGroup group, string groupName)
    {
        var user = new User(1, "testuser", DefaultUpn, "Test User", null, true,
            (int)group, group, GroupName: groupName);
        _factory.MockUserRepository.GetByUpnAsync(DefaultUpn).Returns(user);

        var response = await _factory.CreateClient().GetAsync("/Help");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetHelp_AuthenticatedUser_RendersHelpNavLinkWithNewTab()
    {
        var user = new User(1, "testuser", DefaultUpn, "Test User", null, true,
            (int)UserGroup.ReadOnly, UserGroup.ReadOnly, GroupName: "DEFRA Viewer");
        _factory.MockUserRepository.GetByUpnAsync(DefaultUpn).Returns(user);

        var response = await _factory.CreateClient().GetAsync("/Home");
        var html = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        html.Should().Contain("href=\"/Help\"");
        html.Should().Contain(">Help<span");
        html.Should().Contain("(opens in new tab)");
        html.Should().Contain("target=\"_blank\"");
    }

    [Fact]
    public async Task GetHelp_Page_ContainsGuidanceContent()
    {
        var user = new User(1, "testuser", DefaultUpn, "Test User", null, true,
            (int)UserGroup.ReadOnly, UserGroup.ReadOnly, GroupName: "DEFRA Viewer");
        _factory.MockUserRepository.GetByUpnAsync(DefaultUpn).Returns(user);

        var response = await _factory.CreateClient().GetAsync("/Help");
        var html = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        html.Should().Contain("Case management");
        html.Should().Contain("Farm management");
    }
}
