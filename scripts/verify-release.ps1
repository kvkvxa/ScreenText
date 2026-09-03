[CmdletBinding()]
param(
    [string]$Version = "0.1.0",
    [switch]$RequireAuthenticodeSignature
)

$ErrorActionPreference = "Stop"
if ($Version -notmatch '^\d+\.\d+\.\d+$') {
    throw "Version must match semantic form MAJOR.MINOR.PATCH."
}

$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$artifactRoot = Join-Path $repositoryRoot "artifacts\release"
$zipName = "ScreenText-v$Version-win-x64.zip"
$zipPath = Join-Path $artifactRoot $zipName
$checksumPath = "$zipPath.sha256"

if (-not (Test-Path -LiteralPath $zipPath)) { throw "Release ZIP not found: $zipPath" }
if (-not (Test-Path -LiteralPath $checksumPath)) { throw "Release checksum not found: $checksumPath" }

$actualHash = (Get-FileHash -LiteralPath $zipPath -Algorithm SHA256).Hash.ToLowerInvariant()
$expectedRecord = "$actualHash  $zipName"
$recordedHash = (Get-Content -LiteralPath $checksumPath -Raw).Trim()
if ($recordedHash -ne $expectedRecord) { throw "Release checksum does not match the ZIP." }

if ($RequireAuthenticodeSignature) {
    $executablePath = Join-Path $repositoryRoot "artifacts\release\ScreenText-v$Version-win-x64\ScreenText.exe"
    if (-not (Test-Path -LiteralPath $executablePath)) { throw "Published executable not found: $executablePath" }
    $signature = Get-AuthenticodeSignature -LiteralPath $executablePath
    if ($signature.Status -ne "Valid") {
        throw "Authenticode signature is not valid: $($signature.Status)."
    }
}

Add-Type -AssemblyName System.IO.Compression.FileSystem
$archive = [System.IO.Compression.ZipFile]::OpenRead($zipPath)
try {
    $entries = @($archive.Entries | ForEach-Object { $_.FullName })
    foreach ($requiredEntry in @(
        "ScreenText.exe",
        "Assets/tessdata/eng.traineddata",
        "Assets/tessdata/rus.traineddata",
        "Assets/tessdata/SHA256SUMS.txt",
        "README.md",
        "LICENSE",
        "THIRD_PARTY_NOTICES.md"
    )) {
        if ($entries -notcontains $requiredEntry) { throw "Required release entry is missing: $requiredEntry" }
    }

    if ($entries | Where-Object { $_ -match '(^|/)x86(/|$)' }) {
        throw "Release contains unexpected x86 files."
    }
}
finally {
    $archive.Dispose()
}

Write-Host "Release verified: $zipPath"
Write-Host "SHA-256: $actualHash"
