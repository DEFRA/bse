# Runs the role×page access matrix against the real app and real database.
# The app resolves roles from [User] at startup, so it is restarted once per group.
[CmdletBinding()]
param(
    [string]$RepoRoot = 'C:\Sabari\Dev\Entra\bse',
    [int]$Port = 5199,
    [string]$ConnectionString = 'Server=(localdb)\MSSQLLocalDB;Database=BSE;Trusted_Connection=True;TrustServerCertificate=True;'
)

$ErrorActionPreference = 'Stop'

# One active user per group, taken from the local [User] table.
$groups = [ordered]@{
    'DEFRA Viewer'                 = 'js.test.user2@defra.gov.uk'
    'DEFRA Data Entry'             = 'a7@abc.com'
    'DEFRA Maintenance'            = 'zztest003@defra.gov.uk'
    'VLA Data Entry'               = 'zztest004@defra.gov.uk'
    'VLA Maintenance'              = 'efr@efr.com'
    'DEFRA AI, Wales and Scotland' = 'abc@abc.comsdads'
    'DEFRA AHO User'               = 'a1@a1.com'
}

$baseUrl    = "http://localhost:$Port"
$hostProj   = Join-Path $RepoRoot 'src\BSE.Host'
$e2eProj    = Join-Path $RepoRoot 'src\BSE.E2E.Tests\BSE.E2E.Tests.csproj'
$resultsDir = Join-Path $RepoRoot 'TestResults\RoleMatrix'

Remove-Item $resultsDir -Recurse -Force -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Path $resultsDir -Force | Out-Null

function Stop-AppOnPort {
    param([int]$P)
    Get-NetTCPConnection -LocalPort $P -State Listen -ErrorAction SilentlyContinue |
        Select-Object -ExpandProperty OwningProcess -Unique |
        ForEach-Object { Stop-Process -Id $_ -Force -ErrorAction SilentlyContinue }
    Start-Sleep -Milliseconds 800
}

foreach ($group in $groups.Keys) {
    $email = $groups[$group]
    Write-Host "`n=== $group  ($email) ===" -ForegroundColor Cyan

    Stop-AppOnPort -P $Port

    $env:ASPNETCORE_ENVIRONMENT             = 'Development'
    $env:Authentication__BypassEnabled      = 'true'
    $env:Authentication__UseWindowsIdentity = 'false'
    $env:Authentication__DevUserEmail       = $email
    $env:ConnectionStrings__BSE             = $ConnectionString

    $app = Start-Process -FilePath 'dotnet' `
        -ArgumentList @('run', '--no-launch-profile', '--urls', $baseUrl) `
        -WorkingDirectory $hostProj -PassThru -WindowStyle Hidden

    # Poll until the host answers rather than sleeping a fixed amount.
    $ready = $false
    foreach ($i in 1..60) {
        Start-Sleep -Seconds 1
        try {
            Invoke-WebRequest "$baseUrl/Unauthorized" -UseBasicParsing -TimeoutSec 3 | Out-Null
            $ready = $true; break
        }
        catch {
            if ($_.Exception.Response) { $ready = $true; break }
        }
    }
    if (-not $ready) {
        Write-Warning "App did not start for $group - skipping"
        if (-not $app.HasExited) { Stop-Process -Id $app.Id -Force -ErrorAction SilentlyContinue }
        continue
    }

    $env:BSE_E2E_GROUP   = $group
    $env:BSE_E2E_BASEURL = $baseUrl

    $safe = ($group -replace '[^A-Za-z0-9]', '_')
    & dotnet test $e2eProj -c Release --no-build `
        --filter 'FullyQualifiedName~RoleAccessMatrixTests' `
        --logger "trx;LogFileName=$safe.trx" `
        --results-directory $resultsDir *>&1 | Out-Null

    $trx = Join-Path $resultsDir "$safe.trx"
    if (Test-Path $trx) {
        [xml]$doc = Get-Content $trx -Raw
        $c = $doc.TestRun.ResultSummary.Counters
        Write-Host ("  total={0} passed={1} failed={2}" -f $c.total, $c.passed, $c.failed) -ForegroundColor Yellow
    }

    Stop-AppOnPort -P $Port
    if (-not $app.HasExited) { Stop-Process -Id $app.Id -Force -ErrorAction SilentlyContinue }
}

Write-Host "`nTRX files written to $resultsDir" -ForegroundColor Green
