using BSE.SharedKernel;
using FluentAssertions;

namespace BSE.Modules.FarmManagement.Tests;

public sealed class ChangeCphhResultTests
{
    [Fact]
    public void Success_HasIntegerValueZero()
    {
        ((int)ChangeCphhResult.Success).Should().Be(0);
    }

    [Fact]
    public void OldCphhNotFoundOrNewCphhAlreadyExists_HasIntegerValueOne()
    {
        ((int)ChangeCphhResult.OldCphhNotFoundOrNewCphhAlreadyExists).Should().Be(1);
    }

    [Fact]
    public void ErrorInsertingNewFarmRecord_HasIntegerValueTwo()
    {
        ((int)ChangeCphhResult.ErrorInsertingNewFarmRecord).Should().Be(2);
    }

    [Fact]
    public void ErrorUpdatingFarmHistorical_HasIntegerValueThree()
    {
        ((int)ChangeCphhResult.ErrorUpdatingFarmHistorical).Should().Be(3);
    }

    [Fact]
    public void ErrorUpdatingFarmRelation_HasIntegerValueFour()
    {
        ((int)ChangeCphhResult.ErrorUpdatingFarmRelation).Should().Be(4);
    }

    [Fact]
    public void ErrorUpdatingHerdSize_HasIntegerValueFive()
    {
        ((int)ChangeCphhResult.ErrorUpdatingHerdSize).Should().Be(5);
    }

    [Fact]
    public void ErrorUpdatingCase_HasIntegerValueSix()
    {
        ((int)ChangeCphhResult.ErrorUpdatingCase).Should().Be(6);
    }

    [Fact]
    public void ErrorUpdatingOtherOwner_HasIntegerValueSeven()
    {
        ((int)ChangeCphhResult.ErrorUpdatingOtherOwner).Should().Be(7);
    }

    [Fact]
    public void ErrorDeletingOldFarmRecord_HasIntegerValueEight()
    {
        ((int)ChangeCphhResult.ErrorDeletingOldFarmRecord).Should().Be(8);
    }

    [Fact]
    public void AllReturnCodes_DefinedForZeroToEight()
    {
        var values = Enum.GetValues<ChangeCphhResult>().Select(v => (int)v).OrderBy(v => v).ToArray();
        values.Should().Equal(0, 1, 2, 3, 4, 5, 6, 7, 8);
    }

    [Theory]
    [InlineData(0, ChangeCphhResult.Success)]
    [InlineData(1, ChangeCphhResult.OldCphhNotFoundOrNewCphhAlreadyExists)]
    [InlineData(2, ChangeCphhResult.ErrorInsertingNewFarmRecord)]
    [InlineData(3, ChangeCphhResult.ErrorUpdatingFarmHistorical)]
    [InlineData(4, ChangeCphhResult.ErrorUpdatingFarmRelation)]
    [InlineData(5, ChangeCphhResult.ErrorUpdatingHerdSize)]
    [InlineData(6, ChangeCphhResult.ErrorUpdatingCase)]
    [InlineData(7, ChangeCphhResult.ErrorUpdatingOtherOwner)]
    [InlineData(8, ChangeCphhResult.ErrorDeletingOldFarmRecord)]
    public void CastFromInt_ProducesCorrectEnumValue(int code, ChangeCphhResult expected)
    {
        ((ChangeCphhResult)code).Should().Be(expected);
    }
}
