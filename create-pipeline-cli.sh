#!/bin/bash
# Create Azure DevOps Pipeline via CLI

echo "Creating Azure DevOps Pipeline for SQL Analyzer..."

# Variables
ORG="https://dev.azure.com/dbbuilder-dev"
PROJECT="SQLAnalyzer"
PIPELINE_NAME="SqlAnalyzer-CI-CD"
REPO_URL="https://github.com/dbbuilder/sqlauditor"
BRANCH="master"
YAML_PATH="azure-pipelines.yml"

# First, we need to create a GitHub service connection if it doesn't exist
echo "Setting up GitHub connection..."

# Create pipeline using az pipelines create
echo "Creating pipeline..."
az pipelines create \
    --name "$PIPELINE_NAME" \
    --description "SQL Analyzer CI/CD Pipeline" \
    --repository "$REPO_URL" \
    --branch "$BRANCH" \
    --yml-path "$YAML_PATH" \
    --repository-type github \
    --org "$ORG" \
    --project "$PROJECT" \
    --skip-first-run true

# Get pipeline ID
PIPELINE_ID=$(az pipelines list \
    --org "$ORG" \
    --project "$PROJECT" \
    --query "[?name=='$PIPELINE_NAME'].id" -o tsv)

echo "Pipeline created with ID: $PIPELINE_ID"

# Link variable group to pipeline
echo "Linking variable group..."
VARGROUP_ID=$(az pipelines variable-group list \
    --org "$ORG" \
    --project "$PROJECT" \
    --query "[?name=='SqlAnalyzer-Variables'].id" -o tsv)

if [ ! -z "$VARGROUP_ID" ]; then
    echo "Variable group ID: $VARGROUP_ID"
    # Variable group linking needs to be done through the UI or REST API
    echo "Note: Variable group needs to be linked manually in the pipeline settings"
fi

# Run the pipeline
echo ""
echo "Pipeline created successfully!"
echo "To run the pipeline:"
echo "  az pipelines run --id $PIPELINE_ID --org $ORG --project $PROJECT"
echo ""
echo "Or visit: $ORG/$PROJECT/_build?definitionId=$PIPELINE_ID"