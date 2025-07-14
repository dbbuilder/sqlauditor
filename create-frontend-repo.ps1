# Create Frontend Repository for Static Web App Deployment
Write-Host "=== Creating Frontend Repository ===" -ForegroundColor Cyan

# Create a new directory for the frontend repository
$frontendRepoPath = "D:\Dev2\sqlanalyzer-web"

if (Test-Path $frontendRepoPath) {
    Write-Host "Directory already exists. Cleaning up..." -ForegroundColor Yellow
    Remove-Item -Path $frontendRepoPath -Recurse -Force
}

Write-Host "Creating frontend repository directory..." -ForegroundColor Yellow
New-Item -Path $frontendRepoPath -ItemType Directory | Out-Null

# Copy frontend files
Write-Host "Copying frontend files..." -ForegroundColor Yellow
Copy-Item -Path "src\SqlAnalyzer.Web\*" -Destination $frontendRepoPath -Recurse -Force

# Initialize Git repository
Set-Location -Path $frontendRepoPath
Write-Host "`nInitializing Git repository..." -ForegroundColor Yellow
git init

# Create .gitignore
Write-Host "Creating .gitignore..." -ForegroundColor Yellow
@"
# Dependencies
node_modules/
.pnp
.pnp.js

# Production
dist/

# Testing
coverage/

# Misc
.DS_Store
.env.local
.env.development.local
.env.test.local
.env.production.local

# Logs
npm-debug.log*
yarn-debug.log*
yarn-error.log*

# Editor
.vscode/
.idea/
*.swp
*.swo
*~

# OS
Thumbs.db
"@ | Set-Content .gitignore

# Create README
Write-Host "Creating README..." -ForegroundColor Yellow
@"
# SQL Analyzer Web Frontend

This is the Vue.js frontend for SQL Analyzer, deployed as an Azure Static Web App.

## Features

- JWT Authentication with login/logout
- Connection string builder UI
- Database analysis interface
- Real-time analysis progress with SignalR
- Support for SQL Server, PostgreSQL, and MySQL

## Development

\`\`\`bash
# Install dependencies
npm install

# Run development server
npm run dev

# Build for production
npm run build
\`\`\`

## Environment Variables

Create a \`.env\` file:

\`\`\`
VITE_API_URL=https://sqlanalyzer-api.azurewebsites.net
VITE_ENABLE_MOCK_DATA=false
\`\`\`

## Deployment

This repository is configured to deploy automatically to Azure Static Web Apps via GitHub Actions.

### Manual Deployment

\`\`\`bash
# Build the application
npm run build

# Deploy using SWA CLI
swa deploy ./dist --app-name sqlanalyzer-web
\`\`\`

## Authentication

Default credentials:
- Username: admin
- Password: AnalyzeThis!!
"@ | Set-Content README.md

# Create GitHub workflow for Static Web Apps
Write-Host "Creating GitHub Actions workflow..." -ForegroundColor Yellow
New-Item -Path ".github\workflows" -ItemType Directory -Force | Out-Null

@'
name: Azure Static Web Apps CI/CD

on:
  push:
    branches:
      - main
      - master
  pull_request:
    types: [opened, synchronize, reopened, closed]
    branches:
      - main
      - master

jobs:
  build_and_deploy_job:
    if: github.event_name == 'push' || (github.event_name == 'pull_request' && github.event.action != 'closed')
    runs-on: ubuntu-latest
    name: Build and Deploy Job
    steps:
      - uses: actions/checkout@v3
        with:
          submodules: true
          lfs: false
          
      - name: Setup Node.js
        uses: actions/setup-node@v3
        with:
          node-version: '18'
          
      - name: Install dependencies
        run: npm ci
        
      - name: Build application
        run: npm run build
        env:
          VITE_API_URL: https://sqlanalyzer-api.azurewebsites.net
          VITE_ENABLE_MOCK_DATA: false
          
      - name: Build And Deploy
        id: builddeploy
        uses: Azure/static-web-apps-deploy@v1
        with:
          azure_static_web_apps_api_token: ${{ secrets.AZURE_STATIC_WEB_APPS_API_TOKEN }}
          repo_token: ${{ secrets.GITHUB_TOKEN }}
          action: "upload"
          app_location: "/"
          api_location: ""
          output_location: "dist"

  close_pull_request_job:
    if: github.event_name == 'pull_request' && github.event.action == 'closed'
    runs-on: ubuntu-latest
    name: Close Pull Request Job
    steps:
      - name: Close Pull Request
        id: closepullrequest
        uses: Azure/static-web-apps-deploy@v1
        with:
          azure_static_web_apps_api_token: ${{ secrets.AZURE_STATIC_WEB_APPS_API_TOKEN }}
          action: "close"
'@ | Set-Content ".github\workflows\azure-static-web-apps.yml"

# Create static web app configuration
Write-Host "Creating staticwebapp.config.json..." -ForegroundColor Yellow
@'
{
  "routes": [
    {
      "route": "/api/*",
      "allowedRoles": ["authenticated"]
    },
    {
      "route": "/login",
      "allowedRoles": ["anonymous", "authenticated"]
    },
    {
      "route": "/*",
      "allowedRoles": ["anonymous", "authenticated"],
      "headers": {
        "cache-control": "no-cache, no-store, must-revalidate"
      }
    }
  ],
  "navigationFallback": {
    "rewrite": "/index.html"
  },
  "mimeTypes": {
    ".json": "application/json"
  }
}
'@ | Set-Content staticwebapp.config.json

# Clean up unnecessary files
Write-Host "`nCleaning up unnecessary files..." -ForegroundColor Yellow
if (Test-Path "deploy.zip") { Remove-Item "deploy.zip" }
if (Test-Path "api-deploy.zip") { Remove-Item "api-deploy.zip" }
if (Test-Path "Dockerfile") { Remove-Item "Dockerfile" }
if (Test-Path "nginx.conf") { Remove-Item "nginx.conf" }

# Add all files to git
Write-Host "`nAdding files to Git..." -ForegroundColor Yellow
git add -A

# Commit
Write-Host "Committing changes..." -ForegroundColor Yellow
git commit -m "Initial commit - SQL Analyzer Web Frontend

- Vue.js frontend with authentication
- Connection string builder UI
- Azure Static Web Apps deployment configuration
- GitHub Actions workflow"

Write-Host "`n✅ Frontend repository created at: $frontendRepoPath" -ForegroundColor Green
Write-Host "`nNext steps:" -ForegroundColor Yellow
Write-Host "1. Create repository on GitHub: https://github.com/new" -ForegroundColor White
Write-Host "   - Name: sqlanalyzer-web" -ForegroundColor Gray
Write-Host "   - Keep it public or private as needed" -ForegroundColor Gray
Write-Host ""
Write-Host "2. Add remote and push:" -ForegroundColor White
Write-Host "   cd $frontendRepoPath" -ForegroundColor Gray
Write-Host "   git remote add origin https://github.com/YOUR_USERNAME/sqlanalyzer-web.git" -ForegroundColor Gray
Write-Host "   git push -u origin main" -ForegroundColor Gray
Write-Host ""
Write-Host "3. Get the Static Web App deployment token:" -ForegroundColor White
Write-Host "   - Go to Azure Portal > sqlanalyzer-web" -ForegroundColor Gray
Write-Host "   - Manage deployment token" -ForegroundColor Gray
Write-Host "   - Copy the token" -ForegroundColor Gray
Write-Host ""
Write-Host "4. Add the token to GitHub secrets:" -ForegroundColor White
Write-Host "   - Go to GitHub repo > Settings > Secrets" -ForegroundColor Gray
Write-Host "   - Add new secret: AZURE_STATIC_WEB_APPS_API_TOKEN" -ForegroundColor Gray
Write-Host "   - Paste the deployment token" -ForegroundColor Gray

Set-Location -Path "D:\Dev2\sqlauditor"