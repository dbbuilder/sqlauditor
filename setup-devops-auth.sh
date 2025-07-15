#!/bin/bash
# Setup Azure DevOps CLI with PAT

echo "Setting up Azure DevOps CLI authentication..."

# Option 1: Set PAT as environment variable (persists for session)
export AZURE_DEVOPS_EXT_PAT="YOUR_PAT_HERE"

# Option 2: Login with PAT (interactive)
# echo "YOUR_PAT_HERE" | az devops login --organization https://dev.azure.com/dbbuilder-dev

# Option 3: Create a .env file for permanent storage
cat > ~/.azure-devops-cli.env << 'EOF'
# Azure DevOps CLI Configuration
export AZURE_DEVOPS_EXT_PAT="YOUR_PAT_HERE"
export AZURE_DEVOPS_ORG="https://dev.azure.com/dbbuilder-dev"
export AZURE_DEVOPS_PROJECT="SQLAnalyzer"
EOF

echo "Add this to your .bashrc or .zshrc to make it permanent:"
echo "source ~/.azure-devops-cli.env"

# Test the connection
echo "Testing connection..."
az devops project show --project SQLAnalyzer --org https://dev.azure.com/dbbuilder-dev