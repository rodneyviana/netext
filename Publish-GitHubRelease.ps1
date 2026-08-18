<#
.SYNOPSIS
    Creates and publishes a GitHub release using the version in README.md.

.DESCRIPTION
    Extracts a four-part version from the "LATEST VERSION" heading, creates a
    tag such as v3.0.3.5000, and uploads the specified files with GitHub CLI.
    The release description is built from every commit reachable from the
    current branch's upstream since the previous release tag (not just the
    newest commit), so housekeeping commits made after the real "Version
    x.y.z: ..." commit don't bury it and nothing important gets dropped. If
    there is no previous tag, the full history up to the upstream tip is used.
    Binaries\Install-NetExt.ps1 is always included. If -Files is omitted, the
    matching versioned ZIP under Binaries is also used.

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

$upstream = (& git -C $PSScriptRoot rev-parse --abbrev-ref --symbolic-full-name '@{upstream}' 2>$null | Out-String).Trim()
if ($LASTEXITCODE -ne 0 -or -not $upstream) {
    throw 'The current branch has no upstream. Push it with -u or configure an upstream before creating a release.'
}

$upstreamRemote = $upstream.Split('/', 2)[0]
# --tags: the previous-release lookup below needs release tags to be present locally
& git -C $PSScriptRoot fetch --quiet --tags $upstreamRemote
if ($LASTEXITCODE -ne 0) {
    throw "Failed to refresh remote '$upstreamRemote'."
}

# Release notes = every commit since the previous release tag, not just the newest
# commit - a housekeeping commit made after the real "Version x.y.z: ..." commit
# would otherwise silently become the entire release description.
$previousTag = (& git -C $PSScriptRoot describe --tags --abbrev=0 $upstream 2>$null | Out-String).Trim()
if ($LASTEXITCODE -ne 0 -or -not $previousTag) {
    $range = $upstream
    Write-Host 'No previous tag found; using the full commit history for release notes.'
} else {
    $range = "$previousTag..$upstream"
    Write-Host "Previous tag: $previousTag"
}

# Capturing multi-line `git log` output with `&` returns one array element per
# output LINE. Passing that array where a .NET method expects a string (as the
# old code did with WriteAllText below) makes PowerShell join it with a single
# space via $OFS, silently destroying every line break in the commit message(s).
# Piping through Out-String first collapses it back into one real multi-line
# string, and %x01 (a byte that will never appear in a commit message) safely
# delimits one commit's body from the next regardless of blank lines within it.
$rawLog = (& git -C $PSScriptRoot log $range --format='%B%x01' 2>$null | Out-String)
if ($LASTEXITCODE -ne 0) {
    throw "Could not read the commit log for '$range' from '$upstream'."
}

$commitMessages = @($rawLog -split "\x01" | ForEach-Object { $_.Trim() } | Where-Object { $_ })
if ($commitMessages.Count -eq 0) {
    throw "No commits found in range '$range' on '$upstream'."
}

$releaseNotes = if ($commitMessages.Count -eq 1) {
    $commitMessages[0]
} else {
    Write-Host "Combining $($commitMessages.Count) commits into the release notes."
    ($commitMessages | ForEach-Object { "- $_" }) -join "`n`n"
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