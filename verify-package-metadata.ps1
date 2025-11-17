# Verify all package metadata
$projectFiles = Get-ChildItem -Path "Extensions","Framework" -Filter "*.csproj" -Recurse

$requiredProperties = @(
    "Authors",
    "Description",
    "PackageLicenseExpression",
    "PackageIcon",
    "PackageReadme",
    "PackageTags",
    "RepositoryUrl",
    "SymbolPackageFormat"
)

$issues = @()

foreach ($file in $projectFiles) {
    $content = Get-Content $file.FullName -Raw
    $projectName = $file.BaseName
    
    # Check if package is packable
    $isPackable = $content -match '<IsPackable>True</IsPackable>'
    
    if (-not $isPackable) {
        Write-Host "⚠️  $projectName - IsPackable is False (skipping metadata check)" -ForegroundColor Yellow
        continue
    }
    
    Write-Host "`n📦 Checking $projectName..." -ForegroundColor Cyan
    
    $missingProperties = @()
    foreach ($prop in $requiredProperties) {
        if ($content -notmatch "<$prop>") {
            $missingProperties += $prop
        }
    }
    
    if ($missingProperties.Count -gt 0) {
        Write-Host "  ❌ Missing: $($missingProperties -join ', ')" -ForegroundColor Red
        $issues += @{
            Project = $projectName
            Missing = $missingProperties
        }
    } else {
        Write-Host "  ✅ All required metadata present" -ForegroundColor Green
    }
    
    # Check for README file
    $readmePath = Join-Path $file.DirectoryName "README.md"
    if (-not (Test-Path $readmePath)) {
        Write-Host "  ⚠️  README.md file not found" -ForegroundColor Yellow
        $issues += @{
            Project = $projectName
            Missing = @("README.md file")
        }
    }
    
    # Check for icon file
    $iconPath = Join-Path $file.DirectoryName "Minded-128.png"
    if (-not (Test-Path $iconPath)) {
        Write-Host "  ⚠️  Minded-128.png file not found" -ForegroundColor Yellow
        $issues += @{
            Project = $projectName
            Missing = @("Minded-128.png file")
        }
    }
}

Write-Host "`n" -NoNewline
if ($issues.Count -eq 0) {
    Write-Host "✅ All packable projects have complete metadata!" -ForegroundColor Green
} else {
    Write-Host "⚠️  Found $($issues.Count) issue(s) across packages" -ForegroundColor Yellow
}

