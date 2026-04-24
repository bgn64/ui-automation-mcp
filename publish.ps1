<#
.SYNOPSIS
    Publishes ui-automation-mcp as a self-contained single-file executable.
.PARAMETER OutputDir
    Directory to publish to. Defaults to ./publish.
#>
param(
    [string]$OutputDir = (Join-Path $PSScriptRoot "publish")
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
