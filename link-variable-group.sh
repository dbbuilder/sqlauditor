#!/bin/bash
# Link variable group to pipeline using REST API

PAT="50lOB49w868Z6gCBhJqClT6NsnjeJAXhrFs3VbTR508lQih412weJQQJ99BGACAAAAAZ4Zw5AAASAZDO3OIt"
ORG="dbbuilder-dev"
PROJECT="SQLAnalyzer"
PIPELINE_ID="1"
AUTH=$(echo -n ":$PAT" | base64 -w 0)

# Get variable group ID
echo "Getting variable group ID..."
VAR_GROUP_ID=$(az pipelines variable-group list \
    --org "https://dev.azure.com/$ORG" \
    --project "$PROJECT" \
    --query "[?name=='SqlAnalyzer-Variables'].id" -o tsv)

echo "Variable Group ID: $VAR_GROUP_ID"

# Get current pipeline definition
echo "Getting pipeline definition..."
PIPELINE_DEF=$(curl -s \
    -H "Authorization: Basic $AUTH" \
    "https://dev.azure.com/$ORG/$PROJECT/_apis/build/definitions/$PIPELINE_ID?api-version=7.0")

# Update pipeline to include variable group
echo "Updating pipeline with variable group..."
UPDATED_DEF=$(echo "$PIPELINE_DEF" | python3 -c "
import sys, json
data = json.load(sys.stdin)
if 'variableGroups' not in data:
    data['variableGroups'] = []
data['variableGroups'].append($VAR_GROUP_ID)
print(json.dumps(data))
")

# Update the pipeline
curl -X PUT \
    -H "Authorization: Basic $AUTH" \
    -H "Content-Type: application/json" \
    -d "$UPDATED_DEF" \
    "https://dev.azure.com/$ORG/$PROJECT/_apis/build/definitions/$PIPELINE_ID?api-version=7.0" \
    -o /dev/null -s

echo "Variable group linked to pipeline!"
echo ""
echo "Now run the pipeline again:"
echo "az pipelines run --id $PIPELINE_ID --org 'https://dev.azure.com/$ORG' --project '$PROJECT'"