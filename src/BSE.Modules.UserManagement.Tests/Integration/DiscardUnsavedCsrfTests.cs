using System.Net;
using BSE.Modules.UserManagement.Models;
using BSE.Modules.UserManagement.Repositories;
using BSE.SharedKernel;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace BSE.Modules.UserManagement.Tests.Integration;

[Trait("Category", "Integration")]
[Collection(ProgramHostCollection.Name)]
public sealed class DiscardUnsavedCsrfTests : IClassFixture<UserManagementWebFactory>
{
    private readonly UserManagementWebFactory _factory;

    public DiscardUnsavedCsrfTests(UserManagementWebFactory factory) => _factory = factory;

    [Fact]
    public async Task DiscardUnsaved_WithoutAntiforgeryToken_IsRejected()
    {
        const string upn = "testuser@placeholder.domain";
        _factory.MockUserRepository.GetByEmailAsync(upn).Returns(
            new User(1, "testuser", upn, "Test User", null, true, (int)UserGroup.DataEntry, UserGroup.DataEntry,
                GroupName: "DEFRA Data Entry"));
        var client = _factory.CreateClient(new() { AllowAutoRedirect = false });

        var response = await client.PostAsync("/case/123/discard-unsaved", content: null, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task DiscardUnsaved_WithForgedToken_IsRejected()
    {
        const string upn = "testuser@placeholder.domain";
        _factory.MockUserRepository.GetByEmailAsync(upn).Returns(
            new User(1, "testuser", upn, "Test User", null, true, (int)UserGroup.DataEntry, UserGroup.DataEntry,
                GroupName: "DEFRA Data Entry"));
        var client = _factory.CreateClient(new() { AllowAutoRedirect = false });
        using var request = new HttpRequestMessage(HttpMethod.Post, "/case/123/discard-unsaved");
        request.Headers.Add("RequestVerificationToken", "forged");

        var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
