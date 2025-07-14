# SQL Analyzer Web Frontend

This is the Vue.js frontend for SQL Analyzer, deployed as an Azure Static Web App.

## Features

- JWT Authentication with login/logout
- Connection string builder UI
- Database analysis interface
- Real-time analysis progress with SignalR
- Support for SQL Server, PostgreSQL, and MySQL

## Development

\\\ash
# Install dependencies
npm install

# Run development server
npm run dev

# Build for production
npm run build
\\\

## Environment Variables

Create a \.env\ file:

\\\
VITE_API_URL=https://sqlanalyzer-api.azurewebsites.net
VITE_ENABLE_MOCK_DATA=false
\\\

## Deployment

This repository is configured to deploy automatically to Azure Static Web Apps via GitHub Actions.

### Manual Deployment

\\\ash
# Build the application
npm run build

# Deploy using SWA CLI
swa deploy ./dist --app-name sqlanalyzer-web
\\\

## Authentication

Default credentials:
- Username: admin
- Password: AnalyzeThis!!
# Force rebuild
