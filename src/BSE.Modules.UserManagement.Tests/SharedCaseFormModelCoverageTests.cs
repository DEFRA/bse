using BSE.Host.Models;
using BSE.Host.Models.ViewModels;
using BSE.Modules.CaseManagement.Commands;
using BSE.Modules.CaseManagement.Models;
using FluentAssertions;
using Xunit;

namespace BSE.Modules.UserManagement.Tests;

public sealed class SharedCaseFormModelCoverageTests
{
    [Fact]
    public void BabFormViewModel_FromRecord_MapsAllFields()
    {
        var record = new CaseBabRecord
        {
            Rbse = "002600001",
            NatalCphh = "01010101010",
            Notes = "case note",
            TracedName = "Trace Name",
            TracedAddress1 = "Address 1",
            TracedAddress2 = "Address 2",
            TracedAddress3 = "Address 3",
            TracedPostcode = "AB1 2CD",
            FeedRisk = "Low",
            HorizontalRisk = "Medium",
            MaternalRisk = "High",
            RowStamp = [1, 2, 3]
        };

        var model = BabFormViewModel.FromRecord(record);

        model.NatalCphh.Should().Be("01010101010");
        model.Notes.Should().Be("case note");
        model.TracedName.Should().Be("Trace Name");
        model.TracedAddress1.Should().Be("Address 1");
        model.TracedAddress2.Should().Be("Address 2");
        model.TracedAddress3.Should().Be("Address 3");
        model.TracedPostcode.Should().Be("AB1 2CD");
        model.FeedRisk.Should().Be("Low");
        model.HorizontalRisk.Should().Be("Medium");
        model.MaternalRisk.Should().Be("High");
    }

    [Fact]
    public void BabFormViewModel_ApplyStagedCommand_OverridesBoundValues()
    {
        var viewModel = new BabFormViewModel
        {
            NatalCphh = "old",
            Notes = "old note",
            TracedName = "Old name",
            TracedAddress1 = "Old line 1",
            TracedAddress2 = "Old line 2",
            TracedAddress3 = "Old line 3",
            TracedPostcode = "ZZ9 9ZZ",
            FeedRisk = "old feed",
            HorizontalRisk = "old horizontal",
            MaternalRisk = "old maternal"
        };

        var command = new EditCaseBabCommand(
            Rbse: "002600001",
            NatalCphh: "new-cphh",
            Notes: "new note",
            TracedName: "New name",
            TracedAddress1: "New line 1",
            TracedAddress2: "New line 2",
            TracedAddress3: "New line 3",
            TracedPostcode: "XY1 2AB",
            FeedRisk: "new feed",
            HorizontalRisk: "new horizontal",
            MaternalRisk: "new maternal",
            RowStamp: [9, 9, 9]);

        viewModel.ApplyStagedCommand(command);

        viewModel.NatalCphh.Should().Be("new-cphh");
        viewModel.Notes.Should().Be("new note");
        viewModel.TracedName.Should().Be("New name");
        viewModel.TracedAddress1.Should().Be("New line 1");
        viewModel.TracedAddress2.Should().Be("New line 2");
        viewModel.TracedAddress3.Should().Be("New line 3");
        viewModel.TracedPostcode.Should().Be("XY1 2AB");
        viewModel.FeedRisk.Should().Be("new feed");
        viewModel.HorizontalRisk.Should().Be("new horizontal");
        viewModel.MaternalRisk.Should().Be("new maternal");
    }

    [Fact]
    public void CaseCancelConfirmViewModel_StoresDirtySignalAndUnsavedState()
    {
        var model = new CaseCancelConfirmViewModel("formDirty", true);

        model.DirtyVar.Should().Be("formDirty");
        model.HasUnsavedChanges.Should().BeTrue();
    }
}
