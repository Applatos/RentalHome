<#
.SYNOPSIS
    One-time setup of a Windows/IIS server for Sommerhus. Run as administrator, on the server.

.DESCRIPTION
    Creates everything deploy.ps1 expects, and nothing else:

      C:\Sites\Sommerhus\mvc        IIS site "Sommerhus", app pool SommerhusMvc
      C:\Sites\Sommerhus\api        IIS application "/api" under that site, app pool SommerhusApi
      C:\Data\Sommerhus\db          the SQLite database (created by the API on first start)
      C:\Data\Sommerhus\backups     one folder per deploy
      C:\Data\Sommerhus\releases    unpacked packages

    Secrets and environment go on the app pools as environment variables. That is the one
    place a deploy cannot overwrite: web.config is replaced every time, the pool is not.

    Safe to run again: existing pools, sites and variables are updated, not duplicated.

.PARAMETER HostName
    The DNS name the site answers on, e.g. sommerhus.firma.dk. Used for the IIS binding and
    for the URL the MVC site calls the API on.

.PARAMETER AdminPassword
    Password of the seeded "admin" account. Must satisfy the Identity rules: 8+ characters,
    a digit and an upper-case letter.

.PARAMETER JwtKey
    Signing key for auth tokens. Generated randomly when omitted; you never need to know it.

.PARAMETER CertThumbprint
    Thumbprint of a certificate in LocalMachine\My. When given, an https binding is added and
    the MVC site calls the API over https. Without it the site is http only.

.EXAMPLE
    .\setup-server.ps1 -HostName sommerhus.firma.dk -AdminPassword 'Choose1Strong!'
    .\setup-server.ps1 -HostName sommerhus.firma.dk -AdminPassword 'Choose1Strong!' -CertThumbprint 3F2A...
#>
#Requires -RunAsAdministrator
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)] [string]$HostName,
    [Parameter(Mandatory = $true)] [string]$AdminPassword,
    [string]$JwtKey,
    [string]$CertThumbprint,
    [string]$SiteRoot = 'C:\Sites\Sommerhus',
    [string]$DataRoot = 'C:\Data\Sommerhus',
    [string]$SiteName = 'Sommerhus'
)

$ErrorActionPreference = 'Stop'

# ---------------------------------------------------------------- prerequisites
$ancm = Join-Path $env:ProgramFiles 'IIS\Asp.Net Core Module\V2\aspnetcorev2.dll'
if (-not (Test-Path $ancm)) {
    throw "The .NET 8 Hosting Bundle is not installed. Download 'ASP.NET Core 8.0 Runtime - Windows Hosting Bundle' from https://dotnet.microsoft.com/download/dotnet/8.0 on a machine with internet, copy it here, install it, then run this script again."
}
try { Import-Module WebAdministration } catch {
    throw "IIS management tools are missing. Run:  Enable-WindowsOptionalFeature -Online -FeatureName IIS-WebServerRole,IIS-WebServer,IIS-ManagementScriptingTools,IIS-ManagementConsole  and then run this script again."
}
$appcmd = Join-Path $env:windir 'system32\inetsrv\appcmd.exe'

if (-not $JwtKey) {
    $bytes = New-Object byte[] 48
    [System.Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($bytes)
    $JwtKey = [Convert]::ToBase64String($bytes)
}
$scheme = if ($CertThumbprint) { 'https' } else { 'http' }

$mvcPath = Join-Path $SiteRoot 'mvc'
$apiPath = Join-Path $SiteRoot 'api'

# ---------------------------------------------------------------- folders
Write-Host "==> Folders" -ForegroundColor Cyan
foreach ($dir in @($mvcPath, "$mvcPath\logs", $apiPath, "$apiPath\logs", "$apiPath\wwwroot\uploads",
                   "$DataRoot\db", "$DataRoot\backups", "$DataRoot\releases")) {
    New-Item -ItemType Directory -Path $dir -Force | Out-Null
}

# ---------------------------------------------------------------- app pools
function Ensure-AppPool([string]$Name) {
    if (-not (Test-Path "IIS:\AppPools\$Name")) {
        New-WebAppPool -Name $Name | Out-Null
        Write-Host "    created pool $Name"
    }
    # "No Managed Code": the .NET 8 runtime is loaded by the ASP.NET Core module, not by IIS.
    Set-ItemProperty "IIS:\AppPools\$Name" -Name managedRuntimeVersion -Value ''
    Set-ItemProperty "IIS:\AppPools\$Name" -Name startMode -Value 'AlwaysRunning'
    Set-ItemProperty "IIS:\AppPools\$Name" -Name processModel.idleTimeout -Value ([TimeSpan]::Zero)
    Set-ItemProperty "IIS:\AppPools\$Name" -Name autoStart -Value $true
}

function Set-PoolEnvironment([string]$Pool, [hashtable]$Variables) {
    # appcmd is the reliable way to edit a pool's environmentVariables collection from a script.
    foreach ($name in $Variables.Keys) {
        & $appcmd set config -section:system.applicationHost/applicationPools `
            "/-[name='$Pool'].environmentVariables.[name='$name']" /commit:apphost 2>&1 | Out-Null
        & $appcmd set config -section:system.applicationHost/applicationPools `
            "/+[name='$Pool'].environmentVariables.[name='$name',value='$($Variables[$name])']" /commit:apphost | Out-Null
        if ($LASTEXITCODE -ne 0) { throw "Could not set $name on pool $Pool." }
    }
}

Write-Host "==> App pools" -ForegroundColor Cyan
Ensure-AppPool 'SommerhusMvc'
Ensure-AppPool 'SommerhusApi'

Set-PoolEnvironment 'SommerhusMvc' @{
    ASPNETCORE_ENVIRONMENT = 'Production'
    # Trailing slash matters: without it HttpClient drops the /api segment.
    Api__BaseUrl           = "$scheme`://$HostName/api/"
}
Set-PoolEnvironment 'SommerhusApi' @{
    ASPNETCORE_ENVIRONMENT     = 'Production'
    ConnectionStrings__Default = "Data Source=$DataRoot\db\sommerhus.db"
    DefaultAdmin__Password     = $AdminPassword
    Jwt__Key                   = $JwtKey
}

# ---------------------------------------------------------------- site + /api application
Write-Host "==> Site $SiteName ($HostName)" -ForegroundColor Cyan
$site = Get-Website -Name $SiteName -ErrorAction SilentlyContinue
if (-not $site) {
    New-Website -Name $SiteName -PhysicalPath $mvcPath -ApplicationPool 'SommerhusMvc' -HostHeader $HostName -Port 80 | Out-Null
    Write-Host "    created site with http binding $HostName`:80"
}
else {
    Set-ItemProperty "IIS:\Sites\$SiteName" -Name physicalPath -Value $mvcPath
    Set-ItemProperty "IIS:\Sites\$SiteName" -Name applicationPool -Value 'SommerhusMvc'
    if (-not (Get-WebBinding -Name $SiteName -Protocol http -HostHeader $HostName -Port 80)) {
        New-WebBinding -Name $SiteName -Protocol http -HostHeader $HostName -Port 80
    }
}

if ($CertThumbprint) {
    $cert = Get-Item "Cert:\LocalMachine\My\$CertThumbprint" -ErrorAction SilentlyContinue
    if (-not $cert) { throw "No certificate with thumbprint $CertThumbprint in LocalMachine\My." }
    if (-not (Get-WebBinding -Name $SiteName -Protocol https -HostHeader $HostName -Port 443)) {
        New-WebBinding -Name $SiteName -Protocol https -HostHeader $HostName -Port 443 -SslFlags 1
    }
    $binding = Get-WebBinding -Name $SiteName -Protocol https -HostHeader $HostName -Port 443
    $binding.AddSslCertificate($CertThumbprint, 'My')
    Write-Host "    https binding bound to certificate $($cert.Subject)"
}

$app = Get-WebApplication -Site $SiteName -Name 'api' -ErrorAction SilentlyContinue
if (-not $app) {
    New-WebApplication -Site $SiteName -Name 'api' -PhysicalPath $apiPath -ApplicationPool 'SommerhusApi' | Out-Null
    Write-Host "    created application /api"
}
else {
    Set-ItemProperty "IIS:\Sites\$SiteName\api" -Name physicalPath -Value $apiPath
    Set-ItemProperty "IIS:\Sites\$SiteName\api" -Name applicationPool -Value 'SommerhusApi'
}

# ---------------------------------------------------------------- permissions
# Each pool runs as the virtual account "IIS AppPool\<name>". It must be able to write where the
# app writes: logs for both, and the database and uploads for the API. Everything else is read.
Write-Host "==> Permissions" -ForegroundColor Cyan
& icacls $SiteRoot /grant "IIS AppPool\SommerhusMvc:(OI)(CI)RX" "IIS AppPool\SommerhusApi:(OI)(CI)RX" /T /Q | Out-Null
& icacls "$mvcPath\logs" /grant "IIS AppPool\SommerhusMvc:(OI)(CI)M" /Q | Out-Null
& icacls "$apiPath\logs" /grant "IIS AppPool\SommerhusApi:(OI)(CI)M" /Q | Out-Null
& icacls "$apiPath\wwwroot\uploads" /grant "IIS AppPool\SommerhusApi:(OI)(CI)M" /Q | Out-Null
& icacls "$DataRoot\db" /grant "IIS AppPool\SommerhusApi:(OI)(CI)M" /Q | Out-Null

# ---------------------------------------------------------------- summary
Write-Host ""
Write-Host "Server is ready." -ForegroundColor Green
Write-Host "  Site:       $scheme`://$HostName      (files: $mvcPath)"
Write-Host "  API:        $scheme`://$HostName/api  (files: $apiPath)"
Write-Host "  Database:   $DataRoot\db\sommerhus.db  (created on first API start)"
Write-Host "  Admin user: admin / <the password you passed>"
Write-Host ""
Write-Host "Next: build a package with deploy\publish.ps1 on your own machine, copy the zip here,"
Write-Host "and run  .\deploy.ps1 -Package <zip>  from inside it (or from any copy of deploy.ps1)."
