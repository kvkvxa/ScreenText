[CmdletBinding()]
param(
    [string]$Version = "0.1.0",
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"
if ($Version -notmatch '^\d+\.\d+\.\d+$') {
    throw "Version must match semantic form MAJOR.MINOR.PATCH."
}
$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot ".." )).Path
$projectPath = Join-Path $repositoryRoot "src\ScreenText\ScreenText.csproj"
$modelManifest = Join-Path $repositoryRoot "src\ScreenText\Assets\tessdata\SHA256SUMS.txt"
$modelDirectory = Join-Path $repositoryRoot "src\ScreenText\Assets\tessdata"
$artifactRoot = Join-Path $repositoryRoot "artifacts\release"
$publishPath = Join-Path $artifactRoot "ScreenText-v$Version-win-x64"
$zipPath = Join-Path $artifactRoot "ScreenText-v$Version-win-x64.zip"
$checksumPath = "$zipPath.sha256"

New-Item -ItemType Directory -Force -Path $artifactRoot | Out-Null
if (Test-Path -LiteralPath $publishPath) { Remove-Item -LiteralPath $publishPath -Recurse -Force }
if (Test-Path -LiteralPath $zipPath) { Remove-Item -LiteralPath $zipPath -Force }
if (Test-Path -LiteralPath $checksumPath) { Remove-Item -LiteralPath $checksumPath -Force }

foreach ($line in Get-Content -LiteralPath $modelManifest) {
    if ([string]::IsNullOrWhiteSpace($line)) { continue }
    $parts = $line -split '\s+', 2
    if ($parts.Count -ne 2) { throw "Invalid model checksum line: $line" }
    $modelPath = Join-Path $modelDirectory $parts[1].Trim()
    if (-not (Test-Path -LiteralPath $modelPath)) { throw "Missing OCR model: $($parts[1].Trim())" }
    $actualHash = (Get-FileHash -LiteralPath $modelPath -Algorithm SHA256).Hash.ToUpperInvariant()
    if ($actualHash -ne $parts[0].ToUpperInvariant()) { throw "OCR model hash mismatch: $($parts[1].Trim())" }
}

dotnet restore $projectPath --locked-mode --runtime win-x64
if ($LASTEXITCODE -ne 0) {
    throw "dotnet restore failed with exit code $LASTEXITCODE."
}

dotnet publish $projectPath `
    --configuration $Configuration `
    --runtime win-x64 `
    --self-contained true `
    --output $publishPath `
    -p:Version=$Version `
    -p:PublishTrimmed=false `
    -p:PublishSingleFile=false `
    -p:DebugType=None `
    --no-restore
if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish failed with exit code $LASTEXITCODE."
}

if (Test-Path -LiteralPath (Join-Path $publishPath "x86")) {
    Remove-Item -LiteralPath (Join-Path $publishPath "x86") -Recurse -Force
}

foreach ($document in @("README.md", "LICENSE", "THIRD_PARTY_NOTICES.md")) {
    Copy-Item -LiteralPath (Join-Path $repositoryRoot $document) -Destination (Join-Path $publishPath $document)
}

# Compress-Archive on Windows PowerShell 5.1 stores entry names with backslashes,
# which is invalid for Zip files. Build the archive directly with .NET so that
# every entry uses the required forward-slash separators.
Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem
$zip = [System.IO.Compression.ZipFile]::Open($zipPath, [System.IO.Compression.ZipArchiveMode]::Create)
try {
    Get-ChildItem -LiteralPath $publishPath -Recurse -File | ForEach-Object {
        $relativePath = $_.FullName.Substring($publishPath.Length + 1) -replace '\\', '/'
        [System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile(
            $zip, $_.FullName, $relativePath, [System.IO.Compression.CompressionLevel]::Optimal) | Out-Null
    }
}
finally {
    $zip.Dispose()
}
$hash = (Get-FileHash -LiteralPath $zipPath -Algorithm SHA256).Hash.ToLowerInvariant()
"$hash  $(Split-Path $zipPath -Leaf)" | Set-Content -LiteralPath $checksumPath -Encoding ascii

Write-Host "Published: $zipPath"
Write-Host "SHA-256:   $checksumPath"
