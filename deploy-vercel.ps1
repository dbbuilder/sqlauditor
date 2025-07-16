# SQL Analyzer Vercel Deployment Script
param(
    [Parameter(Mandatory=$false)]
    [string]$Environment = "preview",
    
    [Parameter(Mandatory=$false)]
    [string]$Token = "3wRsfj8ZPCOe9CbvQf2qh6XA",
    
    [switch]$Production,
    [switch]$SkipBuild
)

Write-Host "SQL Analyzer - Vercel Deployment" -ForegroundColor Cyan
Write-Host "================================" -ForegroundColor Cyan
Write-Host ""

# Set environment
if ($Production) {
    $Environment = "production"
}

# Check if Vercel CLI is installed
Write-Host "Checking Vercel CLI..." -NoNewline
try {
    vercel --version | Out-Null
    Write-Host " ✓" -ForegroundColor Green
} catch {
    Write-Host " ✗" -ForegroundColor Red
    Write-Host "Installing Vercel CLI..." -ForegroundColor Yellow
    npm install -g vercel
}

# Navigate to Web UI directory
Push-Location "src/SqlAnalyzer.Web"

try {
    # Install dependencies
    if (-not $SkipBuild) {
        Write-Host ""
        Write-Host "Installing dependencies..." -ForegroundColor Yellow
        npm install
        
        # Build the project
        Write-Host "Building for production..." -ForegroundColor Yellow
        npm run build
    }
    
    # Set Vercel token
    $env:VERCEL_TOKEN = $Token
    
    # Deploy to Vercel
    Write-Host ""
    Write-Host "Deploying to Vercel ($Environment)..." -ForegroundColor Yellow
    
    if ($Production) {
        vercel --prod --token $Token
    } else {
        vercel --token $Token
    }
    
    Write-Host ""
    Write-Host "Deployment complete!" -ForegroundColor Green
    
    # Show deployment info
    Write-Host ""
    Write-Host "Next steps:" -ForegroundColor Cyan
    Write-Host "1. Set environment variables in Vercel dashboard:" -ForegroundColor White
    Write-Host "   - VITE_API_URL: Your API endpoint" -ForegroundColor Gray
    Write-Host "   - VITE_ENABLE_MOCK_DATA: false" -ForegroundColor Gray
    Write-Host "2. Configure custom domain (optional)" -ForegroundColor White
    Write-Host "3. Set up API deployment separately" -ForegroundColor White
    
} catch {
    Write-Host "Deployment failed: $_" -ForegroundColor Red
    exit 1
} finally {
    Pop-Location
}