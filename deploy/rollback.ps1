<#
.SYNOPSIS
    Put the previously deployed version back. Run as administrator, on the server.

.DESCRIPTION
    deploy.ps1 keeps the version it replaces in <SiteRoot>\previous. This script copies it back
    over the live folders and restarts the pools. Uploads are untouched.

    It does NOT restore the database. If the failed deploy ran a migration you need to undo,
    stop the pools and copy the files from <DataRoot>\backups\<timestamp>\db back into
    <DataRoot>\db by hand first. Every deploy prints its backup folder.

.EXAMPLE
    .\rollback.ps1
#>
#Requires -RunAsAdministrator
[CmdletBinding()]
param(
    [string]$SiteRoot = 'C:\Sites\Sommerhus'
)

$ErrorActionPreference = 'Stop'
Import-Module WebAdministration

$pools = @('SommerhusMvc', 'SommerhusApi')
$prevRoot = Join-Path $SiteRoot 'previous'
if (-not (Test-Path (Join-Path $prevRoot 'mvc\Sommerhus.Mvc.dll'))) { throw "No previous version in $prevRoot." }

function Invoke-Robocopy {
    param([string]$Source, [string]$Destination, [string[]]$ExcludeDirs = @())
    $args = @($Source, $Destination, '/MIR', '/R:2', '/W:2', '/NFL', '/NDL', '/NJH', '/NJS', '/NP')
    if ($ExcludeDirs.Count -gt 0) {
        $args += '/XD'
        $args += ($ExcludeDirs | ForEach-Object { Join-Path $Destination $_ })
    }
    & robocopy @args | Out-Null
    if ($LASTEXITCODE -ge 8) { throw "robocopy $Source -> $Destination failed with exit code $LASTEXITCODE" }
    $global:LASTEXITCODE = 0
}

Write-Host "==> Stopping app pools" -ForegroundColor Cyan
foreach ($pool in $pools) {
    if ((Get-WebAppPoolState -Name $pool).Value -ne 'Stopped') { Stop-WebAppPool -Name $pool }
}
Start-Sleep -Seconds 3

Write-Host "==> Restoring previous version" -ForegroundColor Cyan
Invoke-Robocopy -Source (Join-Path $prevRoot 'mvc') -Destination (Join-Path $SiteRoot 'mvc') -ExcludeDirs @('logs')
Invoke-Robocopy -Source (Join-Path $prevRoot 'api') -Destination (Join-Path $SiteRoot 'api') -ExcludeDirs @('logs', 'wwwroot\uploads')
Remove-Item (Join-Path $SiteRoot 'mvc\app_offline.htm'), (Join-Path $SiteRoot 'api\app_offline.htm') -ErrorAction SilentlyContinue
Remove-Item (Join-Path $SiteRoot 'CURRENT.txt') -ErrorAction SilentlyContinue

Write-Host "==> Starting app pools" -ForegroundColor Cyan
foreach ($pool in $pools) { Start-WebAppPool -Name $pool }

Write-Host ""
Write-Host "Rolled back to the previous version." -ForegroundColor Green
Write-Host "Check the site in a browser. If the database also needs restoring, see the notes at the top of this script."
