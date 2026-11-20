#!/usr/bin/env pwsh
# Builds the programs against the Maniacal repository as it is now, instead of the release on
# nuget.org: packs Maniacal.Scumm from the repository into .local/feed under a version of its own
# (the repository's, with -local.<date and time> for its prerelease part), and restores it into a
# package folder of its own, .local/packages, so it never meets the release in NuGet's cache.
#
#   ./use-local.ps1                      pack the repository at ../maniacal and build against it
#   ./use-local.ps1 -Repo <folder>       the repository is elsewhere
#   ./use-local.ps1 -Released            back to the release on nuget.org (.local is removed)
param(
    [string] $Repo = (Join-Path $PSScriptRoot '../maniacal'),
    [switch] $Released
)

$ErrorActionPreference = 'Stop'
$local = Join-Path $PSScriptRoot '.local'
if (Test-Path $local) { Remove-Item -Recurse -Force $local }

if (-not $Released) {
    $project = Join-Path $Repo 'src/Maniacal.Scumm/Maniacal.Scumm.csproj'
    if (-not (Test-Path $project)) { throw "No Maniacal repository at $Repo (no $project)" }
    $Repo = (Resolve-Path $Repo).Path

    $suffix = 'local.' + (Get-Date -Format 'yyyyMMddHHmmss')
    $feed = Join-Path $local 'feed'
    & dotnet pack $project -c Release -o $feed "-p:VersionSuffix=$suffix"
    if ($LASTEXITCODE -ne 0) { throw "dotnet pack exited with $LASTEXITCODE" }

    $package = Get-ChildItem $feed -Filter 'Maniacal.Scumm.*.nupkg' | Where-Object Name -NotLike '*.symbols.nupkg' | Select-Object -First 1
    $version = $package.BaseName.Substring('Maniacal.Scumm.'.Length)
    @"
<Project>
  <!-- written by use-local.ps1: Maniacal.Scumm packed from $Repo -->
  <PropertyGroup>
    <ManiacalVersion>$version</ManiacalVersion>
    <RestoreAdditionalProjectSources>`$(MSBuildThisFileDirectory)feed</RestoreAdditionalProjectSources>
    <RestorePackagesPath>`$(MSBuildThisFileDirectory)packages</RestorePackagesPath>
  </PropertyGroup>
</Project>
"@ | Set-Content -Path (Join-Path $local 'Maniacal.props') -Encoding utf8
}

& dotnet build (Join-Path $PSScriptRoot 'Playground.slnx')
if ($LASTEXITCODE -ne 0) { throw "dotnet build exited with $LASTEXITCODE" }
if ($Released) { 'Built against the release on nuget.org.' } else { "Built against Maniacal.Scumm $version, packed from $Repo." }
