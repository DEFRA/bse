using System.Net;
using BSE.Modules.ReferenceData.Models;
using BSE.Modules.ReferenceData.Services;
using BSE.Modules.UserManagement.Models;
using BSE.Modules.UserManagement.Repositories;
using BSE.Modules.UserManagement.Services;
using BSE.Modules.UserManagement.Tests.TestAuth;
using BSE.SharedKernel;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute;

namespace BSE.Modules.UserManagement.Tests.Integration;

/// <summary>
/// HTTP-level regression test for the Admin/Users Razor view, proving the real rendered HTML
/// (not just the PageModel) is unaffected by the Sonar remediation on Users.cshtml.
/// Exercises the full Razor Pages pipeline — routing, auth, model binding and view rendering —
/// which PageModel-only unit tests cannot cover.
/// </summary>
[Trait("Category", "Integration")]
[Collection(ProgramHostCollection.Name)]
public sealed class AdminUsersPageRenderingTests : IClassFixture<AdminUsersWebFactory>
{
    private readonly AdminUsersWebFactory _factory;

    public AdminUsersPageRenderingTests(AdminUsersWebFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task AddNewUserRow_RendersRowIdSuffixedFieldsAndNoStrayViewDataText()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/Admin/Users?IsAddingNew=true");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var html = await response.Content.ReadAsStringAsync();

        // Regression guard: before the fix, "ViewData[\"RowId\"] = addRowId;" was emitted as literal
        // page text because it sat inside the <tr> markup block instead of executing as C#.
        html.Should().NotContain("ViewData[\"RowId\"]");

        // The add-row partial must receive rowId == "new" so its field ids are unique and addressable.
        html.Should().Contain("id=\"edit-user-name-new\"");
        html.Should().Contain("id=\"edit-email-new\"");
        html.Should().Contain("id=\"edit-user-group-id-new\"");
    }

    [Fact]
    public async Task UsersGrid_RendersWithoutAddingNew_Returns200()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/Admin/Users");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var html = await response.Content.ReadAsStringAsync();
        html.Should().Contain("User Maintenance");
    }
}

/// <summary>
/// <see cref="WebApplicationFactory{TEntryPoint}"/> that authenticates as a VLA Maintenance user
/// (satisfying the page's <c>[Authorize(Policy = "VLAMaintenance")]</c>) and stubs out the
/// user/lookup services so no real database is required.
/// </summary>
public sealed class AdminUsersWebFactory : WebApplicationFactory<Program>
{
    private const string Upn = "admin.test@placeholder.domain";

    public IUserRepository MockUserRepository { get; } = Substitute.For<IUserRepository>();
    public IUserManagementService MockUserManagementService { get; } = Substitute.For<IUserManagementService>();
    public ILookupDataService MockLookupDataService { get; } = Substitute.For<ILookupDataService>();

    public AdminUsersWebFactory()
    {
        MockUserRepository.GetByEmailAsync(Upn).Returns(new User(
            1, "admintest", Upn, "Admin Test", Upn, true, 99, UserGroup.None, "VLA Maintenance"));

        MockUserManagementService.GetAllUsersAsync().Returns(new List<User>
        {
            new(2, "existinguser", "existing@placeholder.domain", "Existing User", "existing@placeholder.domain",
                true, (int)UserGroup.DataEntry, UserGroup.DataEntry, "DEFRA Data Entry")
        }.AsEnumerable());

        MockLookupDataService.GetUserGroupsAsync().Returns(new List<LuUserGroup>
        {
            new() { Id = (int)UserGroup.DataEntry, Name = "DEFRA Data Entry" }
        }.AsEnumerable());
    }

    protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
    {
        builder.UseSetting("Authentication:BypassEnabled", "false");
        builder.UseSetting("Authentication:UseWindowsIdentity", "false");
        builder.UseSetting("Authentication:DevUserEmail", Upn);

        builder.ConfigureTestServices(services =>
        {
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                options.DefaultChallengeScheme = TestAuthHandler.SchemeName;
                options.DefaultScheme = TestAuthHandler.SchemeName;
            })
            .AddScheme<TestAuthOptions, TestAuthHandler>(TestAuthHandler.SchemeName, options =>
            {
                options.DefaultUpn = Upn;
                options.DefaultDisplayName = "Admin Test";
            });

            services.RemoveAll<IUserRepository>();
            services.AddScoped<IUserRepository>(_ => MockUserRepository);

            services.RemoveAll<IUserManagementService>();
            services.AddScoped<IUserManagementService>(_ => MockUserManagementService);

            services.RemoveAll<ILookupDataService>();
            services.AddScoped<ILookupDataService>(_ => MockLookupDataService);
        });
    }
}
