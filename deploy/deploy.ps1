<#
.SYNOPSIS
    Install a Sommerhus package on the IIS server. Run as administrator, on the server.

.DESCRIPTION
    Steps, in order:
      1. Unpack the zip under <DataRoot>\releases\<name>.
      2. Take the sites offline and stop both app pools, so no file is locked and nothing
         writes to the database while it is copied.
      3. Back up the SQLite database and the uploaded images to <DataRoot>\backups\<timestamp>.
         Migrations run when the API starts, so this is the safety net for a bad migration.
      4. Keep the currently deployed files in <SiteRoot>\previous, for rollback.ps1.
      5. Mirror the new files in. Uploads and logs are never touched.
      6. Start the pools, wait for both sites to answer, and only then remove app_offline.htm.

    If the health check fails, the sites stay offline and the script tells you to run
    rollback.ps1. Nothing else is undone automatically.

.EXAMPLE
    .\deploy.ps1 -Package C:\Data\Sommerhus\releases\sommerhus-20260922-1530-390f1e3.zip
#>
#Requires -RunAsAdministrator
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$Package,
    [string]$SiteRoot = 'C:\Sites\Sommerhus',
    [string]$DataRoot = 'C:\Data\Sommerhus',
    [string]$SiteName = 'Sommerhus',
    [switch]$SkipHealthCheck
)

$ErrorActionPreference = 'Stop'
Import-Module WebAdministration

$pools = @('SommerhusMvc', 'SommerhusApi')
$mvcPath = Join-Path $SiteRoot 'mvc'
$apiPath = Join-Path $SiteRoot 'api'
$prevRoot = Join-Path $SiteRoot 'previous'
$stamp = Get-Date -Format 'yyyyMMdd-HHmmss'

function Invoke-Robocopy {
    param([string]$Source, [string]$Destination, [string[]]$ExcludeDirs = @())
    $args = @($Source, $Destination, '/MIR', '/R:2', '/W:2', '/NFL', '/NDL', '/NJH', '/NJS', '/NP')
    if ($ExcludeDirs.Count -gt 0) {
        $args += '/XD'
        $args += ($ExcludeDirs | ForEach-Object { Join-Path $Destination $_ })
    }
    & robocopy @args | Out-Null
    # robocopy: 0-7 are success codes (0 = nothing to do, 1 = copied), 8+ are failures.
    if ($LASTEXITCODE -ge 8) { throw "robocopy $Source -> $Destination failed with exit code $LASTEXITCODE" }
    $global:LASTEXITCODE = 0
}

function Set-Offline([bool]$offline) {
    foreach ($dir in @($mvcPath, $apiPath)) {
        $file = Join-Path $dir 'app_offline.htm'
        if ($offline) {
            New-Item -ItemType Directory -Path $dir -Force | Out-Null
            Set-Content -Path $file -Value '<html><body style="font-family:sans-serif"><h2>Sommerhus opdateres</h2><p>Siden er tilbage om et &oslash;jeblik.</p></body></html>'
        }
        elseif (Test-Path $file) {
            Remove-Item $file -Force
        }
    }
}

function Test-Url([string]$Url, [string]$HostHeader) {
    # HttpWebRequest rather than Invoke-WebRequest: Windows PowerShell 5.1 refuses to set a
    # Host header on the latter, and the site binding is host-specific.
    try {
        $req = [System.Net.HttpWebRequest]::Create($Url)
        if ($HostHeader) { $req.Host = $HostHeader }
        $req.Timeout = 15000
        $req.AllowAutoRedirect = $false
        $res = $req.GetResponse()
        $code = [int]$res.StatusCode
        $res.Close()
        return ($code -ge 200 -and $code -lt 400)
    }
    catch {
        return $false
    }
}

# ---------------------------------------------------------------- 1. unpack
$Package = (Resolve-Path $Package).Path
if ($Package -like '*.zip') {
    $release = Join-Path $DataRoot ("releases\" + [IO.Path]::GetFileNameWithoutExtension($Package))
    if (Test-Path $release) { Remove-Item $release -Recurse -Force }
    Write-Host "==> Unpacking to $release" -ForegroundColor Cyan
    Expand-Archive -Path $Package -DestinationPath $release
}
else {
    $release = $Package
}
foreach ($required in @('api\Sommerhus.Api.dll', 'mvc\Sommerhus.Mvc.dll', 'VERSION.txt')) {
    if (-not (Test-Path (Join-Path $release $required))) { throw "Not a Sommerhus package: missing $required in $release" }
}
$version = (Get-Content (Join-Path $release 'VERSION.txt') | Select-Object -First 1)
Write-Host "==> Deploying $version" -ForegroundColor Cyan

foreach ($pool in $pools) {
    if (-not (Test-Path "IIS:\AppPools\$pool")) { throw "App pool '$pool' does not exist. Run setup-server.ps1 first." }
}
$site = Get-Website -Name $SiteName -ErrorAction SilentlyContinue
if (-not $site) { throw "Site '$SiteName' does not exist. Run setup-server.ps1 first." }
$hostName = ($site.bindings.Collection | Where-Object { $_.bindingInformation -match ':([^:]+)$' } | ForEach-Object { $Matches[1] } | Select-Object -First 1)

# ---------------------------------------------------------------- 2. offline
Write-Host "==> Taking sites offline" -ForegroundColor Cyan
Set-Offline $true
foreach ($pool in $pools) {
    if ((Get-WebAppPoolState -Name $pool).Value -ne 'Stopped') { Stop-WebAppPool -Name $pool }
}
$tries = 0
while (($pools | ForEach-Object { (Get-WebAppPoolState -Name $_).Value }) -contains 'Stopping' -and $tries -lt 30) {
    Start-Sleep -Seconds 1; $tries++
}

# ---------------------------------------------------------------- 3. backup
$backup = Join-Path $DataRoot "backups\$stamp"
Write-Host "==> Backing up database and uploads to $backup" -ForegroundColor Cyan
New-Item -ItemType Directory -Path "$backup\db" -Force | Out-Null
# The -wal and -shm files hold changes not yet checkpointed into the main file; take them too.
Get-ChildItem (Join-Path $DataRoot 'db') -Filter 'sommerhus.db*' -ErrorAction SilentlyContinue |
    Copy-Item -Destination "$backup\db"
$uploads = Join-Path $apiPath 'wwwroot\uploads'
if (Test-Path $uploads) { Invoke-Robocopy -Source $uploads -Destination "$backup\uploads" }
Get-ChildItem (Join-Path $DataRoot 'backups') -Directory | Sort-Object Name -Descending | Select-Object -Skip 10 |
    Remove-Item -Recurse -Force   # keep the last ten

# ---------------------------------------------------------------- 4. previous
if (Test-Path (Join-Path $mvcPath 'Sommerhus.Mvc.dll')) {
    Write-Host "==> Keeping current version in $prevRoot" -ForegroundColor Cyan
    Invoke-Robocopy -Source $mvcPath -Destination (Join-Path $prevRoot 'mvc') -ExcludeDirs @('logs')
    Invoke-Robocopy -Source $apiPath -Destination (Join-Path $prevRoot 'api') -ExcludeDirs @('logs', 'wwwroot\uploads')
    Remove-Item (Join-Path $prevRoot 'mvc\app_offline.htm'), (Join-Path $prevRoot 'api\app_offline.htm') -ErrorAction SilentlyContinue
}

# ---------------------------------------------------------------- 5. copy
Write-Host "==> Copying new files" -ForegroundColor Cyan
Invoke-Robocopy -Source (Join-Path $release 'mvc') -Destination $mvcPath -ExcludeDirs @('logs')
Invoke-Robocopy -Source (Join-Path $release 'api') -Destination $apiPath -ExcludeDirs @('logs', 'wwwroot\uploads')
Set-Offline $true   # /MIR removed the marker files; put them back until the health check passes
New-Item -ItemType Directory -Path (Join-Path $mvcPath 'logs'), (Join-Path $apiPath 'logs'), $uploads -Force | Out-Null
Copy-Item (Join-Path $release 'VERSION.txt') (Join-Path $SiteRoot 'CURRENT.txt') -Force

# ---------------------------------------------------------------- 6. start + verify
Write-Host "==> Starting app pools" -ForegroundColor Cyan
foreach ($pool in $pools) { Start-WebAppPool -Name $pool }
Set-Offline $false

if (-not $SkipHealthCheck) {
    Write-Host "==> Health check (host: $hostName)" -ForegroundColor Cyan
    $ok = $false
    for ($i = 0; $i -lt 30 -and -not $ok; $i++) {
        Start-Sleep -Seconds 2
        # The API lives under /api and its own routes start with api/, hence /api/api/...
        $ok = (Test-Url 'http://127.0.0.1/api/api/cities' $hostName) -and (Test-Url 'http://127.0.0.1/' $hostName)
    }
    if (-not $ok) {
        Set-Offline $true
        Write-Host ""
        Write-Host "HEALTH CHECK FAILED. Sites are offline." -ForegroundColor Red
        Write-Host "Look at:  $apiPath\logs\stdout*.log  and  $mvcPath\logs\stdout*.log"
        Write-Host "To go back to the previous version:  .\rollback.ps1"
        throw "Deployment of $version did not come up healthy."
    }
}

Write-Host ""
Write-Host "Deployed $version" -ForegroundColor Green
Write-Host "Backup:   $backup"
Write-Host "Previous: $prevRoot  (rollback.ps1 restores it)"
