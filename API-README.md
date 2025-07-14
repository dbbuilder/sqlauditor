# SQL Analyzer API & Web UI

This project provides both a REST API and Vue.js web interface for the SQL Analyzer functionality, allowing you to run database analysis through multiple interfaces.

## Architecture Overview

```
┌─────────────────┐     ┌─────────────────┐     ┌─────────────────┐
│   Console CLI   │     │   Vue.js Web    │     │  Other Clients  │
└────────┬────────┘     └────────┬────────┘     └────────┬────────┘
         │                       │                         │
         │                       ▼                         │
         │              ┌─────────────────┐                │
         │              │   Web Browser   │                │
         │              └────────┬────────┘                │
         │                       │                         │
         └───────────────────────┴─────────────────────────┘
                                 │
                                 ▼
                    ┌────────────────────────┐
                    │   ASP.NET Core API     │
                    │  (REST + SignalR Hub)  │
                    └────────────┬───────────┘
                                 │
                                 ▼
                    ┌────────────────────────┐
                    │  SqlAnalyzer.Core      │
                    │  (Shared Backend)      │
                    └────────────────────────┘
```

## Getting Started

### Prerequisites
- .NET 8.0 SDK
- Node.js 18+ and npm
- SQL Server, PostgreSQL, or MySQL database

### Running the API

1. **Navigate to API directory**:
   ```bash
   cd src/SqlAnalyzer.Api
   ```

2. **Install dependencies**:
   ```bash
   dotnet restore
   ```

3. **Run the API**:
   ```bash
   dotnet run
   ```

   The API will start on `https://localhost:5001` (or `http://localhost:5000`)

4. **Access Swagger UI**:
   Open browser to `https://localhost:5001/swagger`

### Running the Web UI

1. **Navigate to Web directory**:
   ```bash
   cd src/SqlAnalyzer.Web
   ```

2. **Install dependencies**:
   ```bash
   npm install
   ```

3. **Configure API endpoint**:
   Create `.env.local` file:
   ```env
   VITE_API_URL=https://localhost:5001
   ```

4. **Run development server**:
   ```bash
   npm run dev
   ```

   The web UI will be available at `http://localhost:5173`

## API Endpoints

### Analysis Operations

#### Start Analysis
```http
POST /api/v1/analysis/start
Content-Type: application/json

{
  "connectionString": "Server=localhost;Database=mydb;User Id=sa;Password=Pass123!",
  "databaseType": "SqlServer",
  "analysisType": "comprehensive",
  "options": {
    "includeIndexAnalysis": true,
    "includeFragmentation": true,
    "includeStatistics": true,
    "includeSecurityAudit": true,
    "includeQueryPerformance": true,
    "timeoutMinutes": 30
  }
}
```

Response:
```json
{
  "jobId": "123e4567-e89b-12d3-a456-426614174000",
  "status": "Started",
  "message": "Analysis started successfully. Use the jobId to track progress."
}
```

#### Get Analysis Status
```http
GET /api/v1/analysis/status/{jobId}
```

Response:
```json
{
  "jobId": "123e4567-e89b-12d3-a456-426614174000",
  "status": "Running",
  "progressPercentage": 45.5,
  "currentStep": "Analyzing table indexes",
  "startedAt": "2025-07-10T10:30:00Z",
  "completedAt": null,
  "duration": null,
  "errorMessage": null
}
```

#### Get Analysis Results
```http
GET /api/v1/analysis/results/{jobId}
```

#### Test Connection
```http
POST /api/v1/analysis/test-connection
Content-Type: application/json

{
  "connectionString": "Server=localhost;Database=mydb;User Id=sa;Password=Pass123!",
  "databaseType": "SqlServer"
}
```

#### Export Results
```http
GET /api/v1/analysis/export/{jobId}?format=pdf
```

### Real-time Updates via SignalR

Connect to SignalR hub at `/hubs/analysis` to receive real-time progress updates:

```javascript
const connection = new signalR.HubConnectionBuilder()
    .withUrl("https://localhost:5001/hubs/analysis")
    .build();

connection.on("AnalysisProgress", (status) => {
    console.log("Progress update:", status);
});

connection.start();
```

## Using with Console CLI

The console application can also use the API backend:

```bash
# Using local analysis (default)
sqlanalyzer analyze --connection "Server=..." --type SqlServer

# Using API backend
sqlanalyzer analyze --connection "Server=..." --type SqlServer --use-api --api-url https://localhost:5001
```

## Configuration

### API Configuration (appsettings.json)

```json
{
  "SqlAnalyzer": {
    "DefaultTimeout": 300,
    "MaxConcurrentAnalyses": 5,
    "EnableCaching": true,
    "CacheDuration": 3600
  },
  "Redis": {
    "Enabled": true,
    "ConnectionString": "localhost:6379"
  }
}
```

### Web UI Configuration

Environment variables in `.env.local`:
```env
VITE_API_URL=https://localhost:5001
VITE_ENABLE_MOCK_DATA=false
VITE_ANALYSIS_POLL_INTERVAL=2000
```

## Production Deployment

### API Deployment

1. **Build for production**:
   ```bash
   dotnet publish -c Release -o ./publish
   ```

2. **Docker deployment**:
   ```dockerfile
   FROM mcr.microsoft.com/dotnet/aspnet:8.0
   WORKDIR /app
   COPY --from=publish /app/publish .
   EXPOSE 80
   ENTRYPOINT ["dotnet", "SqlAnalyzer.Api.dll"]
   ```

3. **Environment variables**:
   ```bash
   ASPNETCORE_ENVIRONMENT=Production
   ASPNETCORE_URLS=http://+:80
   ConnectionStrings__Redis=redis-server:6379
   ```

### Web UI Deployment

1. **Build for production**:
   ```bash
   npm run build
   ```

2. **Nginx configuration**:
   ```nginx
   server {
       listen 80;
       server_name sqlanalyzer.example.com;
       
       location / {
           root /var/www/sqlanalyzer;
           try_files $uri $uri/ /index.html;
       }
       
       location /api {
           proxy_pass http://api-server:5000;
           proxy_http_version 1.1;
           proxy_set_header Upgrade $http_upgrade;
           proxy_set_header Connection "upgrade";
       }
   }
   ```

## Security Considerations

1. **Authentication**: Implement authentication middleware
2. **CORS**: Configure allowed origins in production
3. **HTTPS**: Always use HTTPS in production
4. **Connection Strings**: Never expose in logs or responses
5. **Rate Limiting**: Implement to prevent abuse

## Monitoring

The API includes health checks at `/health`:

```bash
curl https://localhost:5001/health
```

Metrics can be exposed for Prometheus:
```csharp
app.UseHttpMetrics();
app.MapMetrics();
```

## Troubleshooting

### Common Issues

1. **CORS errors in browser**:
   - Check CORS configuration in API
   - Ensure API URL in Vue config is correct

2. **SignalR connection fails**:
   - Check WebSocket support
   - Verify firewall rules

3. **Analysis times out**:
   - Increase timeout in configuration
   - Check database query performance

### Logs

API logs location:
- Development: Console output
- Production: `/logs/sqlanalyzer-api-{date}.txt`

## License

See LICENSE file in root directory.