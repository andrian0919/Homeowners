Write-Host "Starting Homeowners Subdivision application on network IP 192.168.1.14" -ForegroundColor Green
Write-Host "HTTP: http://192.168.1.14:5000" -ForegroundColor Cyan
Write-Host "HTTPS: https://192.168.1.14:5001" -ForegroundColor Cyan
Write-Host ""
Write-Host "Press Ctrl+C to stop the server" -ForegroundColor Yellow
Write-Host ""

dotnet run --urls="http://192.168.1.14:5000;https://192.168.1.14:5001" 