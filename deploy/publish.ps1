<#
.SYNOPSIS
    Build a deployable package of Sommerhus on your own machine.

.DESCRIPTION
    Runs the test suite, publishes the API and the MVC site in Release, and zips them together
    with the server-side scripts into  artifacts\sommerhus-<version>.zip.

    The server has no internet access and no SDK, so this is the only place a build happens.
    Carry the zip to the server and run deploy.ps1 there.

.EXAMPLE
    .\deploy\publish.ps1
    .\deploy\publish.ps1 -SkipTests        # only while iterating on the packaging itself
#>
[CmdletBinding()]
param(
    [string]$Version,
    [switch]$SkipTests
)

$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
Push-Location $root
try {
    $sha = (git rev-parse --short HEAD).Trim()
    if (-not $Version) { $Version = (Get-Date -Format 'yyyyMMdd-HHmm') + "-$sha" }

    $dirty = git status --porcelain
    if ($dirty) {
        Write-Warning "Working tree has uncommitted changes. The package will not match commit $sha exactly:"
        $dirty | ForEach-Object { Write-Warning "  $_" }
    }

    $stage = Join-Path $root "artifacts\$Version"
    $zip   = Join-Path $root "artifacts\sommerhus-$Version.zip"
    if (Test-Path $stage) { Remove-Item $stage -Recurse -Force }
    New-Item -ItemType Directory -Path $stage | Out-Null

    if (-not $SkipTests) {
        Write-Host "==> Running tests" -ForegroundColor Cyan
        dotnet test "Sommerhus.Api.Tests\Sommerhus.Api.Tests.csproj" -c Release --nologo -v quiet
        if ($LASTEXITCODE -ne 0) { throw "Tests failed. Nothing was packaged." }
    }

    Write-Host "==> Publishing API" -ForegroundColor Cyan
    dotnet publish "Sommerhus.Api\Sommerhus.Api.csproj" -c Release -o "$stage\api" --nologo -v quiet
    if ($LASTEXITCODE -ne 0) { throw "API publish failed." }

    Write-Host "==> Publishing MVC" -ForegroundColor Cyan
    dotnet publish "Sommerhus.Mvc\Sommerhus.Mvc.csproj" -c Release -o "$stage\mvc" --nologo -v quiet
    if ($LASTEXITCODE -ne 0) { throw "MVC publish failed." }

    # Development-only files never belong on the server.
    Remove-Item "$stage\api\appsettings.Development.json", "$stage\api\appsettings.Testing.json", "$stage\mvc\appsettings.Development.json" -ErrorAction SilentlyContinue

    Copy-Item "$PSScriptRoot\deploy.ps1", "$PSScriptRoot\rollback.ps1", "$PSScriptRoot\setup-server.ps1" $stage
    @(
        "Version: $Version"
        "Commit:  $(git rev-parse HEAD)"
        "Built:   $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss') on $env:COMPUTERNAME"
    ) | Set-Content "$stage\VERSION.txt"

    Write-Host "==> Zipping" -ForegroundColor Cyan
    if (Test-Path $zip) { Remove-Item $zip -Force }
    Compress-Archive -Path "$stage\*" -DestinationPath $zip
    Remove-Item $stage -Recurse -Force

    Write-Host ""
    Write-Host "Package ready: $zip" -ForegroundColor Green
    Write-Host "Next: copy it to the server and run  .\deploy.ps1 -Package <path-to-zip>  there (as administrator)."
}
finally {
    Pop-Location
}
