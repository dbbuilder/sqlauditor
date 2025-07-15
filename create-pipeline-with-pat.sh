#!/bin/bash
# Create Pipeline using PAT directly

# REPLACE WITH YOUR PAT
PAT="YOUR_PAT_HERE"

# Set PAT as environment variable
export AZURE_DEVOPS_EXT_PAT="$PAT"

# Variables
ORG="https://dev.azure.com/dbbuilder-dev"
PROJECT="SQLAnalyzer"

echo "Creating pipeline using PAT authentication..."

# Create pipeline
az pipelines create \
    --name "SqlAnalyzer-CI-CD" \
    --description "SQL Analyzer CI/CD Pipeline" \
    --repository "https://github.com/dbbuilder/sqlauditor" \
    --branch "master" \
    --yml-path "azure-pipelines.yml" \
    --repository-type github \
    --org "$ORG" \
    --project "$PROJECT" \
    --skip-first-run true

# Get pipeline ID
PIPELINE_ID=$(az pipelines list \
    --org "$ORG" \
    --project "$PROJECT" \
    --query "[?name=='SqlAnalyzer-CI-CD'].id" -o tsv)

echo "Pipeline created! ID: $PIPELINE_ID"
echo ""
echo "To run: az pipelines run --id $PIPELINE_ID --org $ORG --project $PROJECT"
echo "Or visit: $ORG/$PROJECT/_build?definitionId=$PIPELINE_ID"