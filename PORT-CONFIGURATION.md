# Port Configuration Summary

## Project Port Assignments

### AgentMarketer.WebApi
- **HTTPS**: https://localhost:7001
- **HTTP**: http://localhost:5001
- **Purpose**: Backend API for chat orchestration
- **Configuration Files**:
  - `launchSettings.json`: Kestrel and IIS Express profiles
  - `appsettings.json`: URLs configuration
  - `appsettings.Development.json`: Development overrides

### AgentMarketer.Web
- **HTTPS**: https://localhost:7002
- **HTTP**: http://localhost:5002
- **Purpose**: Blazor Server frontend with chat interface
- **Configuration Files**:
  - `launchSettings.json`: Kestrel and IIS Express profiles
  - `appsettings.json`: URLs and ApiBaseUrl configuration
  - `appsettings.Development.json`: Development overrides

## CORS Configuration
The WebApi project is configured to allow requests from the Web project:
- Allowed Origins: `https://localhost:7002`, `http://localhost:5002`
- CORS Policy: `AllowSpecificOrigins`

## HttpClient Configuration
The Web project's HttpClient is configured to call the WebApi:
- Base Address: `https://localhost:7001` (from configuration)
- Configuration Key: `ApiBaseUrl`

## Running Both Projects
To test the complete solution:

1. Start the WebApi project:
   ```powershell
   cd "c:\dev\AI specific\AgentMarketerPOC\AgentMarketer.WebApi"
   dotnet run
   ```

2. Start the Web project (in a separate terminal):
   ```powershell
   cd "c:\dev\AI specific\AgentMarketerPOC\AgentMarketer.Web"
   dotnet run
   ```

3. Access the application:
   - Web Interface: https://localhost:7002
   - API Swagger: https://localhost:7001/swagger (if enabled)

## Notes
- All projects target .NET 9.0
- Both use preview versions with appropriate warnings acknowledged
- Port assignments avoid conflicts with existing services
- CORS and HttpClient configurations are aligned for cross-origin communication
