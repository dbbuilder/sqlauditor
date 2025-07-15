#!/bin/bash
# Check SQL Analyzer Deployment Status

echo "SQL Analyzer Deployment Status"
echo "=============================="
echo ""

# Frontend
echo "Frontend:"
echo "  URL: https://black-desert-02d93d30f.2.azurestaticapps.net"
FRONTEND_STATUS=$(curl -s -o /dev/null -w "%{http_code}" https://black-desert-02d93d30f.2.azurestaticapps.net)
if [ "$FRONTEND_STATUS" = "200" ]; then
    echo "  Status: ✅ Online"
else
    echo "  Status: ❌ Offline (HTTP $FRONTEND_STATUS)"
fi

echo ""

# API
echo "API:"
echo "  URL: https://sqlanalyzer-api-win.azurewebsites.net"
API_STATUS=$(curl -s -o /dev/null -w "%{http_code}" https://sqlanalyzer-api-win.azurewebsites.net/api/v1/health)
if [ "$API_STATUS" = "200" ]; then
    echo "  Status: ✅ Online"
else
    echo "  Status: ⏳ Deploying... (currently showing default page)"
    echo "  Note: API deployment typically takes 3-5 minutes"
fi

echo ""
echo "GitHub Actions: https://github.com/dbbuilder/sqlauditor/actions"
echo ""
echo "To manually check API deployment:"
echo "  az webapp log tail --name sqlanalyzer-api-win --resource-group rg-sqlanalyzer"