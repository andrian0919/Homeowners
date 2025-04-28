# Build script using the solution file
Write-Host "Building solution Homeowners.sln..." -ForegroundColor Cyan
dotnet build Homeowners.sln

# Check if build was successful
if ($LASTEXITCODE -eq 0) {
    Write-Host "Build completed successfully!" -ForegroundColor Green
} else {
    Write-Host "Build failed with exit code $LASTEXITCODE" -ForegroundColor Red
} 