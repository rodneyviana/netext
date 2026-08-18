<#
.SYNOPSIS
    Creates and publishes a GitHub release using the version in README.md.

.DESCRIPTION
    Extracts a four-part version from the "LATEST VERSION" heading, creates a
    tag such as v3.0.3.5000, and uploads the specified files with GitHub CLI.
    The latest commit message pushed to the current branch's upstream is used
    as the release description. Binaries\Install-NetExt.ps1 is always included.
    If -Files is omitted, the matching versioned ZIP under Binaries is also used.

.EXAMPLE
    .\Publish-GitHubRelease.ps1 -WhatIf

.EXAMPLE
    .\Publish-GitHubRelease.ps1

.EXAMPLE
    .\Publish-GitHubRelease.ps1 -Files .\Binaries\NetExt-3.0.3.5000.zip, .\Binaries\Install-NetExt.ps1
#>
[CmdletBinding(SupportsShouldProcess, ConfirmImpact = 'High')]
param(
    [string] $ReadmePath = (Join-Path $PSScriptRoot 'README.md'),

    [string[]] $Files,

    [string] $Repository,

    [string] $Target,

    [string] $TagPrefix = 'v',

    [string] $ReleaseNamePrefix = 'NetExt'
)

$ErrorActionPreference = 'Stop'

if (-not (Get-Command gh -ErrorAction SilentlyContinue)) {
    throw "GitHub CLI ('gh') is not on PATH. Install it and try again."
}

if (-not (Get-Command git -ErrorAction SilentlyContinue)) {
    throw "Git ('git') is not on PATH. Install it and try again."
}

if (-not (Test-Path -LiteralPath $ReadmePath -PathType Leaf)) {
    throw "README file not found: $ReadmePath"
}

$readme = Get-Content -LiteralPath $ReadmePath -Raw
$versionPattern = '(?m)^#\s*LATEST VERSION:\s*(?<version>\d+\.\d+\.\d+\.\d+)\b'
$versionMatches = [regex]::Matches($readme, $versionPattern)

if ($versionMatches.Count -ne 1) {
    throw "Expected exactly one '# LATEST VERSION: x.x.x.x' heading in '$ReadmePath'; found $($versionMatches.Count)."
}

$version = $versionMatches[0].Groups['version'].Value
$tag = "$TagPrefix$version"
$releaseName = "$ReleaseNamePrefix $tag"

if (-not $Files -or $Files.Count -eq 0) {
    $Files = @((Join-Path $PSScriptRoot "Binaries\NetExt-$version.zip"))
}

$Files = @($Files) + (Join-Path $PSScriptRoot 'Binaries\Install-NetExt.ps1')

$resolvedFiles = foreach ($file in $Files) {
    if (-not (Test-Path -LiteralPath $file -PathType Leaf)) {
        throw "Release asset not found: $file"
    }

    (Resolve-Path -LiteralPath $file).Path
}
$resolvedFiles = @($resolvedFiles | Select-Object -Unique)

$duplicateNames = $resolvedFiles |
    Group-Object -Property { Split-Path -Path $_ -Leaf } |
    Where-Object Count -gt 1
if ($duplicateNames) {
    throw "Release assets must have unique file names. Duplicate: $($duplicateNames.Name -join ', ')"
}

$upstream = (& git -C $PSScriptRoot rev-parse --abbrev-ref --symbolic-full-name '@{upstream}' 2>$null).Trim()
if ($LASTEXITCODE -ne 0 -or -not $upstream) {
    throw 'The current branch has no upstream. Push it with -u or configure an upstream before creating a release.'
}

$upstreamRemote = $upstream.Split('/', 2)[0]
& git -C $PSScriptRoot fetch --quiet $upstreamRemote
if ($LASTEXITCODE -ne 0) {
    throw "Failed to refresh remote '$upstreamRemote'."
}

$releaseNotes = (& git -C $PSScriptRoot log -1 --format='%B' $upstream).Trim()
if ($LASTEXITCODE -ne 0 -or -not $releaseNotes) {
    throw "Could not read the latest commit message from '$upstream'."
}

Write-Host "Version: $version"
Write-Host "Tag:     $tag"
Write-Host "Title:   $releaseName"
Write-Host "Source:  $upstream"
Write-Host 'Description:'
Write-Host $releaseNotes
Write-Host 'Assets:'
$resolvedFiles | ForEach-Object { Write-Host "  $_" }

if ($WhatIfPreference) {
    $null = $PSCmdlet.ShouldProcess($tag, 'Create and publish GitHub release')
    return
}

& gh auth status
if ($LASTEXITCODE -ne 0) {
    throw "GitHub CLI is not authenticated. Run 'gh auth login' and try again."
}

$commonArguments = @()
if ($Repository) {
    $commonArguments += @('--repo', $Repository)
}

& gh release view $tag @commonArguments --json tagName 2>$null
if ($LASTEXITCODE -eq 0) {
    throw "GitHub release '$tag' already exists. No files were uploaded."
}

if ($PSCmdlet.ShouldProcess($tag, 'Create and publish GitHub release')) {
    $notesFile = [System.IO.Path]::GetTempFileName()
    try {
        [System.IO.File]::WriteAllText(
            $notesFile,
            $releaseNotes,
            [System.Text.UTF8Encoding]::new($false)
        )

        $createArguments = @(
            'release', 'create', $tag
            '--title', $releaseName
            '--notes-file', $notesFile
            '--latest'
        )
        $createArguments += $commonArguments

        if ($Target) {
            $createArguments += @('--target', $Target)
        }

        $createArguments += $resolvedFiles

        & gh @createArguments
        if ($LASTEXITCODE -ne 0) {
            throw "GitHub CLI failed to create release '$tag' (exit code $LASTEXITCODE)."
        }
    }
    finally {
        Remove-Item -LiteralPath $notesFile -Force -ErrorAction SilentlyContinue
    }
}