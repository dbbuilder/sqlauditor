# Create deployment package
Compress-Archive -Path ./publish/* -DestinationPath deploy.zip -Force

# Load Azure credentials
$envFile = Join-Path $PSScriptRoot "../../.env.azure.local"
$envContent = Get-Content $envFile
$credentials = @{}
foreach ($line in $envContent) {
    if ($line -match '^([^#=]+)=(.*)$') {
        $key = $matches[1].Trim()
        $value = $matches[2].Trim()
        $credentials[$key] = $value
    }
}

# Deploy using Azure CLI
az login --service-principal `
    --username $credentials["AZURE_CLIENT_ID"] `
    --password $credentials["AZURE_CLIENT_SECRET"] `
    --tenant $credentials["AZURE_TENANT_ID"]

az webapp deployment source config-zip `
    --name sqlanalyzer-api `
    --resource-group rg-sqlanalyzer `
    --src deploy.zip

Write-Host "Deployment complete!"