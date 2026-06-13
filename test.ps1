#Requires -Version 5.1
<#
.SYNOPSIS
    Vendor-neutral test runner for the Minded repository.

.DESCRIPTION
    Single entry point for every test tier, locally and from any CI system.
    CI pipelines should call this script rather than defining their own test logic.

    Tiers:
      unit - Framework and Extensions tests (Tests/) plus Example unit tests.
             No infrastructure required.
      api  - Example in-process API E2E and integration tests (WebApplicationFactory,
             SQLite in-memory). No infrastructure required.
      ui   - Playwright browser E2E tests (Example/Tests/e2e-ui): full stack with real
             frontend, API and PostgreSQL. Starts the database container automatically.
      all  - unit, api and ui in sequence.

    Results are written to TestResults/<tier>/ as TRX files plus Cobertura
    coverage reports (collected via coverlet).

.PARAMETER Tier
    Which tier to run: unit (default), api, ui or all.

.PARAMETER Configuration
    Build configuration passed to dotnet test. Default: Debug.

.PARAMETER ApiDatabase
    Database for the api tier: sqlite (default, in-memory, no infrastructure) or
    postgres (real PostgreSQL via docker-compose.tests.yml; each run uses a unique,
    automatically dropped database).

.EXAMPLE
    pwsh ./test.ps1 -Tier unit

.EXAMPLE
    pwsh ./test.ps1 -Tier api -ApiDatabase postgres

.EXAMPLE
    pwsh ./test.ps1 -Tier all -Configuration Release
#>
[CmdletBinding()]
param(
    [ValidateSet('unit', 'api', 'ui', 'all')]
    [string]$Tier = 'unit',

    [string]$Configuration = 'Debug',

    [ValidateSet('sqlite', 'postgres')]
    [string]$ApiDatabase = 'sqlite'
)

$ErrorActionPreference = 'Stop'
$repoRoot = $PSScriptRoot
$resultsRoot = Join-Path $repoRoot 'TestResults'

function Get-TierProjects {
    param([string]$TierName)

    $frameworkTestsDir = Join-Path $repoRoot 'Tests'
    $exampleTestsDir = Join-Path (Join-Path $repoRoot 'Example') 'Tests'

    # Projects named Common.* under Example/Tests are shared helper libraries
    # (no [TestClass] content) despite their *Tests suffix - never run them.
    switch ($TierName) {
        'unit' {
            @(Get-ChildItem -Path $frameworkTestsDir -Recurse -Filter '*Tests.csproj') +
            @(Get-ChildItem -Path $exampleTestsDir -Recurse -Filter '*UnitTests.csproj' |
                Where-Object { $_.Name -notlike 'Common.*' })
        }
        'api' {
            @(Get-ChildItem -Path $exampleTestsDir -Recurse -Filter '*IntegrationTests.csproj' |
                Where-Object { $_.Name -notlike 'Common.*' }) +
            @(Get-ChildItem -Path $exampleTestsDir -Recurse -Filter '*E2ETests.csproj' |
                Where-Object { $_.Name -notlike 'Common.*' })
        }
        default {
            @()
        }
    }
}

function Invoke-DotnetTestTier {
    param([string]$TierName)

    $projects = Get-TierProjects -TierName $TierName | Sort-Object Name
    if (-not $projects) {
        throw "No test projects discovered for tier '$TierName'."
    }

    $tierResultsDir = Join-Path $resultsRoot $TierName
    Write-Host "`n=== Tier '$TierName': $($projects.Count) project(s) ===" -ForegroundColor Cyan

    $results = @()
    foreach ($project in $projects) {
        Write-Host "`n>>> [$TierName] $($project.BaseName)" -ForegroundColor Cyan
        $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()

        # Out-Host keeps dotnet output visible without polluting the function's
        # return stream (uncaptured command output would corrupt $results).
        & dotnet test $project.FullName `
            --configuration $Configuration `
            --logger "trx;LogFileName=$($project.BaseName).trx" `
            --results-directory $tierResultsDir `
            --collect "XPlat Code Coverage" `
            --nologo `
            --verbosity minimal | Out-Host

        $stopwatch.Stop()
        $results += [pscustomobject]@{
            Tier     = $TierName
            Project  = $project.BaseName
            Passed   = ($LASTEXITCODE -eq 0)
            Seconds  = [math]::Round($stopwatch.Elapsed.TotalSeconds, 1)
        }
    }
    return $results
}

function Confirm-PostgresAvailable {
    # Returns $true when the PostgreSQL test database is up (starting Docker if needed)
    & docker info 2>$null | Out-Null
    if ($LASTEXITCODE -ne 0) {
        Write-Host 'Docker engine is not running - attempting to start it...' -ForegroundColor Yellow
        & docker desktop start 2>$null | Out-Null

        $engineDeadline = (Get-Date).AddSeconds(120)
        while ((Get-Date) -lt $engineDeadline) {
            & docker info 2>$null | Out-Null
            if ($LASTEXITCODE -eq 0) { break }
            Start-Sleep -Seconds 5
        }

        & docker info 2>$null | Out-Null
        if ($LASTEXITCODE -ne 0) {
            Write-Host 'The Docker engine did not start within 2 minutes.' -ForegroundColor Red
            Write-Host 'Start Docker Desktop manually (check for pending updates or dialogs), or enable' -ForegroundColor Red
            Write-Host '"Start Docker Desktop when you sign in" in Docker Desktop Settings > General.' -ForegroundColor Red
            return $false
        }
    }

    & docker compose -f (Join-Path $repoRoot 'docker-compose.tests.yml') up -d --wait | Out-Host
    if ($LASTEXITCODE -ne 0) {
        Write-Host 'Could not start the PostgreSQL test database container.' -ForegroundColor Red
        return $false
    }

    return $true
}

function Invoke-UiTier {
    Write-Host "`n=== Tier 'ui' (Playwright) ===" -ForegroundColor Cyan
    $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
    $uiDirectory = Join-Path (Join-Path (Join-Path $repoRoot 'Example') 'Tests') 'e2e-ui'

    $passed = $false
    if (Confirm-PostgresAvailable) {
        Push-Location $uiDirectory
        try {
            if (-not (Test-Path (Join-Path $uiDirectory 'node_modules'))) {
                Write-Host 'Installing UI test dependencies (npm ci)...' -ForegroundColor Cyan
                & npm ci | Out-Host
            }

            # No-op when the browser is already installed
            & npx playwright install chromium | Out-Host

            & npm test | Out-Host
            $passed = ($LASTEXITCODE -eq 0)
        }
        finally {
            Pop-Location
        }
    }

    $stopwatch.Stop()
    return [pscustomobject]@{
        Tier    = 'ui'
        Project = 'e2e-ui (Playwright)'
        Passed  = $passed
        Seconds = [math]::Round($stopwatch.Elapsed.TotalSeconds, 1)
    }
}

$tiersToRun = if ($Tier -eq 'all') { @('unit', 'api', 'ui') } else { @($Tier) }

if ($ApiDatabase -eq 'postgres' -and $tiersToRun -contains 'api') {
    Write-Host 'Api tier database: PostgreSQL (starting docker-compose.tests.yml)' -ForegroundColor Cyan
    if (-not (Confirm-PostgresAvailable)) {
        exit 1
    }

    # Picked up by BaseE2ETest configuration (MINDEDTEST_ prefix) in child dotnet test processes
    $env:MINDEDTEST_TestingProfile = 'E2E'
    $env:MINDEDTEST_DatabaseType = 'PostgreSQL'
}

$totalStopwatch = [System.Diagnostics.Stopwatch]::StartNew()

$allResults = @()
foreach ($tierName in $tiersToRun) {
    if ($tierName -eq 'ui') {
        $allResults += Invoke-UiTier
    }
    else {
        $allResults += Invoke-DotnetTestTier -TierName $tierName
    }
}

$totalStopwatch.Stop()

Write-Host "`n=== Summary ===" -ForegroundColor Cyan
$allResults |
    Select-Object Tier, Project, @{ Name = 'Result'; Expression = { if ($_.Passed) { 'PASSED' } else { 'FAILED' } } }, Seconds |
    Format-Table -AutoSize |
    Out-Host

$failedResults = @($allResults | Where-Object { -not $_.Passed })
$totalMinutes = [math]::Round($totalStopwatch.Elapsed.TotalMinutes, 1)

if ($failedResults.Count -gt 0) {
    Write-Host "FAILED: $($failedResults.Count) of $($allResults.Count) project(s) failed ($totalMinutes min). Results in $resultsRoot" -ForegroundColor Red
    exit 1
}

Write-Host "PASSED: all $($allResults.Count) project(s) ($totalMinutes min). Results in $resultsRoot" -ForegroundColor Green
exit 0
