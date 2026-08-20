using System.Net;
using BSE.Modules.Batch.Services;
using BSE.Modules.CaseManagement.Repositories;
using BSE.Modules.UserManagement.Models;
using BSE.Modules.UserManagement.Repositories;
using BSE.Modules.UserManagement.Tests.TestAuth;
using BSE.SharedKernel;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace BSE.Modules.UserManagement.Tests.Integration;

[Trait("Category", "Integration")]
[Collection("Integration")]
public sealed class HomeNavigationIntegrationTests : IClassFixture<HomeNavigationWebFactory>
{
    private const string DefaultUpn = "testuser@placeholder.domain";

    private readonly HomeNavigationWebFactory _factory;

    public HomeNavigationIntegrationTests(HomeNavigationWebFactory factory)
        => _factory = factory;

    [Theory]
    [InlineData(UserGroup.Admin,            "Admin")]
    [InlineData(UserGroup.DataEntry,        "DEFRA Data Entry")]
    [InlineData(UserGroup.ReadOnly,         "DEFRA Viewer")]
    [InlineData(UserGroup.DEFRAMaintenance, "DEFRA Maintenance")]
    [InlineData(UserGroup.Supervisor,       "Supervisor")]
    public async Task GetHome_AllUserGroups_Returns200(UserGroup group, string groupName)
    {
        var user = new User(1, "testuser", DefaultUpn, "Test User", null, true,
            (int)group, group, GroupName: groupName);
        _factory.MockUserRepository.GetByUpnAsync(DefaultUpn).Returns(user);

        var response = await _factory.CreateClient().GetAsync("/Home");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetHome_AuthenticatedUser_RendersHomeNavLink()
    {
        var user = new User(1, "testuser", DefaultUpn, "Test User", null, true,
            (int)UserGroup.ReadOnly, UserGroup.ReadOnly, GroupName: "DEFRA Viewer");
        _factory.MockUserRepository.GetByUpnAsync(DefaultUpn).Returns(user);

        var response = await _factory.CreateClient().GetAsync("/Home");
        var html = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        html.Should().Contain("href=\"/Home\"");
        html.Should().Contain(">Home<");
    }

    [Fact]
    public async Task GetRoot_AuthenticatedUser_RedirectsToHome()
    {
        var user = new User(1, "testuser", DefaultUpn, "Test User", null, true,
            (int)UserGroup.ReadOnly, UserGroup.ReadOnly, GroupName: "DEFRA Viewer");
        _factory.MockUserRepository.GetByUpnAsync(DefaultUpn).Returns(user);

        var client = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        var response = await client.GetAsync("/");

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location?.ToString().Should().Be("/Home");
    }

    [Fact]
    public async Task GetHome_RendersAuthenticatedUserDisplayName()
    {
        var user = new User(1, "testuser", DefaultUpn, "Jane Smith", null, true,
            (int)UserGroup.ReadOnly, UserGroup.ReadOnly, GroupName: "DEFRA Viewer");
        _factory.MockUserRepository.GetByUpnAsync(DefaultUpn).Returns(user);

        var html = await (await _factory.CreateClient().GetAsync("/Home")).Content.ReadAsStringAsync();

        html.Should().Contain("Jane Smith");
    }

    [Fact]
    public async Task GetHome_RendersAuthenticatedUserGroupName()
    {
        var user = new User(1, "testuser", DefaultUpn, "Jane Smith", null, true,
            (int)UserGroup.ReadOnly, UserGroup.ReadOnly, GroupName: "DEFRA Viewer");
        _factory.MockUserRepository.GetByUpnAsync(DefaultUpn).Returns(user);

        var html = await (await _factory.CreateClient().GetAsync("/Home")).Content.ReadAsStringAsync();

        html.Should().Contain("DEFRA Viewer");
    }
}

public sealed class HomeNavigationWebFactory : WebApplicationFactory<Program>
{
    public IUserRepository MockUserRepository { get; } = Substitute.For<IUserRepository>();
    public IBatchService MockBatchService { get; } = Substitute.For<IBatchService>();
    public ICaseRepository MockCaseRepository { get; } = Substitute.For<ICaseRepository>();

    public HomeNavigationWebFactory()
    {
        MockBatchService.GetLatestBatchNumbersAsync().Returns([]);
        MockCaseRepository.GetLatestRbseForYearAsync(Arg.Any<short>()).Returns((string?)null);
        MockCaseRepository.GetLatestDbseForYearAsync(Arg.Any<short>()).Returns((string?)null);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Prevent Program.cs from registering DevAuthHandler so TestAuthHandler is the sole scheme.
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            services.AddAuthentication(TestAuthHandler.SchemeName)
                    .AddScheme<TestAuthOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });

            ReplaceService<IUserRepository>(services, MockUserRepository);
            ReplaceService<IBatchService>(services, MockBatchService);
            ReplaceService<ICaseRepository>(services, MockCaseRepository);
        });
    }

    private static void ReplaceService<T>(IServiceCollection services, T mock) where T : class
    {
        var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(T));
        if (descriptor is not null) services.Remove(descriptor);
        services.AddScoped<T>(_ => mock);
    }
}
