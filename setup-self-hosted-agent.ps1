# Setup Self-Hosted Azure DevOps Agent
Write-Host "Setting up Self-Hosted Agent for Azure DevOps" -ForegroundColor Yellow

$org = "dbbuilder-dev"
$pat = "50lOB49w868Z6gCBhJqClT6NsnjeJAXhrFs3VbTR508lQih412weJQQJ99BGACAAAAAZ4Zw5AAASAZDO3OIt"
$agentName = "SqlAnalyzer-Agent"
$poolName = "Default"

# Download agent
Write-Host "`nDownloading Azure Pipelines Agent..." -ForegroundColor Cyan
$agentUrl = "https://vstsagentpackage.azureedge.net/agent/3.236.1/vsts-agent-win-x64-3.236.1.zip"
$agentZip = "agent.zip"
$agentDir = "azagent"

if (-not (Test-Path $agentDir)) {
    Invoke-WebRequest -Uri $agentUrl -OutFile $agentZip
    Expand-Archive -Path $agentZip -DestinationPath $agentDir
    Remove-Item $agentZip
}

cd $agentDir

Write-Host "`nConfiguring agent..." -ForegroundColor Cyan
Write-Host "This will register the agent with your Azure DevOps organization" -ForegroundColor Yellow

# Configure the agent
.\config.cmd --unattended `
    --url "https://dev.azure.com/$org" `
    --auth pat `
    --token $pat `
    --pool $poolName `
    --agent $agentName `
    --replace `
    --acceptTeeEula

Write-Host "`nAgent configured!" -ForegroundColor Green
Write-Host "`nTo run the agent:" -ForegroundColor Yellow
Write-Host "1. Interactive mode: .\run.cmd" -ForegroundColor White
Write-Host "2. As a service: .\svc.ps1 install followed by .\svc.ps1 start" -ForegroundColor White

cd ..