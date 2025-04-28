# Clean build script for Homeowners project
Write-Host "Cleaning build outputs..." -ForegroundColor Yellow

# Try to stop any processes that might be locking the files
try {
    Get-Process -Name "HomeownersSubdivision" -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
    Write-Host "Stopped running processes" -ForegroundColor Green
} catch {
    Write-Host "No processes needed to be stopped" -ForegroundColor Gray
}

# Wait a moment for any file handles to close
Start-Sleep -Seconds 2

# Clean output directories
if (Test-Path "bin") {
    Write-Host "Removing bin directory..." -ForegroundColor Yellow
    Remove-Item -Path "bin" -Recurse -Force -ErrorAction SilentlyContinue
}

if (Test-Path "obj") {
    Write-Host "Removing obj directory..." -ForegroundColor Yellow
    Remove-Item -Path "obj" -Recurse -Force -ErrorAction SilentlyContinue 
}

# Wait a moment to ensure all files are released
Start-Sleep -Seconds 2

# Run dotnet clean to ensure everything is properly cleaned
Write-Host "Running dotnet clean..." -ForegroundColor Cyan
dotnet clean HomeownersSubdivision.csproj

# Build the project
Write-Host "Building HomeownersSubdivision project..." -ForegroundColor Cyan
dotnet build HomeownersSubdivision.csproj

# Check if build was successful
if ($LASTEXITCODE -eq 0) {
    Write-Host "Build completed successfully!" -ForegroundColor Green
} else {
    Write-Host "Build failed with exit code $LASTEXITCODE" -ForegroundColor Red
} 