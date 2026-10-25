# OpenNFS1 SE - repeatable Windows x64 public release builder.
# Put this entire 'packaging' directory into the repository root ONCE.
# From then on, run: .\packaging\Build-Release.ps1
# No Git branching, pushing, or publishing is performed by this script.
# Requires Windows + .NET 8 SDK on the build computer; WinRAR is optional.
[CmdletBinding()]
param(
    [switch]$SkipBuild,
    [switch]$NoZip,
    [switch]$NoSfx,
    [switch]$RunTests,
    [ValidateSet('Classic','Modern')][string]$IconStyle = 'Classic',
    [string]$WinRARPath = '',
    [string]$FFmpegExe = '',
    [string]$OutputDirectory = ''
)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'

function Assert-File([string]$Path) {
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) { throw "Required file missing: $Path" }
}
function Invoke-DotNet([string[]]$Arguments) {
    Write-Host ('dotnet ' + ($Arguments -join ' ')) -ForegroundColor DarkCyan
    & dotnet @Arguments
    if ($LASTEXITCODE -ne 0) { throw "dotnet failed ($LASTEXITCODE): $($Arguments -join ' ')" }
}
function Get-WinRAR([string]$CustomPath) {
    if ($CustomPath) {
        Assert-File $CustomPath
        return (Resolve-Path -LiteralPath $CustomPath).Path
    }
    $candidates = @()
    $cmd = Get-Command WinRAR.exe -ErrorAction SilentlyContinue
    if ($cmd) { $candidates += $cmd.Source }
    if ($env:ProgramFiles) { $candidates += (Join-Path $env:ProgramFiles 'WinRAR\WinRAR.exe') }
    if (${env:ProgramFiles(x86)}) { $candidates += (Join-Path ${env:ProgramFiles(x86)} 'WinRAR\WinRAR.exe') }
    foreach ($candidate in $candidates) {
        if (Test-Path -LiteralPath $candidate -PathType Leaf) {
            return (Resolve-Path -LiteralPath $candidate).Path
        }
    }
    return $null
}
function Invoke-WinRAR([string]$Program, [string]$WorkDir, [string]$OutputExe,
                       [string]$Options, [string]$Icon, [string]$Logo, [string]$HiDpiLogo) {
    $argsList = @(
        'a','-cfg-','-r','-y','-m5','-sfxDefault.SFX',
        ('-iicon"{0}"' -f $Icon),
        ('-iimg"{0}"' -f $Logo)
    )
    if ($HiDpiLogo) { $argsList += ('-iimg"{0}"' -f $HiDpiLogo) }
    $argsList += @(
        ('-z"{0}"' -f $Options),
        ('"{0}"' -f $OutputExe),
        '*'
    )
    # WinRAR.exe is a GUI executable; using '&' can return before the archive
    # is actually finished. Start-Process -Wait is important here.
    $process = Start-Process -FilePath $Program -WorkingDirectory $WorkDir `
        -ArgumentList ($argsList -join ' ') -Wait -PassThru
    return $process.ExitCode
}
function Assert-PortableFiles([string]$Directory) {
    foreach ($relative in @(
        'OpenNFS1.exe','OpenNFS1.dll','OpenNFS1.runtimeconfig.json',
        'hostfxr.dll','hostpolicy.dll','MonoGame.Framework.dll',
        'gameconfig.json','Content\ArialBlack.xnb','Content\ColorCorrection.xnb',
        'Tools\ffmpeg.exe'
    )) {
        Assert-File (Join-Path $Directory $relative)
    }
    $files = @(Get-ChildItem -LiteralPath $Directory -Recurse -File)
    # WindowsDX uses Direct3D; SDL2.dll and openal.dll belong to DesktopGL.
    # Validate natives only when the project actually targets DesktopGL.
    $projectText = Get-Content -LiteralPath $proj -Raw
    if ($projectText -match 'MonoGame\.Framework\.WindowsDX') {
        if ($projectText -match 'MonoGame\.Framework\.DesktopGL') {
            throw 'Mixed WindowsDX and DesktopGL references in game project.'
        }
    }
    elseif ($projectText -match 'MonoGame\.Framework\.DesktopGL') {
        foreach ($native in @('SDL2.dll','openal.dll')) {
            if (@($files | Where-Object { $_.Name -ieq $native }).Count -eq 0) {
                throw "Missing native MonoGame DesktopGL runtime: $native"
            }
        }
    }
    else {
        throw 'Unknown MonoGame backend in OpenNFS1.csproj; cannot validate release dependencies.'
    }
    # Do not distribute any files from the user's original commercial CD.
    $retail = @($files | Where-Object {
        $_.Name -ieq 'CENTRAL.QFS' -or
        $_.Extension -in @('.iso','.bin','.cue','.as4','.asf','.tri','.cfm','.bnk','.qfs','.fsh')
    })
    if ($retail.Count -gt 0) {
        throw "Retail NFS SE data found in distribution: $($retail[0].FullName)"
    }
    if ((Test-Path -LiteralPath (Join-Path $Directory 'FRONTEND')) -or
        (Test-Path -LiteralPath (Join-Path $Directory 'SIMDATA'))) {
        throw 'Retail CD folders (FRONTEND/SIMDATA) found in distribution.'
    }
}

$root = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..')).Path
$proj = Join-Path $root 'OpenNFS1\OpenNFS1.csproj'
Assert-File $proj
if (-not $OutputDirectory) { $OutputDirectory = Join-Path $root 'dist' }
$OutputDirectory = [IO.Path]::GetFullPath($OutputDirectory)
[xml]$projectXml = Get-Content -LiteralPath $proj -Raw
$versionValues = @($projectXml.Project.PropertyGroup | ForEach-Object { [string]$_.Version } | Where-Object { $_ })
if ($versionValues.Count -eq 0) { throw "Project version not found in $proj" }
$version = $versionValues[0]
if ($version -notmatch '^\d+\.\d+\.\d+(?:[-+][0-9A-Za-z.-]+)?$') { throw "Invalid project version: $version" }
if ($NoZip -and $NoSfx) { throw 'Both ZIP and SFX are disabled. Nothing to build.' }
if ([Environment]::OSVersion.Platform -ne [PlatformID]::Win32NT) {
    throw 'Run this release builder on Windows (MonoGame/Windows Forms project).'
}

$setup = Join-Path $PSScriptRoot 'setup'
$iconName = if ($IconStyle -eq 'Classic') { 'nfsse_classic.ico' } else { 'nfsse_modern.ico' }
$icon = Join-Path $setup $iconName
$logo = Join-Path $setup 'nfsse_sfx_logo.png'
$logoHd = Join-Path $setup 'nfsse_sfx_logo_hd.png'
Assert-File $icon
if (-not $NoSfx) { Assert-File $logo; Assert-File $logoHd }

$baseName = 'Need_For_Speed_SE_v' + $version + '_win-x64'
$stage = Join-Path $OutputDirectory ('_stage\' + $baseName)
$zip = Join-Path $OutputDirectory ($baseName + '.zip')
$sfx = Join-Path $OutputDirectory ($baseName + '_Setup.exe')
$cache = Join-Path $OutputDirectory '_cache'
New-Item -Path $OutputDirectory, $cache -ItemType Directory -Force | Out-Null
Write-Host "`n=== NFS SE v$version | Windows x64 | portable release ===" -ForegroundColor Cyan
Write-Host "Repo: $root"
Write-Host "Output: $OutputDirectory"
Write-Host "Mode: $(if ($SkipBuild) {'reuse staged build'} else {'compile current source'})"

if (-not $SkipBuild) {
    if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
        throw 'dotnet not found. Install the .NET 8 SDK on the build computer.'
    }
    $sdkList = @(& dotnet --list-sdks)
    if (-not @($sdkList | Where-Object { $_ -match '^8\.' }).Count) {
        throw 'The .NET 8 SDK is required on this PC; players do not need it.'
    }

    # Cleaning only our own versioned staging directory. Never touch original game data.
    if (Test-Path -LiteralPath $stage) { Remove-Item -LiteralPath $stage -Recurse -Force }
    New-Item -Path $stage -ItemType Directory -Force | Out-Null
    Push-Location $root
    try {
        Invoke-DotNet @('tool','restore')
        Invoke-DotNet @('restore',$proj,'--runtime','win-x64')
        if ($RunTests) {
            foreach ($name in @('IsoImporterSmokeTests','RaceGateTests','TournamentTests',
                'VehicleCollisionTests','RaceModeTests','ShowroomNavigationTests',
                'DisplayPresentationTests','OriginalFrontendTests')) {
                $testProj = Join-Path $root ("tests\$name\$name.csproj")
                Assert-File $testProj
                Invoke-DotNet @('run','--project',$testProj,'--configuration','Release')
            }
        }
        Invoke-DotNet @(
            'publish',$proj,'--configuration','Release','--runtime','win-x64',
            '--self-contained','true','--no-restore',
            '-p:PublishSingleFile=false','-p:DebugType=none','-p:DebugSymbols=false',
            ('-p:ApplicationIcon=' + $icon),
            '--output',$stage
        )
    } finally { Pop-Location }

    New-Item -Path (Join-Path $stage 'Tools') -ItemType Directory -Force | Out-Null
    $ffmpegDest = Join-Path $stage 'Tools\ffmpeg.exe'
    $vendorDir = Join-Path $root 'third_party\\ffmpeg'
    $vendorExe = Join-Path $vendorDir 'ffmpeg.exe'
    $vendorLicense = Join-Path $vendorDir 'LICENSE.txt'
    if ($FFmpegExe) {
        Assert-File $FFmpegExe
        Copy-Item -LiteralPath $FFmpegExe -Destination $ffmpegDest -Force
        Write-Warning 'Custom FFmpeg: verify redistributable license and decoder configuration.'
    }
    else {
        Assert-File $vendorExe
        Assert-File $vendorLicense
        Copy-Item -LiteralPath $vendorExe -Destination $ffmpegDest -Force
        Write-Host 'Bundling versioned third_party/ffmpeg/ffmpeg.exe (offline release).' -ForegroundColor Green
    }
    $decoderList = (& $ffmpegDest -hide_banner -decoders 2>&1 | Out-String)
    if ($LASTEXITCODE -ne 0 -or -not $decoderList.Contains('adpcm_ima_ea_eacs')) {
        throw 'Bundled FFmpeg lacks decoder adpcm_ima_ea_eacs. No release was produced.'
    }

    $licenseDir = Join-Path $stage 'THIRD_PARTY_LICENSES'
    New-Item -Path $licenseDir -ItemType Directory -Force | Out-Null
    if (-not $FFmpegExe) {
        Copy-Item -LiteralPath $vendorLicense -Destination (Join-Path $licenseDir 'FFmpeg-LICENSE.txt') -Force
    }
    @'
FFmpeg is an independently executed program used to convert original EA EACS music.
Upstream: https://ffmpeg.org/  License: https://ffmpeg.org/legal.html
Binary source (LGPL build): https://github.com/BtbN/FFmpeg-Builds
Bundled repository binary: third_party/ffmpeg/ffmpeg.exe (LGPL build)
Any original EA game/music files must be supplied separately by the player.
'@ | Set-Content -LiteralPath (Join-Path $licenseDir 'FFmpeg-README.txt') -Encoding UTF8
    foreach ($doc in @('readme.md','CREDITS.md')) {
        if (Test-Path -LiteralPath (Join-Path $root $doc)) {
            Copy-Item -LiteralPath (Join-Path $root $doc) -Destination (Join-Path $stage $doc) -Force
        }
    }
    @"
OPENNFS1 SE v$version - UNOFFICIAL FAN REIMPLEMENTATION

1. Extract the entire ZIP to a writable folder on Windows 10/11 x64.
2. Start OpenNFS1.exe.
3. On first start press Enter / I and select your own NFS Special Edition ISO.
   ISO files are imported internally: no DOSBox or virtual CD drive required.
4. You can instead copy original CD FRONTEND and SIMDATA directories into
   the CD_Data directory alongside OpenNFS1.exe.
5. Music decoder Tools\ffmpeg.exe is included. The first music playback creates
   a WAV cache under %LOCALAPPDATA%\OpenNFS1\MusicCache.

No separately installed .NET runtime, .NET SDK, or FFmpeg is required.
Original commercial game assets are NOT included in this release.
Settings and saves: %LOCALAPPDATA%\OpenNFS1
Crash diagnostics: %LOCALAPPDATA%\OpenNFS1\exception.txt
Project: https://github.com/luboshorak/open-need-for-speed-se
Not affiliated with or endorsed by Electronic Arts.
"@ | Set-Content -LiteralPath (Join-Path $stage 'README-FIRST.txt') -Encoding UTF8
    $stageSetup = Join-Path $stage 'setup'
    New-Item -Path $stageSetup -ItemType Directory -Force | Out-Null
    Copy-Item -LiteralPath $icon -Destination (Join-Path $stageSetup 'nfsse.ico') -Force
    # The same staged folder is used for BOTH ZIP and SFX.
    Assert-PortableFiles $stage
    $hashes = @(
        Get-ChildItem -LiteralPath $stage -Recurse -File | Sort-Object FullName | ForEach-Object {
            $relative = $_.FullName.Substring($stage.Length + 1)
            '{0}  {1}' -f (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash, $relative
        }
    )
    [IO.File]::WriteAllLines((Join-Path $stage 'SHA256SUMS.txt'), $hashes, [Text.Encoding]::UTF8)
}
else {
    if (-not (Test-Path -LiteralPath $stage -PathType Container)) {
        throw "There is no staged build for v$version. First run without -SkipBuild. Expected: $stage"
    }
    Assert-PortableFiles $stage
}

if (-not $NoZip) {
    Write-Host "Creating portable ZIP: $zip" -ForegroundColor Cyan
    # Flat archive: OpenNFS1.exe is at the ZIP root, like Spellcross Reloaded.
    Compress-Archive -Path (Join-Path $stage '*') -DestinationPath $zip -CompressionLevel Optimal -Force
    Assert-File $zip
    Write-Host "ZIP OK: $zip" -ForegroundColor Green
}

if (-not $NoSfx) {
    $rar = Get-WinRAR $WinRARPath
    if (-not $rar) {
        Write-Warning 'WinRAR.exe not found. ZIP is ready; SFX EXE was skipped. Install WinRAR or pass -WinRARPath.'
    }
    else {
        Assert-File (Join-Path (Split-Path -Parent $rar) 'Default.SFX')
        $tempDir = Join-Path $OutputDirectory ('_sfx-work-' + [Guid]::NewGuid().ToString('N'))
        New-Item -Path $tempDir -ItemType Directory -Force | Out-Null
        $tempExe = Join-Path $tempDir ($baseName + '_Setup.exe')
        $options = Join-Path $tempDir 'options.txt'
        # ASCII SFX comments work with both older and newer WinRAR versions.
        @"
;The comment below contains SFX script commands
Path=%USERPROFILE%\Games\Need for Speed SE
SavePath
Title=Need for Speed Special Edition v$version
Text
{
OpenNFS1 SE v$version - unofficial fan reimplementation.

Choose an extraction folder and click Install.
Then launch OpenNFS1.exe and select your own original NFS SE ISO.
No external .NET runtime, SDK or music codecs required.
Original copyrighted game data is not included.
}
Overwrite=1
Shortcut=D,OpenNFS1.exe,,Need for Speed SE,Need for Speed SE,setup\nfsse.ico
"@ | Set-Content -LiteralPath $options -Encoding ASCII
        try {
            Write-Host "Creating WinRAR SFX: $sfx" -ForegroundColor Cyan
            $exitCode = Invoke-WinRAR $rar $stage $tempExe $options $icon $logo $logoHd
            if ($exitCode -ne 0) {
                Write-Warning "WinRAR rejected high-DPI logo (exit $exitCode); retrying with standard logo only."
                if (Test-Path -LiteralPath $tempExe) { Remove-Item -LiteralPath $tempExe -Force }
                $exitCode = Invoke-WinRAR $rar $stage $tempExe $options $icon $logo ''
            }
            if ($exitCode -ne 0) { throw "WinRAR failed with exit code $exitCode" }
            Assert-File $tempExe
            $stream = [IO.File]::OpenRead($tempExe)
            try {
                $sig = New-Object byte[] 2
                if ($stream.Read($sig, 0, 2) -ne 2 -or $sig[0] -ne 0x4d -or $sig[1] -ne 0x5a) {
                    throw 'WinRAR output does not have a valid EXE (MZ) header.'
                }
            } finally { $stream.Dispose() }
            Move-Item -LiteralPath $tempExe -Destination $sfx -Force
            Write-Host "SFX OK: $sfx" -ForegroundColor Green
        }
        finally {
            Remove-Item -LiteralPath $tempDir -Recurse -Force -ErrorAction SilentlyContinue
        }
    }
}
Write-Host "`nDONE: OpenNFS1 SE v$version. Only upload tested release archives." -ForegroundColor Green
