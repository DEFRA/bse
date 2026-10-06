using BSE.Modules.FarmManagement.Models;
using FluentAssertions;

namespace BSE.Modules.FarmManagement.Tests;

public sealed class HerdSizeModelTests
{
    [Fact]
    public void AddHerdSizeCommand_StoresAllParameters()
    {
        var command = new AddHerdSizeCommand(
            "01001000101",
            2024,
            125,
            10,
            20,
            30,
            40,
            50,
            60,
            70,
            80,
            90,
            100,
            110);

        command.CPHH.Should().Be("01001000101");
        command.HerdYear.Should().Be((short)2024);
        command.TotalSize.Should().Be((short)125);
        command.Lactation1Size.Should().Be((short?)10);
        command.Lactation10PlusSize.Should().Be((short?)110);
    }

    [Fact]
    public void UpdateHerdSizeCommand_StoresAllParametersAndRowStamp()
    {
        var rowStamp = new byte[] { 1, 2, 3, 4 };

        var command = new UpdateHerdSizeCommand(
            99,
            2023,
            150,
            11,
            12,
            13,
            14,
            15,
            16,
            17,
            18,
            19,
            20,
            21,
            rowStamp);

        command.ID.Should().Be(99);
        command.HerdYear.Should().Be((short)2023);
        command.TotalSize.Should().Be((short)150);
        command.Lactation1Size.Should().Be((short?)11);
        command.Lactation10PlusSize.Should().Be((short?)21);
        command.RowStamp.Should().Equal(rowStamp);
    }

    [Fact]
    public void HerdSizeRecord_StoresExpectedValues()
    {
        var row = new HerdSizeRecord
        {
            ID = 7,
            CPHH = "01001000101",
            HerdYear = 2022,
            TotalSize = 85,
            Lactation1Size = 5,
            Lactation2Size = 10,
            Lactation10Size = 15,
            Lactation10PlusSize = 25,
            RowStamp = new byte[] { 9, 9 }
        };

        row.ID.Should().Be(7);
        row.CPHH.Should().Be("01001000101");
        row.HerdYear.Should().Be((short)2022);
        row.TotalSize.Should().Be((short)85);
        row.Lactation1Size.Should().Be((short?)5);
        row.Lactation2Size.Should().Be((short?)10);
        row.Lactation10Size.Should().Be((short?)15);
        row.Lactation10PlusSize.Should().Be((short?)25);
        row.RowStamp.Should().Equal(new byte[] { 9, 9 });
    }
}
