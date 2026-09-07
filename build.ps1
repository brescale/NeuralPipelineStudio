# Neural Pipeline Studio - Automated Build & Package Script
param(
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"
$repoRoot = $PSScriptRoot
$projectPath = Join-Path $repoRoot "src\NeuralPipelineStudio\NeuralPipelineStudio.csproj"
$distDir = Join-Path $repoRoot "dist"

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host "  NEURAL PIPELINE STUDIO - BUILD & PUBLISH SCRIPT" -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

if (Test-Path $distDir) {
    Remove-Item -Path $distDir -Recurse -Force
}
New-Item -ItemType Directory -Path $distDir -Force | Out-Null

Write-Host "`n[1/3] Building solution ($Configuration)..." -ForegroundColor Yellow
dotnet build $projectPath -c $Configuration

Write-Host "`n[2/3] Publishing standalone single-file binary (win-x64)..." -ForegroundColor Yellow
dotnet publish $projectPath -c $Configuration -r win-x64 -p:PublishSingleFile=true --self-contained true -o $distDir

Write-Host "`n[3/3] Packaging presets and documentation..." -ForegroundColor Yellow
$srcPresets = Join-Path $repoRoot "presets"
$dstPresets = Join-Path $distDir "presets"
if (Test-Path $srcPresets) {
    Copy-Item -Path $srcPresets -Destination $dstPresets -Recurse -Force
}

$outputExe = Join-Path $distDir "NeuralPipelineStudio.exe"
if (Test-Path $outputExe) {
    $sizeMB = [math]::Round(((Get-Item $outputExe).Length / 1MB), 2)
    Write-Host "`n[SUCCESS] Production release packaged successfully!" -ForegroundColor Green
    Write-Host "Binary:  $outputExe ($sizeMB MB)" -ForegroundColor Green
    Write-Host "Presets: $dstPresets" -ForegroundColor Green
} else {
    Write-Host "`n[ERROR] Build output executable not found!" -ForegroundColor Red
    exit 1
}
