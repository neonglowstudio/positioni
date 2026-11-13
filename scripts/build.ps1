param(
    [string]$Dotnet = 'dotnet',
    [string]$OutputDirectory = 'outputs\Portable'
)

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
Push-Location $projectRoot
try {
    $output = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($OutputDirectory)
    $files = Join-Path $output 'Files'
    $env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
    $env:DOTNET_CLI_HOME = Join-Path $projectRoot 'work\dotnet-home'
    & $Dotnet publish 'src\Positioni\Positioni.csproj' -c Release -p:Platform=x64 -p:DebugType=None -o $files -v:minimal
    if ($LASTEXITCODE -ne 0) { throw 'Positioni could not be built.' }

    Copy-Item -LiteralPath @('LICENSE', 'TRADEMARKS.md') -Destination $files
    $notices = Join-Path $files 'Third-party notices'
    New-Item -ItemType Directory -Path $notices -Force | Out-Null
    $runtimeOptions = (Get-Content -LiteralPath (Join-Path $files 'Positioni.runtimeconfig.json') -Raw | ConvertFrom-Json).runtimeOptions
    $runtimeVersion = ($runtimeOptions.includedFrameworks | Where-Object name -eq 'Microsoft.NETCore.App').version
    $runtimePackage = Join-Path $projectRoot "work/packages/microsoft.netcore.app.runtime.win-x64/$runtimeVersion"
    Copy-Item -LiteralPath (Join-Path $runtimePackage 'LICENSE.TXT') -Destination (Join-Path $notices 'dotnet-LICENSE.txt')
    Copy-Item -LiteralPath (Join-Path $runtimePackage 'THIRD-PARTY-NOTICES.TXT') -Destination (Join-Path $notices 'dotnet-NOTICES.txt')

    $appProject = [xml](Get-Content -LiteralPath 'src/Positioni/Positioni.csproj' -Raw)
    $appSdkVersion = ($appProject.Project.ItemGroup.PackageReference | Where-Object Include -eq 'Microsoft.WindowsAppSDK.WinUI').Version
    $appSdk = Join-Path $projectRoot 'work/packages'
    $winUi = Join-Path $appSdk "microsoft.windowsappsdk.winui/$appSdkVersion"
    $appSdkBase = Join-Path $appSdk "microsoft.windowsappsdk.base/$appSdkVersion"
    Copy-Item -LiteralPath (Join-Path $winUi 'license.txt') -Destination (Join-Path $notices 'windows-app-sdk-LICENSE.txt')
    Copy-Item -LiteralPath (Join-Path $winUi 'NOTICE.txt') -Destination (Join-Path $notices 'winui-NOTICE.txt')
    Copy-Item -LiteralPath (Join-Path $appSdkBase 'NOTICE.txt') -Destination (Join-Path $notices 'windows-app-sdk-NOTICE.txt')


    $launcher = Join-Path $output 'Positioni.exe'
    $compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
    $icon = Join-Path $projectRoot 'src\Positioni\Assets\Positioni.ico'
    $source = Join-Path $projectRoot 'src\Positioni.Launcher\Launcher.cs'
    $version = ([xml](Get-Content -LiteralPath 'src\Positioni\Positioni.csproj' -Raw)).Project.PropertyGroup.Version
    $assemblyInfo = Join-Path $output 'LauncherVersion.cs'
    [IO.File]::WriteAllText($assemblyInfo, "[assembly: System.Reflection.AssemblyVersion(""$version.0"")]")
    try {
        & $compiler /nologo /target:winexe /platform:x64 /optimize+ "/win32icon:$icon" "/out:$launcher" $source $assemblyInfo
        if ($LASTEXITCODE -ne 0) { throw 'The portable launcher could not be built.' }

        $updateHelper = Join-Path $files 'Positioni.Update.exe'
        $updateSource = Join-Path $projectRoot 'src\Positioni.Update\UpdateLauncher.cs'
        & $compiler /nologo /target:winexe /platform:x64 /optimize+ "/out:$updateHelper" $updateSource $assemblyInfo
        if ($LASTEXITCODE -ne 0) { throw 'The update helper could not be built.' }
    } finally {
        Remove-Item -LiteralPath $assemblyInfo -Force
    }
    Write-Output $launcher
} finally {
    Pop-Location
}
