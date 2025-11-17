# Fix typos in all .csproj files
$typos = @{
    "Clan code" = "Clean code"
    "scaffholding" = "scaffolding"
    "Encapsuplate" = "Encapsulate"
    "dealth" = "dealt"
}

$projectFiles = Get-ChildItem -Path "Extensions","Framework" -Filter "*.csproj" -Recurse

foreach ($file in $projectFiles) {
    $content = Get-Content $file.FullName -Raw
    $modified = $false
    
    foreach ($typo in $typos.GetEnumerator()) {
        if ($content -match [regex]::Escape($typo.Key)) {
            $content = $content -replace [regex]::Escape($typo.Key), $typo.Value
            $modified = $true
            Write-Host "Fixed '$($typo.Key)' in $($file.Name)" -ForegroundColor Green
        }
    }
    
    if ($modified) {
        Set-Content -Path $file.FullName -Value $content -NoNewline
    }
}

Write-Host "`nAll typos fixed!" -ForegroundColor Cyan

