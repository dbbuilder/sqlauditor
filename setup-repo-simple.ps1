# Simple GitHub Repository Setup
Write-Host "Setting up GitHub repository..." -ForegroundColor Cyan

# Initialize git
git init

# Add all files
git add -A

# Commit
git commit -m "Reorganize into monorepo structure with frontend and api folders"

# Create repository
Write-Host "Creating GitHub repository..." -ForegroundColor Yellow
gh repo create sqlauditor --public --source=. --remote=origin --description="SQL Database Analyzer - Monorepo"

# Push
Write-Host "Pushing to GitHub..." -ForegroundColor Yellow
git push -u origin main

Write-Host "Done! Repository created and pushed." -ForegroundColor Green