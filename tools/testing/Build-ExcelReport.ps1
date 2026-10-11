# Builds a formatted BSE-Test-Report.xlsx from the per-project TRX files.
# Uses Excel COM because the ImportExcel module cannot be installed in this environment.
[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$TrxFolder,
    [Parameter(Mandatory)][string]$OutputPath
)

$ErrorActionPreference = 'Stop'

# Maps a test's name to the business capability a stakeholder would recognise.
# Only the method signature is matched - InlineData arguments embed URLs that would
# otherwise pull every parameterised case into the wrong area.
function Get-FunctionalArea {
    param([string]$TestName)
    $sig = ($TestName -split '\(')[0]
    switch -Regex ($sig) {
        'RoleAccessMatrix|AuthorizationMatrix|GroupMatrix|Identity|Claims|Auth' { return 'Authentication & Roles' }
        'Module_CaseTabs'                                      { return 'Case Management (E2E)' }
        'Module_Search'                                        { return 'Search (E2E)' }
        'Module_AuditLog'                                      { return 'Audit (E2E)' }
        'Module_CaseWork'                                      { return 'Case Work (E2E)' }
        'Module_Admin'                                         { return 'User & Admin (E2E)' }
        'Module_Farm'                                          { return 'Farm Management (E2E)' }
        'Module_Exports'                                       { return 'Regulatory Exports (E2E)' }
        'Module_ErrorPages|Module_ErrorHandling'               { return 'Error Handling (E2E)' }
        'Module_Core'                                          { return 'Core Pages (E2E)' }
        'CaseWork'                                             { return 'Case Work' }
        'Adns|Oss|Bsess'                                       { return 'Regulatory Exports' }
        'Farm'                                                 { return 'Farm Management' }
        'Search'                                               { return 'Search' }
        'Audit'                                                { return 'Audit' }
        'Batch'                                                { return 'Batch Management' }
        'AnimalRelations'                                      { return 'Animal Relations' }
        'ReferenceData|Lookup'                                 { return 'Reference Data' }
        'Case|Clinical|Vla|Bab|Feed|Relations|Edit'            { return 'Case Management' }
        'User|Admin|PickList'                                  { return 'User & Admin' }
        default                                                { return 'Platform' }
    }
}

$results = [System.Collections.Generic.List[object]]::new()
foreach ($trx in Get-ChildItem -LiteralPath $TrxFolder -Filter *.trx) {
    # -Raw is required: without it Get-Content yields a line array that cannot cast to [xml].
    [xml]$doc = Get-Content -LiteralPath $trx.FullName -Raw
    foreach ($r in $doc.TestRun.Results.UnitTestResult) {
        $ms = 0.0
        if ($r.duration) { $ms = [math]::Round(([TimeSpan]$r.duration).TotalMilliseconds, 1) }
        $results.Add([PSCustomObject]@{
            Area       = Get-FunctionalArea $r.testName
            TestName   = [string]$r.testName
            Outcome    = [string]$r.outcome
            DurationMs = $ms
            Error      = [string]$r.Output.ErrorInfo.Message
            StackTrace = [string]$r.Output.ErrorInfo.StackTrace
        })
    }
}
if ($results.Count -eq 0) { throw "No results parsed from $TrxFolder" }

$passedAll = @($results | Where-Object { $_.Outcome -eq 'Passed' }).Count
$failedAll = @($results | Where-Object { $_.Outcome -eq 'Failed' }).Count
$failures  = @($results | Where-Object { $_.Outcome -eq 'Failed' })

$summary = @(
    $results | Group-Object Area | ForEach-Object {
        $p = @($_.Group | Where-Object { $_.Outcome -eq 'Passed' }).Count
        $f = @($_.Group | Where-Object { $_.Outcome -eq 'Failed' }).Count
        [PSCustomObject]@{
            Area   = $_.Name
            Total  = $_.Count
            Passed = $p
            Failed = $f
            PassPc = if ($_.Count) { [math]::Round(100 * $p / $_.Count, 1) } else { 0 }
        }
    } | Sort-Object Area
)

function Set-Grid {
    param($Sheet, $Rows, [string[]]$Props, [string[]]$Headers, [string]$Title)

    $data = @($Rows)
    $cols = $Headers.Count

    $Sheet.Cells.Item(1, 1).Value2 = $Title
    $titleRange = $Sheet.Range($Sheet.Cells.Item(1, 1), $Sheet.Cells.Item(1, $cols))
    $titleRange.Merge()
    $titleRange.Font.Size = 14
    $titleRange.Font.Bold = $true

    # One 2-D array assignment is far faster than per-cell COM round-trips.
    $rowCount = $data.Count + 1
    $grid = New-Object 'object[,]' -ArgumentList $rowCount, $cols
    for ($c = 0; $c -lt $cols; $c++) { $grid[0, $c] = $Headers[$c] }
    for ($r = 0; $r -lt $data.Count; $r++) {
        # Index must be precomputed: inside [ ] the comma binds tighter than +.
        $gr = $r + 1
        for ($c = 0; $c -lt $cols; $c++) {
            $grid[$gr, $c] = [string]$data[$r].($Props[$c])
        }
    }

    $lastRow = 2 + $rowCount
    $rng = $Sheet.Range($Sheet.Cells.Item(3, 1), $Sheet.Cells.Item($lastRow, $cols))
    $rng.Value2 = $grid

    $hdr = $Sheet.Range($Sheet.Cells.Item(3, 1), $Sheet.Cells.Item(3, $cols))
    $hdr.Font.Bold = $true
    $hdr.Interior.Color = 10040115
    $hdr.Font.Color = 16777215
    $rng.Columns.AutoFit() | Out-Null
}

$excel = New-Object -ComObject Excel.Application
$excel.Visible = $false
$excel.DisplayAlerts = $false
$book = $null

try {
    $book = $excel.Workbooks.Add()
    while ($book.Worksheets.Count -gt 1) { $book.Worksheets.Item($book.Worksheets.Count).Delete() }

    $s1 = $book.Worksheets.Item(1)
    $s1.Name = 'Summary'
    Set-Grid -Sheet $s1 -Rows $summary `
        -Props   @('Area', 'Total', 'Passed', 'Failed', 'PassPc') `
        -Headers @('Functional Area', 'Total', 'Passed', 'Failed', 'Pass %') `
        -Title   "BSE Test Report - Summary - $(Get-Date -Format 'yyyy-MM-dd HH:mm')"

    $tRow = 3 + $summary.Count + 2
    $s1.Cells.Item($tRow, 1).Value2 = 'TOTAL'
    $s1.Cells.Item($tRow, 2).Value2 = $results.Count
    $s1.Cells.Item($tRow, 3).Value2 = $passedAll
    $s1.Cells.Item($tRow, 4).Value2 = $failedAll
    $s1.Range($s1.Cells.Item($tRow, 1), $s1.Cells.Item($tRow, 5)).Font.Bold = $true

    $s2 = $book.Worksheets.Add([System.Reflection.Missing]::Value, $s1)
    $s2.Name = 'All Results'
    Set-Grid -Sheet $s2 -Rows ($results | Sort-Object Area, TestName) `
        -Props   @('Area', 'TestName', 'Outcome', 'DurationMs', 'Error') `
        -Headers @('Area', 'Test Name', 'Outcome', 'Duration (ms)', 'Error') `
        -Title   'All Test Results'

    $s3 = $book.Worksheets.Add([System.Reflection.Missing]::Value, $s2)
    $s3.Name = 'Failures'
    if ($failures.Count -gt 0) {
        Set-Grid -Sheet $s3 -Rows $failures `
            -Props   @('Area', 'TestName', 'Error', 'StackTrace') `
            -Headers @('Area', 'Test Name', 'Error', 'Stack Trace') `
            -Title   'Failures'
    }
    else {
        $s3.Cells.Item(1, 1).Value2 = 'No failures in this run.'
        $s3.Cells.Item(1, 1).Font.Size = 14
        $s3.Cells.Item(1, 1).Font.Bold = $true
    }

    $s1.Activate()
    $book.SaveAs($OutputPath, 51)
    Write-Host "Written: $OutputPath"
    Write-Host "Tests: $($results.Count) | Passed: $passedAll | Failed: $failedAll | Areas: $($summary.Count)"
}
finally {
    if ($book) { $book.Close($false) }
    $excel.Quit()
    [System.Runtime.InteropServices.Marshal]::ReleaseComObject($excel) | Out-Null
}
