# Build and run EXACT current master in a fresh, versioned directory.
# Never launch a potentially stale bin/, dist/ or desktop shortcut.
[CmdletBinding()]
param(
    [string]$CdDataPath = '',
    [switch]$BuildOnly
)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$root = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..')).Path

if (-not (Get-Command git.exe -ErrorAction SilentlyContinue)) {
    throw 'Git for Windows is required for a verifiable source commit.'
}
$branch = (& git.exe -C $root branch --show-current).Trim()
if ($LASTEXITCODE -ne 0 -or $branch -ne 'master') {
    throw "The checkout is on '$branch'. Run: git switch master"
}
& git.exe -C $root fetch origin master --quiet
if ($LASTEXITCODE -ne 0) { throw 'Git fetch failed; cannot verify the actual master commit.' }
$commit = (& git.exe -C $root rev-parse HEAD).Trim()
$origin = (& git.exe -C $root rev-parse origin/master).Trim()
if ($commit -ne $origin) {
    throw "Local master ($($commit.Substring(0,12))) is not current origin/master ($($origin.Substring(0,12))). Run: git pull --ff-only origin master"
}
$dirty = @(& git.exe -C $root status --short)
if ($dirty.Count -gt 0) {
    Write-Warning 'There are uncommitted changes. They are NOT deleted; the new build may include them.'
    $dirty | ForEach-Object { Write-Warning $_ }
}
[xml]$project = Get-Content -LiteralPath (Join-Path $root 'OpenNFS1\OpenNFS1.csproj') -Raw
$version = [string]$project.Project.PropertyGroup.Version
if ($version -notmatch '^\d+\.\d+\.\d+$') { throw "Unknown project version '$version'." }

function Test-OriginalDataRoot([string]$Folder) {
    return (Test-Path -LiteralPath (Join-Path $Folder 'FRONTEND\ART\CONTROL\CENTRAL.QFS') -PathType Leaf) -and
           (Test-Path -LiteralPath (Join-Path $Folder 'SIMDATA\MISC\AL1.TRI') -PathType Leaf)
}
function Resolve-OriginalDataRoot([string]$Folder) {
    if (-not (Test-Path -LiteralPath $Folder -PathType Container)) { return $null }
    $possible = @($Folder)
    # Only inspect two levels; different original CD editions have NFSSE
    # as a top-level or nested folder. Do not copy or write anything here.
    try {
        $children = @(Get-ChildItem -LiteralPath $Folder -Directory -ErrorAction Stop)
        foreach ($child in $children) {
            $possible += $child.FullName
            foreach ($nested in @(Get-ChildItem -LiteralPath $child.FullName -Directory -ErrorAction Stop)) {
                $possible += $nested.FullName
            }
        }
    } catch [System.UnauthorizedAccessException] {}
      catch [System.IO.IOException] {}
    foreach ($candidate in $possible) {
        if (Test-OriginalDataRoot $candidate) { return $candidate }
    }
    return $null
}
$candidates = if ($CdDataPath) { @([IO.Path]::GetFullPath($CdDataPath)) } else { @(
    (Join-Path $root 'OpenNFS1\CD_Data'),
    (Join-Path $root 'CD_Data'),
    'I:\'
) }
$CdDataPath = $null
foreach ($candidate in $candidates) {
    $resolved = Resolve-OriginalDataRoot $candidate
    if ($resolved) {
        $CdDataPath = $resolved
        break
    }
}
if (-not $CdDataPath) {
    throw 'Original CD data were not found. Use -CdDataPath "I:\" or a directory containing original NFSSE/FRONTEND/SIMDATA. Source is read-only; imports always go to LocalAppData.'
}

$short = $commit.Substring(0, 12)
$stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$out = Join-Path $root ('dist\_verified\' + $version + '-' + $short + '-' + $stamp)
Write-Host "Verified checkout: $branch $commit" -ForegroundColor Cyan
Write-Host "Source version: $version" -ForegroundColor Cyan
Write-Host "Original CD data: $CdDataPath"
Write-Host "FRESH output (never reuses existing EXE): $out"

& (Join-Path $PSScriptRoot 'Build-Release.ps1') -NoSfx -OutputDirectory $out
if (-not $?) { throw 'Release builder failed; game was NOT launched.' }

$stage = Join-Path $out ('_stage\Need_For_Speed_SE_v' + $version + '_win-x64')
$exe = Join-Path $stage 'OpenNFS1.exe'
$dll = Join-Path $stage 'OpenNFS1.dll'
foreach ($file in @($exe, $dll, (Join-Path $stage 'Tools\ffmpeg.exe'))) {
    if (-not (Test-Path -LiteralPath $file -PathType Leaf)) {
        throw "Fresh build is missing: $file"
    }
}
$ver = (Get-Item -LiteralPath $dll).VersionInfo.FileVersion
if ($ver -and -not $ver.StartsWith($version)) {
    throw "WRONG BINARY: DLL file version '$ver', expected $version."
}
$hash = (Get-FileHash -LiteralPath $dll -Algorithm SHA256).Hash
$provenance = @(
    "git-commit=$commit"
    "source-version=$version"
    "built-at=$(Get-Date -Format o)"
    "build-script=$($MyInvocation.MyCommand.Path)"
    "OpenNFS1.dll-sha256=$hash"
) -join [Environment]::NewLine
Set-Content -LiteralPath (Join-Path $stage 'build-provenance.txt') -Value $provenance -Encoding UTF8
Write-Host "EXE: $exe" -ForegroundColor Green
Write-Host "DLL version: $ver"
Write-Host "DLL SHA256: $hash"
Write-Host "Source identity: $commit"

if (-not $BuildOnly) {
    $process = Start-Process -FilePath $exe -WorkingDirectory $stage -ArgumentList @('--data', ('"{0}"' -f $CdDataPath)) -PassThru
    Write-Host "Launched PID $($process.Id). Check game title: Need for Speed SE Reloaded v$version"
    Write-Host ('Exact runtime identity: ' + (Join-Path $env:LOCALAPPDATA 'OpenNFS1\last-launch.txt'))
    Write-Host 'If this new EXE shows the same defect, it is a game implementation bug, not an outdated binary.'
}
