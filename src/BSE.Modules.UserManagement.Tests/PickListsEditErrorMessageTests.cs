using BSE.Host.Pages.Admin;
using FluentAssertions;
using Xunit;

namespace BSE.Modules.UserManagement.Tests;

public class PickListsEditErrorMessageTests
{
    [Fact]
    public void SaveFailedMessage_MatchesLegacyRowErrorWording_AndExposesNoSqlText()
    {
        PickListsModel.SaveFailedMessage.Should().Be("Unable to save the selected row");
        PickListsModel.SaveFailedMessage.Should().NotContain("PRIMARY KEY");
    }

    [Theory]
    [InlineData(28, "Name", 100)]
    [InlineData(28, "Other", null)]
    [InlineData(17, "Name", null)]
    public void MaxLengthFor_OnlyLimitsAhroName(int tableId, string column, int? expected)
    {
        PickListsModel.MaxLengthFor(tableId, column).Should().Be(expected);
    }

    [Fact]
    public void AddFailedMessage_MatchesLegacyRowErrorWording()
    {
        PickListsModel.AddFailedMessage.Should().Be("Unable to add the selected row");
    }
}
