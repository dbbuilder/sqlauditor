#!/bin/bash

# Manual Azure Static Web App Deployment Script

echo "Manual SWA Deployment"
echo "===================="

# Configuration
SWA_NAME="sqlanalyzer-web"
RESOURCE_GROUP="rg-sqlanalyzer"
DEPLOYMENT_TOKEN="9a26c902adef2c2dfa57b8c414f28f1c9a7df7a48ce492bc48f39f8b5bc4964802-ca65247f-b0a3-44c2-9174-d119816af99300f010802d93d30f"

echo "Building app..."
cd /mnt/d/dev2/sqlauditor/src/SqlAnalyzer.Web

# Set production environment
export VITE_API_URL=https://sqlanalyzer-api.azurewebsites.net
export VITE_ENABLE_MOCK_DATA=false

# Clean and build
rm -rf dist
npm run build

# Copy config
cp staticwebapp.config.json dist/

echo ""
echo "Build complete. Files in dist:"
ls -la dist/

echo ""
echo "Deploying to Azure Static Web Apps..."

# Install SWA CLI globally if not present
if ! command -v swa &> /dev/null; then
    echo "Installing SWA CLI..."
    npm install -g @azure/static-web-apps-cli@latest
fi

# Deploy
swa deploy ./dist \
  --deployment-token "$DEPLOYMENT_TOKEN" \
  --env production \
  --verbose

echo ""
echo "Deployment complete!"
echo "URL: https://black-desert-02d93d30f.2.azurestaticapps.net"