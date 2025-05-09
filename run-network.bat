@echo off
echo Starting Homeowners Subdivision application on network IP 192.168.1.14
echo HTTP: http://192.168.1.14:5000
echo HTTPS: https://192.168.1.14:5001
echo.
echo Press Ctrl+C to stop the server
echo.
dotnet run --urls="http://192.168.1.14:5000;https://192.168.1.14:5001" 