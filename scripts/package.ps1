param(
    [string]$Dotnet = 'dotnet',
    [string]$Iscc = 'ISCC.exe',
    [switch]$Release
)

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
$project = Join-Path $projectRoot 'src\Positioni\Positioni.csproj'
$version = ([xml](Get-Content -LiteralPath $project -Raw)).Project.PropertyGroup.Version
if ($version -notmatch '^\d+\.\d+\.\d+$') { throw 'Expected a three-part application version.' }

$workingBuild = Split-Path $projectRoot -Parent
$localLayout = (Split-Path $projectRoot -Leaf) -eq 'Build' -and (Split-Path $workingBuild -Leaf) -eq 'Working build'
$output = if ($localLayout) { Join-Path $workingBuild 'Output' }
    else { Join-Path $projectRoot 'outputs' }
$portable = Join-Path $output 'Portable'
$installerOutput = Join-Path $output 'Installer'
$stage = Join-Path $projectRoot ("work\package-" + [Guid]::NewGuid().ToString('N'))
$publish = Join-Path $stage 'Portable'
$previousPortable = Join-Path $stage 'Previous Portable'
$installer = Join-Path $projectRoot 'installer\Positioni.iss'
$setupName = "positioni-$version-win-x64-installer.exe"
$setup = Join-Path $stage $setupName
$releaseRoot = if ($localLayout) { Join-Path (Split-Path $workingBuild -Parent) 'Releases' }
    else { Join-Path $workingBuild 'Releases' }
$releaseDirectory = Join-Path $releaseRoot $version

if ($Release -and (Test-Path -LiteralPath $releaseDirectory)) {
    throw "The release directory already exists: $releaseDirectory"
}

$preserveStage = $false
Push-Location $projectRoot
try {
    New-Item -ItemType Directory -Force -Path $stage, $installerOutput | Out-Null
    & (Join-Path $PSScriptRoot 'build.ps1') -Dotnet $Dotnet -OutputDirectory $publish | Out-Host
    $installerFiles = Join-Path $publish 'Files'
    & $Iscc "/DAppVersion=$version" "/DPublishDir=$installerFiles" "/O$stage" $installer
    if ($LASTEXITCODE -ne 0) { throw 'The installer could not be built.' }

    if ($Release) {
        $releaseStage = Join-Path $stage 'Release'
        New-Item -ItemType Directory -Path $releaseStage | Out-Null
        $zipFile = Join-Path $releaseStage "positioni-$version-win-x64-portable.zip"
        Add-Type -AssemblyName System.IO.Compression.FileSystem
        $archive = [IO.Compression.ZipFile]::Open($zipFile, [IO.Compression.ZipArchiveMode]::Create)
        try {
            Get-ChildItem -LiteralPath $publish -Recurse -File | ForEach-Object {
                $entry = 'Positioni/' + $_.FullName.Substring($publish.Length + 1).Replace('\', '/')
                $null = [IO.Compression.ZipFileExtensions]::CreateEntryFromFile($archive, $_.FullName, $entry)
            }
        } finally {
            $archive.Dispose()
        }
        Copy-Item -LiteralPath $setup -Destination $releaseStage
    }

    $resolvedOutput = (Resolve-Path -LiteralPath $output).Path
    $resolvedStage = (Resolve-Path -LiteralPath $stage).Path
    $resolvedWork = (Resolve-Path -LiteralPath (Join-Path $projectRoot 'work')).Path
    if ($resolvedOutput -ne $output -or $resolvedStage -ne $stage -or
        -not $resolvedStage.StartsWith($resolvedWork + '\', [StringComparison]::OrdinalIgnoreCase)) {
        throw 'Unexpected build directory.'
    }

    if (Test-Path -LiteralPath $portable) {
        $existing = Get-Item -LiteralPath $portable -Force
        if (-not $existing.PSIsContainer -or ($existing.Attributes -band [IO.FileAttributes]::ReparsePoint)) {
            throw "Unexpected portable directory: $portable"
        }
        [IO.Directory]::Move($portable, $previousPortable)
    }

    $portablePublished = $false
    $setupOutput = Join-Path $installerOutput $setupName
    try {
        [IO.Directory]::Move($publish, $portable)
        $portablePublished = $true
        Move-Item -LiteralPath $setup -Destination $setupOutput -Force
    } catch {
        $preserveStage = $true
        if ($portablePublished) { [IO.Directory]::Move($portable, $publish) }
        if (Test-Path -LiteralPath $previousPortable) {
            [IO.Directory]::Move($previousPortable, $portable)
        }
        $preserveStage = $false
        throw
    }
    Get-ChildItem -LiteralPath $installerOutput -File | Where-Object {
        $_.Name -match '^positioni-\d+\.\d+\.\d+-win-x64-(?:installer|setup)\.exe$' -and $_.Name -ne $setupName
    } | ForEach-Object { Remove-Item -LiteralPath $_.FullName }

    Write-Output (Join-Path $portable 'Positioni.exe')
    Write-Output $setupOutput

    if ($Release) {
        New-Item -ItemType Directory -Force -Path $releaseRoot | Out-Null
        if ((Resolve-Path -LiteralPath $releaseRoot).Path -ne $releaseRoot) {
            throw 'Unexpected release directory.'
        }
        [IO.Directory]::Move($releaseStage, $releaseDirectory)
        Write-Output $releaseDirectory
    }
} finally {
    Pop-Location
    if (-not $preserveStage -and (Test-Path -LiteralPath $stage)) {
        $resolvedStage = (Resolve-Path -LiteralPath $stage).Path
        $workRoot = (Resolve-Path -LiteralPath (Join-Path $projectRoot 'work')).Path
        if ($resolvedStage.StartsWith($workRoot + '\', [StringComparison]::OrdinalIgnoreCase)) {
            Remove-Item -LiteralPath $resolvedStage -Recurse -Force
        }
    }
}
