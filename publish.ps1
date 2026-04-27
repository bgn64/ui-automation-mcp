<#
.SYNOPSIS
    Publishes ui-automation-mcp as a self-contained single-file executable, optionally producing a versioned zip.
.PARAMETER OutputDir
    Directory to publish to. Defaults to ./publish.
.PARAMETER Version
    Optional version string. When provided, also produces ./ui-automation-mcp-<Version>-win-x64.zip
    next to OutputDir's parent and writes the path to the $env:GITHUB_OUTPUT 'zip' key when running under GitHub Actions.
#>
param(
    [string]$OutputDir = (Join-Path $PSScriptRoot "publish"),
    [string]$Version
)

$ErrorActionPreference = "Stop"

Write-Host "Publishing ui-automation-mcp to $OutputDir ..." -ForegroundColor Cyan

dotnet publish "$PSScriptRoot\src\UIAutomation.Mcp" `
    -c Release `
    -r win-x64 `
    --self-contained `
    -o $OutputDir

if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed" }

Write-Host "`nPublished successfully to: $OutputDir" -ForegroundColor Green
Write-Host "Executable: $(Join-Path $OutputDir 'ui-automation-mcp.exe')" -ForegroundColor Green

if ($Version) {
    $zipName = "ui-automation-mcp-$Version-win-x64.zip"
    $zipPath = Join-Path (Split-Path -Parent $OutputDir) $zipName
    if (Test-Path $zipPath) { Remove-Item $zipPath -Force }

    Write-Host "`nCreating zip: $zipPath ..." -ForegroundColor Cyan
    Compress-Archive -Path (Join-Path $OutputDir '*') -DestinationPath $zipPath -CompressionLevel Optimal
    Write-Host "Zip created: $zipPath" -ForegroundColor Green

    if ($env:GITHUB_OUTPUT) {
        "zip=$zipPath" | Out-File -FilePath $env:GITHUB_OUTPUT -Append -Encoding utf8
        "zipName=$zipName" | Out-File -FilePath $env:GITHUB_OUTPUT -Append -Encoding utf8
    }
}
