#!/bin/bash
# Create Pipeline using Azure DevOps REST API

PAT="50lOB49w868Z6gCBhJqClT6NsnjeJAXhrFs3VbTR508lQih412weJQQJ99BGACAAAAAZ4Zw5AAASAZDO3OIt"
ORG="dbbuilder-dev"
PROJECT="SQLAnalyzer"
API_VERSION="7.0"

# Base64 encode the PAT for basic auth
AUTH=$(echo -n ":$PAT" | base64 -w 0)

# Get project ID
echo "Getting project ID..."
PROJECT_ID=$(curl -s \
  -H "Authorization: Basic $AUTH" \
  "https://dev.azure.com/$ORG/_apis/projects/$PROJECT?api-version=$API_VERSION" | \
  jq -r '.id')

echo "Project ID: $PROJECT_ID"

# Create pipeline definition
echo "Creating pipeline definition..."
PIPELINE_JSON=$(cat <<EOF
{
  "name": "SqlAnalyzer-CI-CD",
  "folder": "",
  "configuration": {
    "type": "yaml",
    "path": "/azure-pipelines.yml",
    "repository": {
      "id": "dbbuilder/sqlauditor",
      "type": "gitHub",
      "name": "dbbuilder/sqlauditor",
      "url": "https://github.com/dbbuilder/sqlauditor",
      "defaultBranch": "master",
      "properties": {
        "connectedServiceId": ""
      }
    }
  }
}
EOF
)

# Create the pipeline
echo "Creating pipeline..."
RESPONSE=$(curl -s -X POST \
  -H "Authorization: Basic $AUTH" \
  -H "Content-Type: application/json" \
  -d "$PIPELINE_JSON" \
  "https://dev.azure.com/$ORG/$PROJECT/_apis/build/definitions?api-version=$API_VERSION")

PIPELINE_ID=$(echo $RESPONSE | jq -r '.id')

if [ "$PIPELINE_ID" != "null" ]; then
  echo "Pipeline created successfully!"
  echo "Pipeline ID: $PIPELINE_ID"
  echo ""
  echo "View pipeline: https://dev.azure.com/$ORG/$PROJECT/_build?definitionId=$PIPELINE_ID"
else
  echo "Failed to create pipeline. Response:"
  echo $RESPONSE | jq .
fi