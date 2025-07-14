#!/usr/bin/env python3
import os
import pathlib

# Frontend workflow
frontend_workflow = """name: Deploy Frontend to Azure Static Web Apps

on:
  push:
    branches:
      - main
      - master
    paths:
      - 'frontend/**'
      - '.github/workflows/deploy-frontend.yml'
  workflow_dispatch:

jobs:
  build_and_deploy:
    runs-on: ubuntu-latest
    name: Build and Deploy Frontend
    steps:
      - uses: actions/checkout@v3
        with:
          submodules: true
          lfs: false
          
      - name: Setup Node.js
        uses: actions/setup-node@v3
        with:
          node-version: '18'
          cache: 'npm'
          cache-dependency-path: frontend/package-lock.json
          
      - name: Install dependencies
        run: npm ci
        working-directory: frontend
        
      - name: Build application
        run: npm run build
        working-directory: frontend
        env:
          VITE_API_URL: https://sqlanalyzer-api.azurewebsites.net
          VITE_ENABLE_MOCK_DATA: false
          
      - name: Deploy to Azure Static Web Apps
        uses: Azure/static-web-apps-deploy@v1
        with:
          azure_static_web_apps_api_token: ${{ secrets.AZURE_STATIC_WEB_APPS_API_TOKEN }}
          repo_token: ${{ secrets.GITHUB_TOKEN }}
          action: "upload"
          app_location: "frontend"
          api_location: ""
          output_location: "dist"
          skip_app_build: true
"""

# API workflow
api_workflow = """name: Deploy API to Azure App Service

on:
  push:
    branches:
      - main
      - master
    paths:
      - 'api/**'
      - '.github/workflows/deploy-api.yml'
  workflow_dispatch:

jobs:
  build_and_deploy:
    runs-on: ubuntu-latest
    name: Build and Deploy API
    steps:
      - uses: actions/checkout@v3
      
      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '8.0.x'
          
      - name: Restore dependencies
        run: dotnet restore
        working-directory: api
        
      - name: Build
        run: dotnet build -c Release --no-restore
        working-directory: api
        
      - name: Publish
        run: dotnet publish SqlAnalyzer.Api/SqlAnalyzer.Api.csproj -c Release -o ./publish
        working-directory: api
        
      - name: Deploy to Azure App Service
        uses: azure/webapps-deploy@v2
        with:
          app-name: sqlanalyzer-api
          publish-profile: ${{ secrets.AZURE_WEBAPP_PUBLISH_PROFILE }}
          package: api/publish
"""

# Create workflows directory
workflows_dir = pathlib.Path('.github/workflows')
workflows_dir.mkdir(parents=True, exist_ok=True)

# Write frontend workflow
with open(workflows_dir / 'deploy-frontend.yml', 'w') as f:
    f.write(frontend_workflow)
print("✅ Created .github/workflows/deploy-frontend.yml")

# Write API workflow
with open(workflows_dir / 'deploy-api.yml', 'w') as f:
    f.write(api_workflow)
print("✅ Created .github/workflows/deploy-api.yml")

print("\n✅ GitHub Actions workflows created successfully!")
print("\nNext steps:")
print("1. Commit these changes")
print("2. Push to GitHub")
print("3. Add required secrets to GitHub repository:")
print("   - AZURE_STATIC_WEB_APPS_API_TOKEN")
print("   - AZURE_WEBAPP_PUBLISH_PROFILE")