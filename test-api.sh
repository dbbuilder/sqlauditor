#!/bin/bash

echo "Testing SQL Analyzer API endpoints..."
echo "======================================="

API_URL="https://sqlanalyzer-api-win.azurewebsites.net"

echo "1. Testing Version endpoint:"
curl -s "$API_URL/api/version" | head -50
echo ""

echo "2. Testing Email Status endpoint (requires auth):"
TOKEN="eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJ1bmlxdWVfbmFtZSI6ImFkbWluIiwicm9sZSI6IkFkbWluIiwidXNlcm5hbWUiOiJhZG1pbiIsIm5iZiI6MTc1MjY0MTg2NywiZXhwIjoxNzUyNzI4MjY3LCJpYXQiOjE3NTI2NDE4NjcsImlzcyI6Imh0dHBzOi8vc3FsYW5hbHl6ZXItYXBpLXdpbi5henVyZXdlYnNpdGVzLm5ldCIsImF1ZCI6Imh0dHBzOi8vYmxhY2stZGVzZXJ0LTAyZDkzZDMwZi4yLmF6dXJlc3RhdGljYXBwcy5uZXQifQ.GhmEgI_q9PtAjwCeoqtNEIS2NZQ7CngGoxlywF7qWD0"
curl -s -H "Authorization: Bearer $TOKEN" "$API_URL/api/v1/email/status" -w "\nStatus: %{http_code}\n"
echo ""

echo "3. Listing all available routes:"
echo "Auth endpoints:"
echo "  - POST $API_URL/api/v1/auth/login"
echo "  - POST $API_URL/api/v1/auth/logout"
echo "  - GET  $API_URL/api/v1/auth/verify"
echo ""
echo "Email endpoints:"
echo "  - POST $API_URL/api/v1/email/test"
echo "  - GET  $API_URL/api/v1/email/status"
echo ""
echo "Analysis endpoints:"
echo "  - POST $API_URL/api/v1/analysis/start"
echo "  - GET  $API_URL/api/v1/analysis/status/{jobId}"
echo "  - POST $API_URL/api/v1/analysis/test-connection"