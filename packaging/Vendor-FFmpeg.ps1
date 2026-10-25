# Run once on your Windows development machine after a previous successful release.
# Copies already verified LGPL FFmpeg and license from dist/_cache to tracked repo.
$ErrorActionPreference = 'Stop'
$root = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$src = Join-Path $root 'dist\_cache\ffmpeg.exe'
$license = Join-Path $root 'dist\_cache\ffmpeg-license.txt'
$dst = Join-Path $root 'third_party\ffmpeg'
foreach ($f in @($src, $license)) {
    if (-not (Test-Path -LiteralPath $f -PathType Leaf)) { throw "Missing cached prerequisite: $f" }
}
New-Item -Path $dst -ItemType Directory -Force | Out-Null
Copy-Item -LiteralPath $src -Destination (Join-Path $dst 'ffmpeg.exe') -Force
Copy-Item -LiteralPath $license -Destination (Join-Path $dst 'LICENSE.txt') -Force
$exe = Join-Path $dst 'ffmpeg.exe'
$decoders = (& $exe -hide_banner -decoders 2>&1 | Out-String)
if ($LASTEXITCODE -ne 0 -or -not $decoders.Contains('adpcm_ima_ea_eacs')) {
    Remove-Item -LiteralPath $exe -Force
    throw 'FFmpeg binary has no EA EACS decoder. It was removed; nothing committed.'
}
Write-Host 'LGPL source attribution: https://github.com/BtbN/FFmpeg-Builds'
Write-Host 'Confirm LICENSE.txt matches the redistributed build before commit.'
Write-Host ('SHA256: ' + (Get-FileHash -LiteralPath $exe -Algorithm SHA256).Hash)
Write-Host 'Run: git add third_party/ffmpeg/ffmpeg.exe third_party/ffmpeg/LICENSE.txt'
Write-Host 'Then: git commit -m "Vendor FFmpeg LGPL Windows x64 executable and license"'
