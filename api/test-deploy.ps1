# Simple test deployment
Write-Host "Creating minimal API deployment..." -ForegroundColor Yellow

# Create a simple test page
@"
<%@ Page Language="C#" %>
<!DOCTYPE html>
<html>
<head>
    <title>SQL Analyzer API</title>
</head>
<body>
    <h1>SQL Analyzer API - Windows</h1>
    <p>Status: Running</p>
    <p>Time: <%= DateTime.UtcNow.ToString("o") %></p>
    <p>.NET Version: <%= System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription %></p>
</body>
</html>
"@ | Out-File -FilePath "default.aspx" -Encoding UTF8

# Deploy just this file
az webapp deployment source config-zip `
    --resource-group rg-sqlanalyzer `
    --name sqlanalyzer-api-win `
    --src <(echo "default.aspx" | zip -@ -)

Write-Host "Test deployment complete" -ForegroundColor Green