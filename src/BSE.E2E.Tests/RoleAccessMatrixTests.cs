namespace BSE.E2E.Tests;

/// <summary>
/// AUTH-04 / AUTH-05 executed against the <b>real application and real database</b>.
///
/// The app resolves a user's roles from the [User] table at startup via the configured
/// DevUserEmail, so one role is active per app instance. The driver script
/// (<c>Run-RoleMatrix.ps1</c>) restarts the app once per group and sets
/// <c>BSE_E2E_GROUP</c>; this class then asserts the full page matrix for that group.
///
/// This validates the mocked in-process matrix against genuine end-to-end behaviour.
/// </summary>
public sealed class RoleAccessMatrixTests(BrowserFixture fixture) : E2ETestBase(fixture)
{
    private static string Group =>
        Environment.GetEnvironmentVariable("BSE_E2E_GROUP")
        ?? throw new InvalidOperationException("BSE_E2E_GROUP not set — run via Run-RoleMatrix.ps1");

    /// <summary>Expected access per group. true = must be allowed, false = must be denied.</summary>
    private static readonly Dictionary<string, Dictionary<string, bool>> Expected = new()
    {
        ["DEFRA Viewer"] = new()
        {
            ["/Search/Menu"] = true,
            ["/AuditLog/Menu"] = true,
            ["/Bsess/CheckByRbse"] = true,
            ["/Farm/Edit/" + E2EConfig.Cphh] = false,
            ["/Farm/Lookup"] = false,
            ["/AdnsExport/Menu"] = false,
            ["/OssExport/Menu"] = false,
            ["/CaseWork/Menu"] = false,
            ["/Admin/Users"] = false,
            ["/Admin/PickLists"] = false,
        },
        ["DEFRA Data Entry"] = new()
        {
            ["/Search/Menu"] = true,
            ["/AuditLog/Menu"] = true,
            ["/Bsess/CheckByRbse"] = true,
            ["/Farm/Edit/" + E2EConfig.Cphh] = true,
            ["/Farm/Lookup"] = true,
            ["/AdnsExport/Menu"] = false,
            ["/OssExport/Menu"] = false,
            ["/CaseWork/Menu"] = false,
            ["/Admin/Users"] = false,
            ["/Admin/PickLists"] = false,
        },
        ["DEFRA Maintenance"] = new()
        {
            ["/Search/Menu"] = true,
            ["/AuditLog/Menu"] = true,
            ["/Bsess/CheckByRbse"] = true,
            ["/Farm/Edit/" + E2EConfig.Cphh] = true,
            ["/Farm/Lookup"] = true,
            ["/AdnsExport/Menu"] = true,
            ["/OssExport/Menu"] = false,
            ["/CaseWork/Menu"] = false,
            ["/Admin/Users"] = false,
            ["/Admin/PickLists"] = true,
        },
        ["VLA Data Entry"] = new()
        {
            ["/Search/Menu"] = true,
            ["/AuditLog/Menu"] = true,
            ["/Bsess/CheckByRbse"] = true,
            ["/Farm/Edit/" + E2EConfig.Cphh] = true,
            ["/Farm/Lookup"] = false,
            ["/AdnsExport/Menu"] = false,
            ["/OssExport/Menu"] = true,
            ["/CaseWork/Menu"] = false,
            ["/Admin/Users"] = false,
            ["/Admin/PickLists"] = true,
        },
        ["VLA Maintenance"] = new()
        {
            ["/Search/Menu"] = true,
            ["/AuditLog/Menu"] = true,
            ["/Bsess/CheckByRbse"] = true,
            ["/Farm/Edit/" + E2EConfig.Cphh] = true,
            ["/Farm/Lookup"] = true,
            ["/AdnsExport/Menu"] = true,
            ["/OssExport/Menu"] = true,
            ["/CaseWork/Menu"] = true,
            ["/Admin/Users"] = true,
            ["/Admin/PickLists"] = true,
        },
        ["DEFRA AI, Wales and Scotland"] = new()
        {
            ["/Search/Menu"] = true,
            ["/AuditLog/Menu"] = false,
            ["/Bsess/CheckByRbse"] = false,
            ["/Farm/Edit/" + E2EConfig.Cphh] = false,
            ["/Farm/Lookup"] = false,
            ["/AdnsExport/Menu"] = false,
            ["/OssExport/Menu"] = false,
            ["/CaseWork/Menu"] = false,
            ["/Admin/Users"] = false,
            ["/Admin/PickLists"] = false,
        },
        ["DEFRA AHO User"] = new()
        {
            ["/Search/Menu"] = true,
            ["/AuditLog/Menu"] = false,
            ["/Bsess/CheckByRbse"] = false,
            ["/Farm/Edit/" + E2EConfig.Cphh] = false,
            ["/Farm/Lookup"] = false,
            ["/AdnsExport/Menu"] = false,
            ["/OssExport/Menu"] = false,
            ["/CaseWork/Menu"] = false,
            ["/Admin/Users"] = false,
            ["/Admin/PickLists"] = false,
        },
    };

    public static TheoryData<string> Pages()
    {
        var data = new TheoryData<string>();
        foreach (var path in Expected["VLA Maintenance"].Keys) data.Add(path);
        return data;
    }

    [Theory]
    [MemberData(nameof(Pages))]
    public async Task Role_PageAccessMatchesPolicy(string path)
    {
        var group = Group;
        Assert.True(Expected.ContainsKey(group), $"No expectation defined for group '{group}'");
        var shouldAllow = Expected[group][path];

        var response = await Api.GetAsync(path, new() { MaxRedirects = 0 });
        var status = response.Status;
        var location = response.Headers.TryGetValue("location", out var l) ? l : string.Empty;

        Assert.True(status < 500,
            $"[{group}] {path}: server error HTTP {status}");

        var denied = status == 403
                     || (status is >= 300 and < 400
                         && location.Contains("Unauthorized", StringComparison.OrdinalIgnoreCase));

        if (shouldAllow)
        {
            Assert.False(denied,
                $"[{group}] {path}: expected ALLOWED but was denied (HTTP {status} -> {location})");
        }
        else
        {
            Assert.True(denied,
                $"[{group}] {path}: expected DENIED but was allowed (HTTP {status} -> {location})");
        }
    }

    [Fact]
    public async Task Role_HomePage_IsAlwaysReachable()
    {
        var response = await Api.GetAsync("/Home", new() { MaxRedirects = 0 });
        Assert.True(response.Status < 500,
            $"[{Group}] /Home: server error HTTP {response.Status}");
    }
}
