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

# Copy primary standalone executable to root of publish/
Copy-Item publish\standalone\ROBOCoy.exe publish\ROBOCoy.exe -Force

Write-Host ""
Write-Host "==> Deployment complete!" -ForegroundColor Green
Write-Host "   Standalone (.exe runs on any Windows machine):  $ScriptDir\publish\ROBOCoy.exe"
Write-Host "   Portable (ultra-lightweight ~230KB single file): $ScriptDir\publish\portable\ROBOCoy.exe"
