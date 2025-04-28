#!/bin/bash
# Build script for Homeowners project
echo "Building HomeownersSubdivision project..."
dotnet build HomeownersSubdivision.csproj

# Check if build was successful
if [ $? -eq 0 ]; then
    echo -e "\e[32mBuild completed successfully!\e[0m"
else
    echo -e "\e[31mBuild failed with exit code $?\e[0m"
fi 