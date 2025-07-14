# SQL Analyzer - Vercel Deployment Guide

This guide covers deploying the SQL Analyzer Web UI to Vercel and the API to a separate cloud service.

## Architecture Overview

```
┌─────────────────┐         ┌─────────────────┐         ┌─────────────────┐
│   Vercel CDN    │         │ Cloud Service   │         │   Database      │
│   (Web UI)      │ ──API──▶│ (ASP.NET API)   │ ──SQL──▶│ (SQL Server)    │
│ sqlanalyzer.app │         │ api.domain.com  │         │                 │
└─────────────────┘         └─────────────────┘         └─────────────────┘
```

## Prerequisites

1. **Vercel Account**: Sign up at https://vercel.com
2. **Vercel CLI**: `npm install -g vercel`
3. **Git Repository**: Code pushed to GitHub
4. **API Hosting**: Azure, AWS, Railway, or similar for .NET hosting

## Step 1: Prepare for Deployment

### 1.1 Set Environment Variables

Create `.env.production.local` in `src/SqlAnalyzer.Web/`:

```env
# Your deployed API URL
VITE_API_URL=https://your-api-domain.com

# Optional settings
VITE_ENABLE_MOCK_DATA=false
VITE_ANALYSIS_POLL_INTERVAL=2000
```

### 1.2 Update API URL

Edit `src/SqlAnalyzer.Web/src/services/api.js`:

```javascript
const API_BASE_URL = import.meta.env.VITE_API_URL || 'http://localhost:5000';
```

## Step 2: Deploy Web UI to Vercel

### Option A: Using Vercel CLI

```bash
# One-time setup
vercel login

# Deploy preview
./deploy-vercel.sh

# Deploy to production
./deploy-vercel.sh --production
```

### Option B: Using GitHub Integration

1. Connect GitHub repository to Vercel
2. Configure build settings:
   - Framework Preset: `Vue.js`
   - Build Command: `npm run build`
   - Output Directory: `dist`
   - Install Command: `npm install`

3. Set environment variables in Vercel dashboard:
   ```
   VITE_API_URL = https://your-api-domain.com
   ```

### Option C: Manual Deployment

```bash
cd src/SqlAnalyzer.Web
npm install
npm run build
vercel --prod
```

## Step 3: Deploy API Separately

Since Vercel doesn't support .NET natively, deploy the API to one of these services:

### Option A: Azure App Service

```bash
# Build and publish
cd src/SqlAnalyzer.Api
dotnet publish -c Release -o ./publish

# Deploy using Azure CLI
az webapp up --name sqlanalyzer-api --os-type Linux --runtime "DOTNET|8.0"
```

### Option B: Railway

1. Connect GitHub repository to Railway
2. Add environment variables:
   ```
   ASPNETCORE_URLS=http://0.0.0.0:$PORT
   ConnectionStrings__DefaultConnection=your-connection-string
   ```

### Option C: AWS Lambda

Use AWS Lambda with .NET 8 runtime:

```bash
# Install Amazon.Lambda.Tools
dotnet tool install -g Amazon.Lambda.Tools

# Deploy
cd src/SqlAnalyzer.Api
dotnet lambda deploy-serverless
```

### Option D: Docker + Any Cloud

```bash
# Build Docker image
docker build -t sqlanalyzer-api -f src/SqlAnalyzer.Api/Dockerfile .

# Push to registry
docker tag sqlanalyzer-api your-registry/sqlanalyzer-api
docker push your-registry/sqlanalyzer-api
```

## Step 4: Configure Vercel Project

### 4.1 Environment Variables

In Vercel Dashboard → Project Settings → Environment Variables:

```
VITE_API_URL = https://sqlanalyzer-api.azurewebsites.net
VITE_ENABLE_MOCK_DATA = false
VITE_ANALYSIS_POLL_INTERVAL = 2000
```

### 4.2 Domain Configuration

1. Go to Project Settings → Domains
2. Add custom domain: `sqlanalyzer.yourdomain.com`
3. Configure DNS:
   ```
   CNAME    sqlanalyzer    cname.vercel-dns.com
   ```

### 4.3 CORS Configuration

Update API to allow Vercel domain:

```csharp
// Program.cs
builder.Services.AddCors(options =>
{
    options.AddPolicy("Production", policy =>
    {
        policy.WithOrigins(
            "https://sqlanalyzer.vercel.app",
            "https://sqlanalyzer.yourdomain.com"
        )
        .AllowAnyMethod()
        .AllowAnyHeader()
        .AllowCredentials();
    });
});
```

## Step 5: GitHub Actions Setup

### 5.1 Add Secrets

In GitHub repository settings → Secrets:

```
VERCEL_TOKEN = 3wRsfj8ZPCOe9CbvQf2qh6XA
VERCEL_ORG_ID = your-org-id
VERCEL_PROJECT_ID = your-project-id
```

### 5.2 Get Vercel IDs

```bash
# Link local directory to Vercel project
cd src/SqlAnalyzer.Web
vercel link

# Get project info
vercel env pull .env.vercel
cat .env.vercel
```

## Step 6: Post-Deployment

### 6.1 Verify Deployment

1. Check Web UI: https://sqlanalyzer.vercel.app
2. Test API connection: Network tab in browser DevTools
3. Verify SignalR: Real-time updates during analysis

### 6.2 Monitor Performance

Vercel Analytics:
- Page load times
- API response times
- Error rates

### 6.3 Set Up Alerts

1. Vercel: Deployment failures
2. API host: Server errors, high CPU
3. Database: Connection failures

## Troubleshooting

### CORS Errors

```javascript
// Check browser console
Access to fetch at 'https://api.domain.com' from origin 'https://sqlanalyzer.vercel.app' has been blocked by CORS policy
```

**Solution**: Update API CORS configuration

### API Connection Failed

```javascript
// Check API URL
console.log(import.meta.env.VITE_API_URL);
```

**Solution**: Verify environment variable in Vercel dashboard

### Build Failures

```bash
# Check build logs
vercel logs
```

**Common issues**:
- Missing dependencies
- TypeScript errors
- Environment variables not set

## Security Considerations

1. **API Keys**: Never commit to repository
2. **Connection Strings**: Use environment variables
3. **HTTPS**: Always use HTTPS in production
4. **Authentication**: Implement before production use
5. **Rate Limiting**: Configure on API

## Scaling Considerations

### Web UI (Vercel)
- Automatic scaling with CDN
- Edge functions for dynamic content
- Image optimization built-in

### API Scaling Options
1. **Horizontal**: Multiple API instances
2. **Caching**: Redis for repeated queries
3. **Database**: Read replicas for analysis

## Monitoring

### Vercel Analytics
```javascript
// Add to src/SqlAnalyzer.Web/index.html
<script>
  window.va = window.va || function () { (window.vaq = window.vaq || []).push(arguments); };
</script>
<script defer src="/_vercel/insights/script.js"></script>
```

### API Monitoring
- Application Insights (Azure)
- CloudWatch (AWS)
- Custom metrics endpoint

## Rollback Procedure

### Vercel
```bash
# List deployments
vercel ls

# Rollback to previous
vercel rollback [deployment-url]
```

### API
Depends on hosting platform:
- Azure: Deployment slots
- AWS: Blue/green deployments
- Docker: Tag previous version

## Cost Optimization

### Vercel (Free Tier)
- 100GB bandwidth/month
- Unlimited deployments
- Analytics included

### API Hosting
- Start with free tiers
- Scale based on usage
- Monitor costs weekly

## Support

- Vercel Documentation: https://vercel.com/docs
- Vercel Support: https://vercel.com/support
- GitHub Issues: Report bugs in repository