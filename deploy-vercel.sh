#!/bin/bash

# SQL Analyzer Vercel Deployment Script

set -e

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
NC='\033[0m'

# Default values
ENVIRONMENT="preview"
TOKEN="3wRsfj8ZPCOe9CbvQf2qh6XA"
SKIP_BUILD=false
PRODUCTION=false

# Parse arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        --production|-p)
            PRODUCTION=true
            ENVIRONMENT="production"
            shift
            ;;
        --token|-t)
            TOKEN="$2"
            shift 2
            ;;
        --skip-build)
            SKIP_BUILD=true
            shift
            ;;
        *)
            echo "Unknown option: $1"
            exit 1
            ;;
    esac
done

echo -e "${CYAN}SQL Analyzer - Vercel Deployment${NC}"
echo -e "${CYAN}================================${NC}"
echo ""

# Check if Vercel CLI is installed
echo -n "Checking Vercel CLI..."
if command -v vercel &> /dev/null; then
    echo -e " ${GREEN}✓${NC}"
else
    echo -e " ${RED}✗${NC}"
    echo -e "${YELLOW}Installing Vercel CLI...${NC}"
    npm install -g vercel
fi

# Navigate to Web UI directory
cd src/SqlAnalyzer.Web

# Install dependencies and build
if [ "$SKIP_BUILD" = false ]; then
    echo ""
    echo -e "${YELLOW}Installing dependencies...${NC}"
    npm install
    
    echo -e "${YELLOW}Building for production...${NC}"
    npm run build
fi

# Set Vercel token
export VERCEL_TOKEN=$TOKEN

# Deploy to Vercel
echo ""
echo -e "${YELLOW}Deploying to Vercel ($ENVIRONMENT)...${NC}"

if [ "$PRODUCTION" = true ]; then
    vercel --prod --token $TOKEN
else
    vercel --token $TOKEN
fi

echo ""
echo -e "${GREEN}Deployment complete!${NC}"

# Show deployment info
echo ""
echo -e "${CYAN}Next steps:${NC}"
echo -e "1. Set environment variables in Vercel dashboard:"
echo -e "   - VITE_API_URL: Your API endpoint"
echo -e "   - VITE_ENABLE_MOCK_DATA: false"
echo -e "2. Configure custom domain (optional)"
echo -e "3. Set up API deployment separately"