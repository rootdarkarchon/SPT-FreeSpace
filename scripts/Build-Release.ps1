[CmdletBinding()]
param(
    [Parameter()]
    [string] $SptPath = $env:SPT_ROOT,

    [Parameter()]
    [ValidateSet('Release')]
    [string] $Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$version = '1.0.0'
$expectedEftFileVersion = '0.16.9.40087'
$expectedSptAssemblyVersion = [Version] '4.0.13.0'
$repoRoot = [IO.Path]::GetFullPath((Split-Path -Parent $PSScriptRoot))
$solution = Join-Path $repoRoot 'SPT-FreeSpace.slnx'

if ([string]::IsNullOrWhiteSpace($SptPath)) {
    throw 'Set -SptPath or SPT_ROOT to the exact SPT 4.0.13 installation.'
}

$resolvedSptPath = [IO.Path]::GetFullPath($SptPath)
$eftExecutable = Join-Path $resolvedSptPath 'EscapeFromTarkov.exe'
$sptCore = Join-Path $resolvedSptPath 'BepInEx\plugins\spt\spt-core.dll'

if (-not (Test-Path -LiteralPath $eftExecutable -PathType Leaf)) {
    throw "EscapeFromTarkov.exe was not found under: $resolvedSptPath"
}

if (-not (Test-Path -LiteralPath $sptCore -PathType Leaf)) {
    throw "spt-core.dll was not found under: $resolvedSptPath"
}

$eftVersion = [Diagnostics.FileVersionInfo]::GetVersionInfo($eftExecutable).FileVersion
if ($eftVersion -ne $expectedEftFileVersion) {
    throw "Expected EFT file version $expectedEftFileVersion, found '$eftVersion'."
}

$sptVersion = [Reflection.AssemblyName]::GetAssemblyName($sptCore).Version
if ($sptVersion -ne $expectedSptAssemblyVersion) {
    throw "Expected spt-core assembly version $expectedSptAssemblyVersion, found '$sptVersion'."
}

& dotnet build $solution -c $Configuration "-p:SPTPath=$resolvedSptPath"
if ($LASTEXITCODE -ne 0) {
    throw "Release build failed with exit code $LASTEXITCODE."
}

& dotnet test $solution -c $Configuration "-p:SPTPath=$resolvedSptPath" --no-build --no-restore
if ($LASTEXITCODE -ne 0) {
    throw "Unit tests failed with exit code $LASTEXITCODE."
}

$builtDll = Join-Path $repoRoot "src\SPT-FreeSpace\bin\$Configuration\netstandard2.1\SPT-FreeSpace.dll"
if (-not (Test-Path -LiteralPath $builtDll -PathType Leaf)) {
    throw "Built plugin DLL was not found: $builtDll"
}

$distPlugin = Join-Path $repoRoot 'dist\BepInEx\plugins\SPT-FreeSpace'
$distDll = Join-Path $distPlugin 'SPT-FreeSpace.dll'
$stagingRoot = Join-Path $repoRoot "artifacts\staging\SPT-FreeSpace-$version"
$stagingPlugin = Join-Path $stagingRoot 'BepInEx\plugins\SPT-FreeSpace'
$releaseRoot = Join-Path $repoRoot 'artifacts\release'
$zipPath = Join-Path $releaseRoot "SPT-FreeSpace-$version.zip"
$hashPath = "$zipPath.sha256"

function Assert-WorkspaceChild {
    param([Parameter(Mandatory = $true)][string] $Path)

    $fullPath = [IO.Path]::GetFullPath($Path)
    $rootPrefix = $repoRoot.TrimEnd('\') + '\'
    if (-not $fullPath.StartsWith($rootPrefix, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Refusing filesystem operation outside the workspace: $fullPath"
    }
}

Assert-WorkspaceChild -Path $stagingRoot
Assert-WorkspaceChild -Path $distPlugin
Assert-WorkspaceChild -Path $releaseRoot

if (Test-Path -LiteralPath $stagingRoot) {
    Remove-Item -LiteralPath $stagingRoot -Recurse -Force
}

if (Test-Path -LiteralPath $distPlugin) {
    Remove-Item -LiteralPath $distPlugin -Recurse -Force
}

New-Item -ItemType Directory -Path $distPlugin -Force | Out-Null
New-Item -ItemType Directory -Path $stagingPlugin -Force | Out-Null
New-Item -ItemType Directory -Path $releaseRoot -Force | Out-Null

Copy-Item -LiteralPath $builtDll -Destination $distDll -Force
Copy-Item -LiteralPath $builtDll -Destination (Join-Path $stagingPlugin 'SPT-FreeSpace.dll') -Force

if (Test-Path -LiteralPath $zipPath) {
    Remove-Item -LiteralPath $zipPath -Force
}

if (Test-Path -LiteralPath $hashPath) {
    Remove-Item -LiteralPath $hashPath -Force
}

Add-Type -AssemblyName System.IO.Compression.FileSystem
[IO.Compression.ZipFile]::CreateFromDirectory(
    $stagingRoot,
    $zipPath,
    [IO.Compression.CompressionLevel]::Optimal,
    $false)

$archive = [IO.Compression.ZipFile]::OpenRead($zipPath)
try {
    $entries = @($archive.Entries | ForEach-Object { $_.FullName.Replace('\', '/') })
}
finally {
    $archive.Dispose()
}

$expectedEntry = 'BepInEx/plugins/SPT-FreeSpace/SPT-FreeSpace.dll'
if ($entries.Count -ne 1 -or $entries[0] -ne $expectedEntry) {
    throw "Unexpected ZIP layout: $($entries -join ', ')"
}

$hash = (Get-FileHash -LiteralPath $zipPath -Algorithm SHA256).Hash.ToLowerInvariant()
[IO.File]::WriteAllText(
    $hashPath,
    "$hash  SPT-FreeSpace-$version.zip`n",
    [Text.UTF8Encoding]::new($false))

Remove-Item -LiteralPath $stagingRoot -Recurse -Force

Write-Output "Build: $builtDll"
Write-Output 'Tests: passed'
Write-Output "Artifact: $zipPath"
Write-Output "SHA-256: $hash"
Write-Output "ZIP entry: $expectedEntry"
