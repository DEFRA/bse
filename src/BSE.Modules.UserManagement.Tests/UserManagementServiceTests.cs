using BSE.Modules.UserManagement.Models;
using BSE.Modules.UserManagement.Repositories;
using BSE.Modules.UserManagement.Services;
using BSE.SharedKernel;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace BSE.Modules.UserManagement.Tests;

public sealed class UserManagementServiceTests
{
    private readonly IUserRepository _repo = Substitute.For<IUserRepository>();
    private readonly UserManagementService _sut;

    public UserManagementServiceTests()
    {
        _sut = new UserManagementService(_repo);
    }

    private static User SampleUser(int id = 1, UserGroup group = UserGroup.DataEntry) =>
        new(id, "jsmith", "jsmith@domain", "John Smith", "j.smith@domain", true, (int)group, group);

    // ── GetAllUsersAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task GetAllUsersAsync_DelegatesToRepository()
    {
        var users = new[] { SampleUser(1), SampleUser(2) };
        _repo.GetAllAsync().Returns(users);

        var result = await _sut.GetAllUsersAsync();

        result.Should().BeEquivalentTo(users);
        await _repo.Received(1).GetAllAsync();
    }

    [Fact]
    public async Task GetAllUsersAsync_ReturnsEmpty_WhenRepositoryReturnsEmpty()
    {
        _repo.GetAllAsync().Returns(Enumerable.Empty<User>());

        var result = await _sut.GetAllUsersAsync();

        result.Should().BeEmpty();
    }

    // ── AddUserAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task AddUserAsync_DelegatesToRepository_ReturnsNewId()
    {
        var user = SampleUser();
        _repo.AddAsync(user).Returns(42);

        var newId = await _sut.AddUserAsync(user);

        newId.Should().Be(42);
        await _repo.Received(1).AddAsync(user);
    }

    // ── UpdateUserAsync ────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateUserAsync_DelegatesToRepository()
    {
        var user = SampleUser(5, UserGroup.Admin);
        _repo.UpdateAsync(user).Returns(Task.CompletedTask);

        await _sut.UpdateUserAsync(user);

        await _repo.Received(1).UpdateAsync(user);
    }

    // ── Round-trip: add then get ───────────────────────────────────────────────

    [Fact]
    public async Task AddThenGet_ReturnsInsertedUser()
    {
        var newUser = SampleUser(0, UserGroup.ReadOnly);
        const int assignedId = 99;
        var savedUser = newUser with { UserId = assignedId };

        _repo.AddAsync(newUser).Returns(assignedId);
        _repo.GetAllAsync().Returns([savedUser]);

        var id = await _sut.AddUserAsync(newUser);
        var all = (await _sut.GetAllUsersAsync()).ToList();

        id.Should().Be(assignedId);
        all.Should().ContainSingle().Which.UserId.Should().Be(assignedId);
    }
}
