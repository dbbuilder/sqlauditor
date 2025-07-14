# Setup GitHub Repository with New Structure
Write-Host "=== Setting up GitHub Repository ===" -ForegroundColor Cyan

# Check if we're in the right directory
if (!(Test-Path "frontend") -or !(Test-Path "api")) {
    Write-Host "❌ Missing frontend or api directories" -ForegroundColor Red
    exit 1
}

# Initialize git if needed
if (!(Test-Path ".git")) {
    Write-Host "Initializing git repository..." -ForegroundColor Yellow
    git init
}

# Stage all changes
Write-Host "`nStaging changes..." -ForegroundColor Yellow
git add -A

# Commit the new structure
Write-Host "Committing new structure..." -ForegroundColor Yellow
git commit -m "Reorganize project into monorepo structure

- Move frontend to frontend/ directory
- Move API to api/ directory  
- Update .gitignore
- Add documentation for new structure"

# Create repository on GitHub
Write-Host "`nCreating GitHub repository..." -ForegroundColor Yellow
gh repo create sqlauditor --public --source=. --remote=origin --description="SQL Database Analyzer - Monorepo with Vue.js frontend and .NET API"

if ($LASTEXITCODE -eq 0) {
    Write-Host "✅ Repository created successfully!" -ForegroundColor Green
    
    # Push to GitHub
    Write-Host "`nPushing to GitHub..." -ForegroundColor Yellow
    git push -u origin main 2>&1
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Trying master branch..." -ForegroundColor Yellow
        git branch -m main master
        git push -u origin master
    }
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✅ Code pushed successfully!" -ForegroundColor Green
        
        # Get deployment token for frontend
        Write-Host "`n=== Frontend Deployment Configuration ===" -ForegroundColor Cyan
        $token = az staticwebapp secrets list `
            --name sqlanalyzer-web `
            --resource-group rg-sqlanalyzer `
            --query "properties.apiKey" -o tsv
        
        if ($token) {
            Write-Host "✅ Retrieved deployment token" -ForegroundColor Green
            
            # Add secret to GitHub
            Write-Host "`nAdding deployment token to GitHub secrets..." -ForegroundColor Yellow
            echo $token | gh secret set AZURE_STATIC_WEB_APPS_API_TOKEN
            
            if ($LASTEXITCODE -eq 0) {
                Write-Host "✅ Deployment token added to GitHub!" -ForegroundColor Green
            } else {
                Write-Host "❌ Failed to add secret. Add manually at:" -ForegroundColor Red
                Write-Host "   https://github.com/settings/secrets/actions" -ForegroundColor Gray
                Write-Host "   Name: AZURE_STATIC_WEB_APPS_API_TOKEN" -ForegroundColor Gray
                Write-Host "   Value: $token" -ForegroundColor Yellow
            }
        }
        
        # Open repository
        Write-Host "`nOpening repository..." -ForegroundColor Yellow
        gh repo view --web
    }
} else {
    Write-Host "❌ Failed to create repository" -ForegroundColor Red
    Write-Host "The repository might already exist or you need to authenticate with:" -ForegroundColor Yellow
    Write-Host "gh auth login" -ForegroundColor Gray
}

Write-Host "`n=== Next Steps ===" -ForegroundColor Cyan
Write-Host "1. Configure GitHub Actions workflows for deployment" -ForegroundColor White
Write-Host "2. Update Azure Static Web App to deploy from frontend/ directory" -ForegroundColor White
Write-Host "3. Configure API deployment workflow" -ForegroundColor White