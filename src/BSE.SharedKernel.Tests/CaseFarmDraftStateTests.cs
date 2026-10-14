namespace BSE.SharedKernel.Tests;

public sealed class CaseFarmDraftStateTests
{
    [Fact]
    public void CaseFarmDraftLinkedFarmItem_InitialisesStableDefaults()
    {
        var item = new CaseFarmDraftLinkedFarmItem();

        item.ClientKey.Should().NotBeNullOrWhiteSpace();
        item.Id.Should().BeNull();
        item.RelatedCphh.Should().BeEmpty();
        item.RowStampBase64.Should().BeEmpty();
        item.Status.Should().BeEmpty();
    }

    [Fact]
    public void CaseFarmDraftHerdSizeItem_InitialisesExpectedDraftDefaults()
    {
        var item = new CaseFarmDraftHerdSizeItem();

        item.ClientKey.Should().NotBeNullOrWhiteSpace();
        item.Id.Should().BeNull();
        item.HerdYear.Should().Be(0);
        item.TotalSize.Should().Be(0);
        item.RowStampBase64.Should().BeEmpty();
        item.Lactation1Size.Should().BeNull();
        item.Lactation10PlusSize.Should().BeNull();
    }
}
