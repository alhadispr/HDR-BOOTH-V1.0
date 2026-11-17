[CmdletBinding()]
param(
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64"
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot
$project = Join-Path $repoRoot "src/HdrBooth/HdrBooth.csproj"
$publishDir = Join-Path $repoRoot "artifacts/HDR-BOOTH-$Runtime"
$zipPath = Join-Path $repoRoot "artifacts/HDR-BOOTH-$Runtime.zip"

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    throw ".NET 8 SDK belum terpasang. Instal .NET 8 SDK dan jalankan ulang skrip ini."
}
if (-not (Test-Path $project)) {
    throw "Project file tidak ditemukan: $project"
}

dotnet --info
dotnet restore $project
if ($LASTEXITCODE -ne 0) { throw "NuGet restore gagal." }

dotnet publish $project -c $Configuration -r $Runtime --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:EnableCompressionInSingleFile=true `
    -p:DebugType=None `
    -p:DebugSymbols=false `
    -o $publishDir
if ($LASTEXITCODE -ne 0) { throw "Build/publish gagal. Periksa error di atas." }

$exe = Join-Path $publishDir "HDR-BOOTH.exe"
if (-not (Test-Path $exe)) {
    throw "Build selesai tetapi HDR-BOOTH.exe tidak ditemukan di $publishDir"
}

New-Item -ItemType Directory -Force -Path (Split-Path -Parent $zipPath) | Out-Null
if (Test-Path $zipPath) { Remove-Item $zipPath -Force }
Compress-Archive -Path (Join-Path $publishDir "*") -DestinationPath $zipPath -Force

Write-Host ""
Write-Host "BUILD BERHASIL"
Write-Host "EXE : $exe"
Write-Host "ZIP : $zipPath"
