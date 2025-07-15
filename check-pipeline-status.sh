#!/bin/bash
# Check Pipeline Status

ORG="https://dev.azure.com/dbbuilder-dev"
PROJECT="SQLAnalyzer"

echo "Checking Pipeline Status..."
echo "=========================="

# Get pipeline info
PIPELINE_ID=$(az pipelines list --org "$ORG" --project "$PROJECT" --query "[0].id" -o tsv)
PIPELINE_NAME=$(az pipelines list --org "$ORG" --project "$PROJECT" --query "[0].name" -o tsv)

echo "Pipeline: $PIPELINE_NAME (ID: $PIPELINE_ID)"
echo ""

# Get latest run
echo "Latest Run:"
az pipelines runs list \
    --org "$ORG" \
    --project "$PROJECT" \
    --pipeline-ids $PIPELINE_ID \
    --top 1 \
    --query "[0].{ID:id, Status:status, Result:result, StartTime:startTime}" \
    -o table

# Get run URL
RUN_ID=$(az pipelines runs list --org "$ORG" --project "$PROJECT" --pipeline-ids $PIPELINE_ID --top 1 --query "[0].id" -o tsv)
echo ""
echo "View details at: https://dev.azure.com/dbbuilder-dev/SQLAnalyzer/_build/results?buildId=$RUN_ID"
echo ""

# Check stages
echo "Pipeline Stages:"
echo "================"
echo "1. BuildAPI - Build and package .NET API"
echo "2. BuildFrontend - Build Vue.js frontend"  
echo "3. DeployAPI - Deploy to Azure App Service"
echo "4. DeployFrontend - Deploy to Static Web Apps"
echo ""
echo "Common Issues to Check:"
echo "- Variable group 'SqlAnalyzer-Variables' linked?"
echo "- Service connection 'SqlAnalyzer-ServiceConnection' authorized?"
echo "- GitHub repository accessible?"