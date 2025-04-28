# Build script for Homeowners project
Write-Host "Building HomeownersSubdivision project..."
dotnet build HomeownersSubdivision.csproj

# Check if build was successful
if ($LASTEXITCODE -eq 0) {
    Write-Host "Build completed successfully!" -ForegroundColor Green
} else {
    Write-Host "Build failed with exit code $LASTEXITCODE" -ForegroundColor Red
} 