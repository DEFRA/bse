using BSE.Host.Models;
using BSE.Host.Services;
using BSE.Infrastructure.Cache;
using BSE.Modules.Batch.Models;
using FluentAssertions;
using Microsoft.Extensions.Caching.Distributed;
using NSubstitute;

namespace BSE.Modules.UserManagement.Tests;

/// <summary>Covers small record view models that carried 0% new-code coverage simply because nothing
/// in the test suite ever constructed them: <see cref="CaseEditNotificationsViewModel"/>,
/// <see cref="CaseSaveCancelViewModel"/>, and <see cref="CaseTabsViewModel"/>. Each is a plain data
/// record with no branching, so one test per record that exercises both the required and optional
/// parameters is sufficient to hit every line.</summary>
public sealed class SmallCaseEditViewModelsTests
{
    [Fact]
    public void CaseEditNotificationsViewModel_WithOnlyIdPrefix_DefaultsEverythingElseToNull()
    {
        var model = new CaseEditNotificationsViewModel("feeds");

        model.IdPrefix.Should().Be("feeds");
        model.SuccessMessage.Should().BeNull();
        model.WarningMessage.Should().BeNull();
        model.ExtraFieldErrors.Should().BeNull();
    }

    [Fact]
    public void CaseEditNotificationsViewModel_WithAllFieldsSupplied_RetainsEveryValue()
    {
        var errors = new[] { "Ration name is required." };

        var model = new CaseEditNotificationsViewModel("bab", "Saved", "Saved with warnings", errors);

        model.IdPrefix.Should().Be("bab");
        model.SuccessMessage.Should().Be("Saved");
        model.WarningMessage.Should().Be("Saved with warnings");
        model.ExtraFieldErrors.Should().BeEquivalentTo(errors);
    }

    [Fact]
    public void CaseSaveCancelViewModel_WithOnlyRequiredFields_DefaultsOptionalFlagsToFalse()
    {
        var model = new CaseSaveCancelViewModel("002600001", "/Case/Farm", "CancelFarmEdit");

        model.Rbse.Should().Be("002600001");
        model.CancelPage.Should().Be("/Case/Farm");
        model.CancelHandler.Should().Be("CancelFarmEdit");
        model.SaveHandler.Should().BeNull();
        model.MarginTop.Should().BeFalse();
        model.NoMargin.Should().BeFalse();
    }

    [Fact]
    public void CaseSaveCancelViewModel_WithAllFieldsSupplied_RetainsEveryValue()
    {
        var model = new CaseSaveCancelViewModel("002600001", "/Case/Farm", "CancelFarmEdit", "SaveFarm", MarginTop: true, NoMargin: true);

        model.SaveHandler.Should().Be("SaveFarm");
        model.MarginTop.Should().BeTrue();
        model.NoMargin.Should().BeTrue();
    }

    [Fact]
    public void CaseTabsViewModel_WithOnlyRequiredFields_DefaultsEverythingElseToNull()
    {
        var model = new CaseTabsViewModel("Farm", "002600001");

        model.ActiveTab.Should().Be("Farm");
        model.Rbse.Should().Be("002600001");
        model.BatchNumbers.Should().BeNull();
        model.ViewDocsUrl.Should().BeNull();
        model.CanEditCurrentTab.Should().BeFalse();
        model.SaveHandlerOverride.Should().BeNull();
        model.CancelPageOverride.Should().BeNull();
        model.CancelHandlerOverride.Should().BeNull();
    }

    [Fact]
    public void CaseTabsViewModel_WithAllFieldsSupplied_RetainsEveryValue()
    {
        var batchNumbers = new List<BatchNumberEntry>();

        var model = new CaseTabsViewModel(
            "Farm", "002600001", batchNumbers, "/docs/002600001",
            CanEditCurrentTab: true, SaveHandlerOverride: "CreateCase",
            CancelPageOverride: "/Home", CancelHandlerOverride: null);

        model.BatchNumbers.Should().BeSameAs(batchNumbers);
        model.ViewDocsUrl.Should().Be("/docs/002600001");
        model.CanEditCurrentTab.Should().BeTrue();
        model.SaveHandlerOverride.Should().Be("CreateCase");
        model.CancelPageOverride.Should().Be("/Home");
    }
}

/// <summary>Covers <see cref="CaseScalarDraftStateService"/> (0% new-code coverage) — each method
/// delegates to <see cref="IDistributedCache"/> via JSON extension helpers, keyed by a user-scoped
/// cache key built from <see cref="ICacheKeyProvider.CaseWizard"/> and the current user's id.</summary>
public sealed class CaseScalarDraftStateServiceTests
{
    private const string Rbse = "002600001";
    private const string ExpectedKey = "user:7:scalar:002600001";

    private readonly IDistributedCache _cache = Substitute.For<IDistributedCache>();
    private readonly ICacheKeyProvider _keys = Substitute.For<ICacheKeyProvider>();
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();

    private CaseScalarDraftStateService CreateSut()
    {
        _currentUser.GetUserIdAsync().Returns(7);
        _keys.CaseWizard("7").Returns("user:7");
        return new CaseScalarDraftStateService(_cache, _keys, _currentUser);
    }

    [Fact]
    public async Task GetAsync_WhenCacheIsEmpty_ReturnsNull()
    {
        _cache.GetAsync(ExpectedKey, Arg.Any<CancellationToken>()).Returns((byte[]?)null);
        var sut = CreateSut();

        var result = await sut.GetAsync(Rbse);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAsync_WhenCacheHasAStoredDraft_DeserialisesIt()
    {
        var stored = new CaseScalarDraftState { Rbse = Rbse, HasPendingChanges = true };
        var bytes = System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(stored);
        _cache.GetAsync(ExpectedKey, Arg.Any<CancellationToken>()).Returns(bytes);
        var sut = CreateSut();

        var result = await sut.GetAsync(Rbse);

        result.Should().NotBeNull();
        result!.Rbse.Should().Be(Rbse);
        result.HasPendingChanges.Should().BeTrue();
    }

    [Fact]
    public async Task SetAsync_SerialisesTheDraftAndWritesItUnderTheUserScopedKey()
    {
        var sut = CreateSut();
        var state = new CaseScalarDraftState { Rbse = Rbse, HasPendingChanges = true };

        await sut.SetAsync(state);

        await _cache.Received(1).SetAsync(
            ExpectedKey, Arg.Any<byte[]>(), Arg.Any<DistributedCacheEntryOptions>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ClearAsync_RemovesTheUserScopedKey()
    {
        var sut = CreateSut();

        await sut.ClearAsync(Rbse);

        await _cache.Received(1).RemoveAsync(ExpectedKey, Arg.Any<CancellationToken>());
    }
}
