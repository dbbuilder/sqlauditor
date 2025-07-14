# SQL Analyzer - Monorepo Structure

This project has been reorganized into a monorepo structure with separate frontend and API folders.

## Project Structure

```
sqlauditor/
├── frontend/              # Vue.js frontend application
│   ├── src/              # Vue source files
│   ├── public/           # Static assets
│   ├── dist/             # Build output
│   └── package.json      # Frontend dependencies
│
├── api/                  # .NET API application
│   ├── SqlAnalyzer.Api/  # API project
│   ├── SqlAnalyzer.Core/ # Core business logic
│   ├── SqlAnalyzer.CLI/  # Command-line interface
│   ├── SqlAnalyzer.Shared/ # Shared utilities
│   └── SqlAnalyzer.sln   # Solution file
│
├── tests/                # Test projects
└── deployment/           # Deployment scripts
```

## Quick Start

### Frontend Development
```bash
cd frontend
npm install
npm run dev
```

### API Development
```bash
cd api
dotnet restore
dotnet run --project SqlAnalyzer.Api
```

### Build Everything
```bash
# Build frontend
cd frontend && npm run build

# Build API
cd ../api && dotnet build -c Release
```

## Deployment

### Deploy Frontend to Azure Static Web Apps
```bash
cd frontend
gh repo create sqlanalyzer-web --public --source=. --push
# Add deployment token to GitHub secrets
# GitHub Actions will deploy automatically
```

### Deploy API to Azure App Service
```bash
cd api
dotnet publish -c Release
# Deploy to Azure App Service
```

## Configuration

### Frontend Environment Variables
Create `frontend/.env.local`:
```
VITE_API_URL=https://localhost:5001
VITE_ENABLE_MOCK_DATA=false
```

### API Configuration
Update `api/SqlAnalyzer.Api/appsettings.json` with your settings.

## Authentication

Default credentials:
- Username: admin
- Password: AnalyzeThis!!

## Technology Stack

### Frontend
- Vue.js 3
- Pinia (state management)
- Vue Router
- PrimeVue UI components
- SignalR client

### Backend
- .NET 8.0
- ASP.NET Core Web API
- Entity Framework Core
- SignalR
- JWT Authentication

### Databases Supported
- SQL Server
- PostgreSQL
- MySQL

## Development Workflow

1. Make changes in respective folders
2. Test locally
3. Commit and push
4. GitHub Actions deploy automatically

## Azure Resources

- **Frontend**: Azure Static Web Apps
  - URL: https://black-desert-02d93d30f.2.azurestaticapps.net
  
- **API**: Azure App Service
  - URL: https://sqlanalyzer-api.azurewebsites.net

## Scripts

- `deploy-frontend.ps1` - Deploy frontend only
- `deploy-api.ps1` - Deploy API only
- `deploy-all.ps1` - Deploy both frontend and API