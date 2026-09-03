using BSE.Infrastructure.Cache;

namespace BSE.Infrastructure.Tests;

public sealed class CacheKeyProviderTests
{
    private readonly CacheKeyProvider _sut = new();

    [Fact]
    public void CaseWizard_ReturnsNamespacedKey()
    {
        var key = _sut.CaseWizard("user-42");
        key.Should().Be("bse:wizard:case:user-42");
    }

    [Fact]
    public void FarmWizard_ReturnsNamespacedKey()
    {
        var key = _sut.FarmWizard("user-42");
        key.Should().Be("bse:wizard:farm:user-42");
    }

    [Fact]
    public void CaseWizard_DifferentUsers_ReturnDifferentKeys()
    {
        _sut.CaseWizard("user-1").Should().NotBe(_sut.CaseWizard("user-2"));
    }

    [Fact]
    public void FarmWizard_DifferentUsers_ReturnDifferentKeys()
    {
        _sut.FarmWizard("user-1").Should().NotBe(_sut.FarmWizard("user-2"));
    }

    [Fact]
    public void CaseWizard_AndFarmWizard_SameUser_ReturnDifferentKeys()
    {
        _sut.CaseWizard("user-1").Should().NotBe(_sut.FarmWizard("user-1"));
    }

    [Fact]
    public void CacheKeyProvider_ImplementsInterface()
    {
        ICacheKeyProvider provider = _sut;
        provider.Should().NotBeNull();
        provider.CaseWizard("x").Should().NotBeNullOrEmpty();
        provider.FarmWizard("x").Should().NotBeNullOrEmpty();
    }
}
