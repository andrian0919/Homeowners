# Network Access for Homeowners Subdivision Application

This guide explains how to run the Homeowners Subdivision application so it's accessible from other computers on your local network.

## Prerequisites

1. Make sure your firewall allows incoming connections on ports 5000 and 5001
2. Ensure your computer has the static IP address 192.168.1.14 configured

## Running the Application on the Network

### Using Batch File (Windows)

1. Open Command Prompt as Administrator
2. Navigate to the project directory
3. Run the batch file:
   ```
   run-network.bat
   ```

### Using PowerShell (Windows)

1. Open PowerShell as Administrator
2. Navigate to the project directory
3. Run the PowerShell script:
   ```
   .\run-network.ps1
   ```

### Manually (Any OS)

Run the following command in the project directory:
```
dotnet run --urls="http://192.168.1.14:5000;https://192.168.1.14:5001"
```

## Accessing the Application

Once the application is running, other computers on your network can access it using:

- HTTP: http://192.168.1.14:5000
- HTTPS: https://192.168.1.14:5001

### HTTPS Certificate Warning

When accessing the application via HTTPS, browsers may show a certificate warning because the application uses a development certificate. This is normal and you can proceed by accepting the risk.

## Troubleshooting

1. **Cannot access the application from other computers**:
   - Check that your firewall allows incoming connections on ports 5000 and 5001
   - Verify that your computer has the correct IP address (192.168.1.14)
   - Make sure the application is running with administrator privileges

2. **Application starts but immediately stops**:
   - Check if another application is already using ports 5000 or 5001
   - Try running with administrator privileges

3. **Error about binding to address**:
   - Make sure your network adapter has the IP address 192.168.1.14 assigned
   - If your IP address is different, modify the `Program.cs` file and the run scripts accordingly 