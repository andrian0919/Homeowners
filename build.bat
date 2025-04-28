@echo off
echo Building Homeowners Subdivision project...
dotnet build HomeownersSubdivision.csproj
if %ERRORLEVEL% EQU 0 (
    echo Build completed successfully!
) else (
    echo Build failed with error code %ERRORLEVEL%
)
pause 