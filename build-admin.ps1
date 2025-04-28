# Check if running as administrator, if not, restart as admin
if (-NOT ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole] "Administrator")) {
    Write-Warning "This script requires administrator privileges. Attempting to restart as administrator..."
    Start-Process powershell.exe "-File `"$PSCommandPath`"" -Verb RunAs
    exit
}

# Clean the obj directory where the access denied error occurs
Write-Host "Cleaning obj directory..." -ForegroundColor Yellow
if (Test-Path "obj") {
    Remove-Item -Path "obj" -Recurse -Force -ErrorAction SilentlyContinue
}

# Build script for Homeowners project
Write-Host "Building HomeownersSubdivision project..." -ForegroundColor Cyan
dotnet build HomeownersSubdivision.csproj

# Check if build was successful
if ($LASTEXITCODE -eq 0) {
    Write-Host "Build completed successfully!" -ForegroundColor Green
} else {
    Write-Host "Build failed with exit code $LASTEXITCODE" -ForegroundColor Red
}

# Keep the window open if running as a script
if ($host.Name -eq 'ConsoleHost') {
    Write-Host "Press any key to continue..." -ForegroundColor Magenta
    $null = $host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
} 