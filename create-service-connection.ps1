# Create Azure Service Connection for SQL Analyzer
Write-Host "Creating Azure Service Connection for SQL Analyzer..." -ForegroundColor Yellow

# Variables
$orgUrl = "https://dev.azure.com/dbbuilder-dev"
$projectName = "SQLAnalyzer"
$serviceConnectionName = "SqlAnalyzer-ServiceConnection"

# Login check
Write-Host "Checking Azure DevOps login status..." -ForegroundColor Cyan
$loginCheck = az devops project list --org $orgUrl 2>&1

if ($LASTEXITCODE -ne 0) {
    Write-Host "Please login to Azure DevOps first:" -ForegroundColor Red
    Write-Host "Run: az devops login" -ForegroundColor Yellow
    Write-Host "Then create a PAT token at: $orgUrl/_usersSettings/tokens" -ForegroundColor Yellow
    exit 1
}

# Set defaults
az devops configure --defaults organization=$orgUrl project=$projectName

# Get subscription details
$subscriptionId = az account show --query id -o tsv
$subscriptionName = az account show --query name -o tsv
$tenantId = az account show --query tenantId -o tsv

Write-Host "Using subscription: $subscriptionName ($subscriptionId)" -ForegroundColor Green

# Method 1: Try using Azure DevOps CLI
Write-Host "`nAttempting to create service connection via CLI..." -ForegroundColor Cyan

$serviceEndpoint = az devops service-endpoint azurerm create `
    --azure-rm-service-principal-id "" `
    --azure-rm-subscription-id $subscriptionId `
    --azure-rm-subscription-name $subscriptionName `
    --azure-rm-tenant-id $tenantId `
    --name $serviceConnectionName `
    --org $orgUrl `
    --project $projectName 2>&1

if ($LASTEXITCODE -eq 0) {
    Write-Host "Service connection created successfully!" -ForegroundColor Green
    
    # Get the ID and enable for all pipelines
    $endpointId = az devops service-endpoint list `
        --org $orgUrl `
        --project $projectName `
        --query "[?name=='$serviceConnectionName'].id" -o tsv
    
    az devops service-endpoint update `
        --id $endpointId `
        --org $orgUrl `
        --project $projectName `
        --enable-for-all true
        
} else {
    Write-Host "CLI method failed. Creating service connection via REST API..." -ForegroundColor Yellow
    
    # Method 2: Use REST API
    $projectId = az devops project show --project $projectName --org $orgUrl --query id -o tsv
    
    # Create service principal
    Write-Host "Creating service principal..." -ForegroundColor Cyan
    $spName = "sp-sqlanalyzer-$((Get-Date).Ticks)"
    $sp = az ad sp create-for-rbac `
        --name $spName `
        --role Contributor `
        --scopes "/subscriptions/$subscriptionId" `
        --query "{clientId:appId, clientSecret:password, tenantId:tenant}" | ConvertFrom-Json
    
    if ($sp) {
        Write-Host "Service principal created" -ForegroundColor Green
        
        # Create service endpoint JSON
        $serviceEndpointBody = @{
            data = @{
                subscriptionId = $subscriptionId
                subscriptionName = $subscriptionName
                environment = "AzureCloud"
                scopeLevel = "Subscription"
                creationMode = "Manual"
            }
            name = $serviceConnectionName
            type = "azurerm"
            url = "https://management.azure.com/"
            authorization = @{
                parameters = @{
                    tenantid = $tenantId
                    serviceprincipalid = $sp.clientId
                    authenticationType = "spnKey"
                    serviceprincipalkey = $sp.clientSecret
                }
                scheme = "ServicePrincipal"
            }
            isShared = $false
            isReady = $true
            serviceEndpointProjectReferences = @(
                @{
                    projectReference = @{
                        id = $projectId
                        name = $projectName
                    }
                    name = $serviceConnectionName
                }
            )
        } | ConvertTo-Json -Depth 10
        
        # Save for manual entry if needed
        $serviceEndpointBody | Out-File -FilePath "service-connection.json" -Encoding UTF8
        
        Write-Host "`nService connection configuration saved to service-connection.json" -ForegroundColor Green
        Write-Host "Service Principal ID: $($sp.clientId)" -ForegroundColor Cyan
        Write-Host "`nTo complete setup manually:" -ForegroundColor Yellow
        Write-Host "1. Go to: $orgUrl/$projectName/_settings/adminservices" -ForegroundColor White
        Write-Host "2. Click 'New service connection' > 'Azure Resource Manager'" -ForegroundColor White
        Write-Host "3. Choose 'Service principal (manual)'" -ForegroundColor White
        Write-Host "4. Enter the details from service-connection.json" -ForegroundColor White
    }
}

Write-Host "`nDone!" -ForegroundColor Green