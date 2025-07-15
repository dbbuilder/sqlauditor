#!/bin/bash
# Manual API deployment script

echo "Building API..."
cd api
dotnet publish SqlAnalyzer.Api/SqlAnalyzer.Api.csproj -c Release -o ./publish

echo "Creating deployment package..."
cd publish
zip -r ../../api-deploy.zip .
cd ../..

echo "Deploying to Azure..."
az webapp deployment source config-zip \
    --resource-group rg-sqlanalyzer \
    --name sqlanalyzer-api-win \
    --src api-deploy.zip

echo "Cleaning up..."
rm api-deploy.zip

echo "Deployment complete!"
echo "API URL: https://sqlanalyzer-api-win.azurewebsites.net"