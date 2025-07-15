# GitHub Actions Workflows

This directory contains CI/CD workflows for SQL Analyzer:

## Workflows

- **deploy-api.yml**: Deploys the .NET API to Azure App Service (Windows)
- **deploy-frontend.yml**: Deploys the Vue.js frontend to Azure Static Web Apps

## Deployment

Deployments are triggered automatically when changes are pushed to the main/master branch.

### Manual Deployment

You can also trigger deployments manually from the Actions tab in GitHub.

## URLs

- API: https://sqlanalyzer-api-win.azurewebsites.net
- Frontend: https://sqlanalyzer-web.azureedge.net