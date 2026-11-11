param([string]$Dotnet = 'dotnet')
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
Push-Location $projectRoot
try {
    $env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
    $env:DOTNET_CLI_HOME = Join-Path $projectRoot 'work\dotnet-home'
    & $Dotnet publish 'src\WindowPositioner\WindowPositioner.csproj' -c Release -p:Platform=x64 -o 'outputs\WindowPositioner' -v:minimal
    if ($LASTEXITCODE -ne 0) { throw 'Window Positioner could not be built.' }
    Write-Output (Join-Path $projectRoot 'outputs\WindowPositioner\WindowPositioner.exe')
} finally { Pop-Location }
