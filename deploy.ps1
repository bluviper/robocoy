# ROBOCoy Deployment Script
# Builds both Self-Contained Standalone and Portable Single-File releases

$ErrorActionPreference = "Stop"
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Definition
Set-Location $ScriptDir

Write-Host "==> Running Test Suite..." -ForegroundColor Cyan
dotnet test Tests/RobocopyGui.Tests.csproj -c Release --nologo
if ($LASTEXITCODE -ne 0) {
    Write-Error "Tests failed! Aborting deployment."
    exit 1
}

Write-Host "==> Publishing Standalone Release (Zero-dependency, runs everywhere)..." -ForegroundColor Cyan
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o publish\standalone --nologo

Write-Host "==> Publishing Portable Release (Lightweight, requires .NET 9)..." -ForegroundColor Cyan
dotnet publish -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -o publish\portable --nologo

# Copy explicitly named single-file executables to publish/
Copy-Item publish\standalone\ROBOCoy.exe publish\ROBOCoy-Standalone.exe -Force
Copy-Item publish\portable\ROBOCoy.exe publish\ROBOCoy-Portable.exe -Force

Write-Host ""
Write-Host "==> Deployment complete!" -ForegroundColor Green
Write-Host "   Standalone (Runs on any Windows PC, 0 dependencies): $ScriptDir\publish\ROBOCoy-Standalone.exe"
Write-Host "   Portable   (Ultra-lightweight, requires .NET 9):      $ScriptDir\publish\ROBOCoy-Portable.exe"
