using BSE.Host.Pages.Case;
using FluentAssertions;

namespace BSE.Modules.UserManagement.Tests;

public sealed class ClinicalViewModelTests
{
    [Theory]
    [InlineData(null, true)]
    [InlineData(0, true)]
    [InlineData(5, false)]
    public void StagedVisitItem_IsUnsavedUntilItHasAPersistedId(int? id, bool expected) =>
        new ClinicalModel.StagedVisitItem { Id = id }.IsUnsaved.Should().Be(expected);

    [Fact]
    public void ClinicalSigns_ToAddCommand_CarriesTheRbseAndTheTickedSigns()
    {
        var signs = new ClinicalModel.ClinicalSignsViewModel { Apprehension = true, MilkYield = true };

        var command = signs.ToAddCommand("002600001");

        command.Rbse.Should().Be("002600001");
        command.Apprehension.Should().BeTrue();
        command.MilkYield.Should().BeTrue();
        command.Kicking.Should().BeFalse();
    }
}
