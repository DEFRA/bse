using System.Diagnostics;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace BSE.E2E.Tests;

/// <summary>
/// CASE-02/06 adjacent — exercises the real commit paths of the standalone case/farm
/// maintenance pages (Delete, RBSE change, CPHH change, Move case, Final result entry)
/// against disposable case+farm records, driven through the real UI.
///
/// Finding: a case created through the real "Create a new case" page (/Case/New) can never
/// be deleted afterwards — case creation always links a BSE1 batch document (lnkBatchCase),
/// and DeleteCase.sql treats any batch link as "APHA have entered data", permanently disabling
/// the Delete button. So disposable fixtures here are seeded directly via the same AddFarm/
/// AddCase stored procedures the app itself uses (bypassing only the batch-link step), which
/// keeps every fixture genuinely deletable through the real Delete page. All synthetic
/// identifiers use RBSE prefix "9941" and CPHH prefix "99", both confirmed absent from the
/// real seeded data, so there is no risk of colliding with or mutating real records.
/// </summary>
public sealed class CaseLifecycleMaintenanceTests(BrowserFixture fixture) : E2ETestBase(fixture)
{
    private const string DevUserId = "386"; // Gunjan Arya — matches DevUserEmail in appsettings.Development.json
    private static int _sequence;

    private static string NextSequence(int digits)
    {
        var n = (uint)(System.Threading.Interlocked.Increment(ref _sequence) + Environment.TickCount);
        return (n % (uint)Math.Pow(10, digits)).ToString().PadLeft(digits, '0');
    }

    private static string NextRbse() => $"9941{NextSequence(5)}";
    private static string NextCphh() => $"99{NextSequence(9)}";

    private static async Task RunSqlAsync(string sql)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "sqlcmd",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        psi.ArgumentList.Add("-S");
        psi.ArgumentList.Add(@"(localdb)\MSSQLLocalDB");
        psi.ArgumentList.Add("-d");
        psi.ArgumentList.Add("BSE");
        psi.ArgumentList.Add("-C");
        psi.ArgumentList.Add("-b");
        psi.ArgumentList.Add("-Q");
        psi.ArgumentList.Add(sql);

        using var process = Process.Start(psi)!;
        var stdout = await process.StandardOutput.ReadToEndAsync();
        var stderr = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        Assert.True(process.ExitCode == 0, $"sqlcmd failed (exit {process.ExitCode}):\n{stdout}\n{stderr}");
    }

    /// <summary>Seeds a standalone Case+Farm pair via the app's own AddFarm/AddCase stored
    /// procedures — the same path the app uses, minus the batch link that would otherwise
    /// make the case permanently undeletable.</summary>
    private static Task SeedDisposableCaseAsync(string rbse, string cphh) => RunSqlAsync($@"
        EXEC AddFarm @CPHH='{cphh}', @OwnerName='E2E Disposable Owner', @Address1='1 Test Lane',
            @Address2=NULL, @Address3=NULL, @Postcode=NULL, @Parish='Test Parish', @District=NULL,
            @County='AVON', @CorrespondenceAddress1=NULL, @CorrespondenceAddress2=NULL, @CorrespondenceAddress3=NULL,
            @CorrespondencePostcode=NULL, @MapReference=NULL, @Herdmark1=NULL, @Herdmark2=NULL, @Herdmark3=NULL,
            @NumericHerdmark1=NULL, @NumericHerdmark2=NULL, @AHO='01', @HerdType=NULL, @PedigreeType=NULL,
            @IsDealer=0, @ADNSRegionID=1, @UserID={DevUserId};
        EXEC AddCase @RBSE='{rbse}', @CPHH='{cphh}', @EartagCountry=NULL, @EartagHerdmark=NULL, @Eartag='12345',
            @PreviousEartag=NULL, @BSE1ReceivedDate=NULL, @FormADate='2024-01-01', @FormAResubmittedDate=NULL,
            @FormBDate=NULL, @Fate=NULL, @FormCDate=NULL, @IsPurchaserBSE1Received=0, @IsBreederBSE1Received=0,
            @IsVendor1BSE1Received=0, @IsHomebredBSE1Received=0, @IsSummarySheetReceived=0, @IsPaperworkComplete=0,
            @ReportedLocation=NULL, @Survey=NULL, @Notes=NULL, @BirthDate=NULL, @IsBirthDateEst=NULL,
            @DamStatus=NULL, @BirthDateSource=NULL, @ValuationAge=NULL, @Sex=NULL, @Breed=NULL, @Origin=NULL,
            @PurchaseDate=NULL, @PurchaseAgeInMonths=NULL, @PurchasedCounty=NULL, @HerdEntryDate=NULL,
            @OnsetDate=NULL, @IsOnsetDateEst=NULL, @MonthsPregnant=NULL, @MonthsPostCalving=NULL,
            @OnsetAgeInMonths=NULL, @SlaughterDate=NULL, @UserID={DevUserId}, @AlternateDiagnosis=NULL,
            @LabComment=NULL, @CaseType=NULL;");

    /// <summary>Guaranteed-safety-net teardown: removes any leftover rows for the exact
    /// synthetic RBSEs/CPHHs a test used, regardless of what state the test left them in.
    /// A direct delete (not DeleteCase.sql) because the test may have renamed/moved the
    /// record, and because this must never be blocked by the app's own business rules.</summary>
    private static Task CleanupAsync(IEnumerable<string> rbses, IEnumerable<string> cphhs)
    {
        var rbseList = string.Join(",", rbses.Select(r => $"'{r}'"));
        var cphhList = string.Join(",", cphhs.Select(c => $"'{c}'"));
        return RunSqlAsync($@"
            DELETE FROM [Case] WHERE [RBSE] IN ({rbseList});
            DELETE FROM [Farm] WHERE [CPHH] IN ({cphhList});");
    }

    private async Task AcceptNextDialogAsync(Func<Task> action)
    {
        void OnDialog(object? _, IDialog dialog) => dialog.AcceptAsync();
        Page.Dialog += OnDialog;
        try { await action(); }
        finally { Page.Dialog -= OnDialog; }
    }

    private ILocator OkButton => Page.GetByRole(AriaRole.Button, new() { Name = "OK", Exact = true });

    /// <summary>Mirrors BseFormat.FormatCphh (NN/NNN/NNNN/NN) — duplicated here because this
    /// project has no reference to BSE.Host.</summary>
    private static string FormatCphh(string cphh) =>
        $"{cphh[..2]}/{cphh[2..5]}/{cphh[5..9]}/{cphh[9..]}";

    [Fact]
    public async Task Delete_RealDisposableCase_RemovesCaseAndFarm()
    {
        var rbse = NextRbse();
        var cphh = NextCphh();
        await SeedDisposableCaseAsync(rbse, cphh);

        try
        {
            await GotoAsync($"/Case/Delete?Rbse={rbse}");
            await Expect(Page.GetByText("E2E Disposable Owner")).ToBeVisibleAsync();
            await Expect(Page.GetByText("Farm will be deleted along with case")).ToBeVisibleAsync();

            await AcceptNextDialogAsync(async () =>
            {
                await OkButton.ClickAsync();
                await Page.WaitForURLAsync("**/MaintenanceConfirmation**", new() { Timeout = 15_000 });
            });

            var body = await Page.ContentAsync();
            Assert.Contains("Case deleted", body);

            // Prove the delete really happened, not just that the page redirected.
            await GotoAsync($"/Case/Delete?Rbse={rbse}");
            Assert.Contains("Cannot find a case with this RBSE", await Page.ContentAsync());
        }
        finally
        {
            await CleanupAsync([rbse], [cphh]);
        }
    }

    [Fact]
    public async Task RbseChange_RealDisposableCase_RenamesAndPersists()
    {
        var oldRbse = NextRbse();
        var newRbse = NextRbse();
        var cphh = NextCphh();
        await SeedDisposableCaseAsync(oldRbse, cphh);

        try
        {
            await GotoAsync($"/Case/RbseChange?OldRbse={oldRbse}");
            await Expect(Page.GetByText("E2E Disposable Owner")).ToBeVisibleAsync();

            await Page.Locator("#new-rbse").FillAsync(newRbse);
            await AcceptNextDialogAsync(async () =>
            {
                await OkButton.ClickAsync();
                await Page.WaitForURLAsync("**/MaintenanceConfirmation**", new() { Timeout = 15_000 });
            });

            var body = await Page.ContentAsync();
            Assert.Contains("RBSE change complete", body);

            // Old RBSE must be gone, new RBSE must resolve, and still point at the same farm.
            await GotoAsync($"/Case/RbseChange?OldRbse={oldRbse}");
            Assert.Contains("A case with this RBSE was not found", await Page.ContentAsync());

            await GotoAsync($"/Case/Delete?Rbse={newRbse}");
            var newBody = await Page.ContentAsync();
            Assert.Contains("E2E Disposable Owner", newBody);
        }
        finally
        {
            await CleanupAsync([oldRbse, newRbse], [cphh]);
        }
    }

    [Fact]
    public async Task CphhChange_RealDisposableFarm_RenamesAndPersists()
    {
        var rbse = NextRbse();
        var oldCphh = NextCphh();
        var newCphh = NextCphh();
        await SeedDisposableCaseAsync(rbse, oldCphh);

        try
        {
            await GotoAsync($"/Farm/CphhChange?OldCphh={oldCphh}");
            await Expect(Page.GetByText("E2E Disposable Owner")).ToBeVisibleAsync();

            await Page.Locator("#new-cphh").FillAsync(newCphh);
            await AcceptNextDialogAsync(async () =>
            {
                await OkButton.ClickAsync();
                await Page.WaitForURLAsync("**/MaintenanceConfirmation**", new() { Timeout = 15_000 });
            });

            var body = await Page.ContentAsync();
            Assert.Contains("CPHH change complete", body);

            // Old CPHH must be gone, new CPHH must resolve, and the case must follow its farm.
            await GotoAsync($"/Farm/CphhChange?OldCphh={oldCphh}");
            Assert.Contains("A farm with this CPHH was not found", await Page.ContentAsync());

            await GotoAsync($"/Farm/CphhChange?OldCphh={newCphh}");
            Assert.Contains("E2E Disposable Owner", await Page.ContentAsync());

            await GotoAsync($"/Case/Delete?Rbse={rbse}");
            var caseBody = await Page.ContentAsync();
            Assert.Contains(FormatCphh(newCphh), caseBody);
        }
        finally
        {
            await CleanupAsync([rbse], [oldCphh, newCphh]);
        }
    }

    [Fact]
    public async Task MoveCase_RealDisposableCase_MovesToDestinationFarmAndDeletesSourceFarm()
    {
        var rbseA = NextRbse();
        var cphhA = NextCphh();
        var rbseB = NextRbse();
        var cphhB = NextCphh();
        await SeedDisposableCaseAsync(rbseA, cphhA);
        await SeedDisposableCaseAsync(rbseB, cphhB);

        try
        {
            await GotoAsync($"/Case/MoveCase?Rbse={rbseA}");
            await Expect(Page.GetByText("This is the only case on this farm so the farm record will be removed")).ToBeVisibleAsync();

            await Page.Locator("#new-cphh").FillAsync(cphhB);
            await Page.GetByRole(AriaRole.Button, new() { Name = "Check", Exact = true }).ClickAsync();
            await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);

            await AcceptNextDialogAsync(async () =>
            {
                await OkButton.ClickAsync();
                await Page.WaitForURLAsync("**/MaintenanceConfirmation**", new() { Timeout = 15_000 });
            });

            var body = await Page.ContentAsync();
            Assert.Contains("Case move complete", body);

            // Source farm is gone (it was the only case on it); case A now resolves under farm B.
            await GotoAsync($"/Farm/CphhChange?OldCphh={cphhA}");
            Assert.Contains("A farm with this CPHH was not found", await Page.ContentAsync());

            await GotoAsync($"/Case/Delete?Rbse={rbseA}");
            var caseABody = await Page.ContentAsync();
            Assert.Contains(FormatCphh(cphhB), caseABody);
        }
        finally
        {
            await CleanupAsync([rbseA, rbseB], [cphhA, cphhB]);
        }
    }

    [Fact]
    public async Task FinalResultEntry_RealDisposableCase_SavesNegativeResult()
    {
        var rbse = NextRbse();
        var cphh = NextCphh();
        await SeedDisposableCaseAsync(rbse, cphh);

        try
        {
            await GotoAsync($"/Case/FinalResultEntry?Rbse={rbse}");
            await Expect(Page.Locator("#final-result")).ToBeVisibleAsync();

            await Page.Locator("#final-result").SelectOptionAsync("Neg");
            await Page.Locator("#lab-comment").FillAsync("E2E disposable case — negative result");

            var saveButton = Page.Locator("[data-save-button='final-result-save']");
            await Expect(saveButton).ToBeEnabledAsync();
            await saveButton.ClickAsync();
            await Page.WaitForURLAsync("**/MaintenanceConfirmation**", new() { Timeout = 15_000 });

            var body = await Page.ContentAsync();
            Assert.Contains("Final result saved", body);

            // Re-load and confirm the result really persisted, not just the confirmation text.
            await GotoAsync($"/Case/FinalResultEntry?Rbse={rbse}");
            var selected = await Page.Locator("#final-result").InputValueAsync();
            Assert.Equal("Neg", selected);
        }
        finally
        {
            await CleanupAsync([rbse], [cphh]);
        }
    }
}
