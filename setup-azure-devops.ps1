# Setup Azure DevOps for SQL Analyzer
Write-Host "Setting up Azure DevOps for SQL Analyzer..." -ForegroundColor Yellow

# Variables
$orgUrl = "https://dev.azure.com/sqlanalyzer"
$projectName = "SqlAnalyzer"
$repoUrl = "https://github.com/dbbuilder/sqlauditor"
$resourceGroup = "rg-sqlanalyzer"
$subscription = "7b2beff3-b38a-4516-a75f-3216725cc4e9"

# Create Azure DevOps project
Write-Host "`nCreating Azure DevOps project..." -ForegroundColor Cyan
az devops project create `
    --name $projectName `
    --description "SQL Analyzer - Database Analysis Tool" `
    --source-control git `
    --visibility private `
    --org $orgUrl 2>$null

if ($LASTEXITCODE -ne 0) {
    Write-Host "Project might already exist, continuing..." -ForegroundColor Yellow
}

# Set defaults
az devops configure --defaults organization=$orgUrl project=$projectName

# Create service connection for Azure
Write-Host "`nCreating Azure service connection..." -ForegroundColor Cyan
$serviceEndpointId = az devops service-endpoint azurerm create `
    --azure-rm-service-principal-id "auto" `
    --azure-rm-subscription-id $subscription `
    --azure-rm-subscription-name "Test Environment" `
    --azure-rm-tenant-id "$(az account show --query tenantId -o tsv)" `
    --name "SqlAnalyzer-ServiceConnection" `
    --org $orgUrl `
    --project $projectName `
    --query id -o tsv

Write-Host "Service connection created: $serviceEndpointId" -ForegroundColor Green

# Create GitHub service connection
Write-Host "`nCreating GitHub service connection..." -ForegroundColor Cyan
Write-Host "NOTE: You'll need to authorize this in the browser" -ForegroundColor Yellow

# First, let's create the pipeline directly from the YAML file
Write-Host "`nCreating pipeline from YAML..." -ForegroundColor Cyan

# Create pipeline definition JSON
$pipelineDefinition = @{
    name = "SqlAnalyzer-CI-CD"
    folder = ""
    configuration = @{
        type = "yaml"
        path = "/azure-pipelines.yml"
        repository = @{
            type = "github"
            name = "dbbuilder/sqlauditor"
            url = $repoUrl
            defaultBranch = "master"
            connection = @{
                id = ""  # Will be filled after GitHub connection
            }
        }
    }
} | ConvertTo-Json -Depth 10

# Save pipeline definition
$pipelineDefinition | Out-File -FilePath "pipeline-def.json" -Encoding UTF8

Write-Host "`nPipeline definition created" -ForegroundColor Green

# Get Static Web App deployment token
Write-Host "`nRetrieving Static Web App deployment token..." -ForegroundColor Cyan
$staticWebAppToken = az staticwebapp secrets list `
    --name sqlanalyzer-frontend `
    --resource-group $resourceGroup `
    --query "properties.apiKey" -o tsv

# Create variable group
Write-Host "`nCreating variable group..." -ForegroundColor Cyan
$variableGroupDef = @{
    name = "SqlAnalyzer-Variables"
    description = "Variables for SQL Analyzer deployment"
    type = "Vsts"
    variables = @{
        AZURE_STATIC_WEB_APPS_API_TOKEN = @{
            value = $staticWebAppToken
            isSecret = $true
        }
    }
} | ConvertTo-Json -Depth 10

$variableGroupDef | Out-File -FilePath "vargroup-def.json" -Encoding UTF8

# Create variable group using REST API
$orgName = "sqlanalyzer"
$pat = Read-Host "Enter your Azure DevOps Personal Access Token" -AsSecureString
$patPlain = [Runtime.InteropServices.Marshal]::PtrToStringAuto([Runtime.InteropServices.Marshal]::SecureStringToBSTR($pat))

$base64AuthInfo = [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes(":$patPlain"))
$headers = @{
    Authorization = "Basic $base64AuthInfo"
    "Content-Type" = "application/json"
}

# Get project ID
$projectId = az devops project show --project $projectName --org $orgUrl --query id -o tsv

# Create variable group via REST API
$variableGroupUrl = "$orgUrl/$projectName/_apis/distributedtask/variablegroups?api-version=7.0"
$response = Invoke-RestMethod -Uri $variableGroupUrl -Method Post -Body $variableGroupDef -Headers $headers

Write-Host "Variable group created with ID: $($response.id)" -ForegroundColor Green

# Clean up
Remove-Item -Path "pipeline-def.json", "vargroup-def.json" -Force -ErrorAction SilentlyContinue

Write-Host "`n=== NEXT STEPS ===" -ForegroundColor Yellow
Write-Host "1. Go to: $orgUrl/$projectName/_settings/serviceconnections" -ForegroundColor White
Write-Host "2. Create a GitHub service connection and authorize it" -ForegroundColor White
Write-Host "3. Go to: $orgUrl/$projectName/_build" -ForegroundColor White
Write-Host "4. Click 'New Pipeline' and select your GitHub repository" -ForegroundColor White
Write-Host "5. Select 'Existing Azure Pipelines YAML file' and choose '/azure-pipelines.yml'" -ForegroundColor White
Write-Host "`nAll Azure resources and variables have been configured!" -ForegroundColor Green