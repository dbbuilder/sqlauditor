#!/bin/bash

# Test CORS Status using curl
echo -e "\033[36mSQL Analyzer CORS Test (curl)\033[0m"
echo -e "\033[36m==============================\033[0m"
echo ""

API_URL="https://sqlanalyzer-api.azurewebsites.net"
SWA_URL="https://black-desert-02d93d30f.2.azurestaticapps.net"

# Test 1: Basic API access
echo -e "\033[33m1. Testing API availability...\033[0m"
if curl -s -o /dev/null -w "%{http_code}" "$API_URL/api/version" | grep -q "200"; then
    echo -e "\033[32m✅ API is accessible (200 OK)\033[0m"
else
    echo -e "\033[31m❌ API is not accessible\033[0m"
fi

# Test 2: OPTIONS preflight request
echo -e "\n\033[33m2. Testing CORS preflight (OPTIONS)...\033[0m"
echo "Request:"
echo "curl -X OPTIONS '$API_URL/api/v1/analysis/types' \\"
echo "  -H 'Origin: $SWA_URL' \\"
echo "  -H 'Access-Control-Request-Method: GET' \\"
echo "  -H 'Access-Control-Request-Headers: content-type' -v"
echo ""
echo "Response headers:"
curl -X OPTIONS "$API_URL/api/v1/analysis/types" \
  -H "Origin: $SWA_URL" \
  -H "Access-Control-Request-Method: GET" \
  -H "Access-Control-Request-Headers: content-type" \
  -s -D - -o /dev/null | grep -i "access-control" || echo -e "\033[31m❌ No CORS headers found\033[0m"

# Test 3: GET with Origin header
echo -e "\n\033[33m3. Testing GET with Origin header...\033[0m"
echo "Request:"
echo "curl '$API_URL/api/v1/analysis/types' -H 'Origin: $SWA_URL' -v"
echo ""
echo "Response headers:"
curl "$API_URL/api/v1/analysis/types" \
  -H "Origin: $SWA_URL" \
  -s -D - -o /dev/null | grep -i "access-control" || echo -e "\033[31m❌ No CORS headers found\033[0m"

# Test 4: SignalR negotiate
echo -e "\n\033[33m4. Testing SignalR negotiate endpoint...\033[0m"
echo "Request:"
echo "curl -X POST '$API_URL/hubs/analysis/negotiate?negotiateVersion=1' -H 'Origin: $SWA_URL' -v"
echo ""
echo "Response:"
response=$(curl -X POST "$API_URL/hubs/analysis/negotiate?negotiateVersion=1" \
  -H "Origin: $SWA_URL" \
  -H "Content-Length: 0" \
  -s -w "\nHTTP_CODE:%{http_code}")

http_code=$(echo "$response" | grep "HTTP_CODE:" | cut -d: -f2)
body=$(echo "$response" | sed '/HTTP_CODE:/d')

if [ "$http_code" = "200" ]; then
    echo -e "\033[32m✅ SignalR negotiate succeeded (200 OK)\033[0m"
    echo "Response: $body"
else
    echo -e "\033[31m❌ SignalR negotiate failed (HTTP $http_code)\033[0m"
fi

# Test 5: Check current Azure Portal CORS
echo -e "\n\033[33m5. Checking Azure Portal CORS configuration...\033[0m"
if command -v az &> /dev/null; then
    cors_origins=$(az webapp cors show --name sqlanalyzer-api --resource-group rg-sqlanalyzer --query allowedOrigins -o tsv 2>/dev/null)
    if [ -n "$cors_origins" ]; then
        echo -e "\033[31m⚠️  Azure Portal CORS is CONFIGURED (This overrides app CORS!)\033[0m"
        echo "Allowed Origins:"
        echo "$cors_origins" | while read -r origin; do
            echo "  - $origin"
        done
        echo -e "\n\033[31m🚨 This is likely causing the CORS errors!\033[0m"
    else
        echo -e "\033[32m✅ No Azure Portal CORS configured (Good!)\033[0m"
    fi
else
    echo "Azure CLI not available - cannot check Portal CORS"
fi

echo -e "\n\033[36m====== SUMMARY ======\033[0m"
echo "API URL: $API_URL"
echo "SWA URL: $SWA_URL"
echo ""
echo -e "\033[33mIf CORS headers are missing above:\033[0m"
echo "1. Go to Azure Portal > sqlanalyzer-api > CORS"
echo "2. Remove ALL entries"
echo "3. Save and restart the service"
echo ""
echo -e "\033[32mExpected CORS headers when working:\033[0m"
echo "- Access-Control-Allow-Origin: $SWA_URL"
echo "- Access-Control-Allow-Methods: GET, POST, OPTIONS"
echo "- Access-Control-Allow-Credentials: true"