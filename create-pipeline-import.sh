#\!/bin/bash
# Import pipeline directly

PAT="50lOB49w868Z6gCBhJqClT6NsnjeJAXhrFs3VbTR508lQih412weJQQJ99BGACAAAAAZ4Zw5AAASAZDO3OIt"
ORG="dbbuilder-dev"
PROJECT="SQLAnalyzer"
REPO_URL="https://github.com/dbbuilder/sqlauditor.git"

# Base64 encode the PAT
AUTH=$(echo -n ":$PAT"  < /dev/null |  base64 -w 0)

# Create import request
echo "Creating pipeline import request..."

IMPORT_JSON=$(cat <<JSON
{
  "name": "SqlAnalyzer-CI-CD",
  "description": "SQL Analyzer CI/CD Pipeline",
  "path": "\\\\",
  "type": "build",
  "configuration": {
    "type": "yaml",
    "yamlFilename": "azure-pipelines.yml",
    "repository": {
      "type": "github",
      "name": "dbbuilder/sqlauditor",
      "url": "$REPO_URL",
      "defaultBranch": "master"
    }
  }
}
JSON
)

echo "$IMPORT_JSON" > pipeline-import.json

echo "Import configuration saved to pipeline-import.json"
echo ""
echo "To create the pipeline:"
echo "1. Go to: https://dev.azure.com/$ORG/$PROJECT/_build"
echo "2. Click 'New pipeline'"
echo "3. Select 'GitHub'"
echo "4. Select 'dbbuilder/sqlauditor' repository"
echo "5. Select 'Existing Azure Pipelines YAML file'"
echo "6. Branch: master, Path: /azure-pipelines.yml"
echo "7. Click Continue"
