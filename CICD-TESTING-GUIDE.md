# CI/CD Testing Guide for SQL Analyzer

## Overview

This guide provides comprehensive instructions for testing and verifying the CI/CD pipeline for SQL Analyzer. The pipeline includes version tagging, deployment verification, and multiple testing layers.

## Version Tracking System

### Version Format
- **Pattern**: `YYYY.MM.DD.BUILD_NUMBER`
- **Example**: `2025.07.10.42`
- **Components**:
  - Year.Month.Day: Date of build
  - Build Number: GitHub Actions run number

### Deployment ID
- **Pattern**: `BUILD_NUMBER-COMMIT_SHA`
- **Example**: `42-a1b2c3d`
- **Purpose**: Unique identifier for each deployment

## Testing Layers

### 1. Local Testing

Test the CI/CD process locally before pushing:

```powershell
# Run local CI/CD simulation
./test-cicd-locally.ps1 -RunNumber 99

# Skip build/tests for faster iteration
./test-cicd-locally.ps1 -SkipBuild -SkipTests
```

This script:
- Simulates GitHub Actions environment variables
- Builds with version information
- Creates version.json file
- Runs API locally and tests endpoints

### 2. Pre-Deployment Verification

Before triggering deployment:

1. **Check current deployment**:
   ```powershell
   ./verify-deployment.ps1
   ```

2. **Save current version info**:
   ```powershell
   $current = Invoke-RestMethod https://sqlanalyzer-api.azurewebsites.net/api/version
   $current.deployment.deploymentId
   ```

### 3. GitHub Actions Workflow

The main deployment workflow (`deploy-api.yml`) includes:

1. **Version Generation**
   - Creates version based on date and run number
   - Tags build with commit SHA
   - Generates unique deployment ID

2. **Build & Test**
   - Updates project files with version
   - Runs all unit tests
   - Creates deployment package

3. **Deployment**
   - Deploys to Azure App Service
   - Sets environment variables
   - Creates version.json file

4. **Verification**
   - Waits for deployment to stabilize
   - Checks version endpoint
   - Verifies deployment ID matches
   - Creates GitHub release

### 4. Post-Deployment Testing

Run deployment tests via GitHub Actions:

```bash
# Trigger smoke tests
gh workflow run test-deployment.yml -f test_type=smoke

# Run full integration tests
gh workflow run test-deployment.yml -f test_type=full

# Run performance tests
gh workflow run test-deployment.yml -f test_type=performance
```

Or manually verify:

```powershell
# Basic verification
./verify-deployment.ps1

# Verify specific version
./verify-deployment.ps1 -ExpectedVersion "2025.07.10.42"

# Detailed output
./verify-deployment.ps1 -Detailed
```

## Key Endpoints for Testing

### Version Information
```bash
GET https://sqlanalyzer-api.azurewebsites.net/api/version
```

Response includes:
- Assembly version
- Deployment ID
- Build timestamp
- Commit SHA
- Runtime information

### Health Checks
```bash
# Simple health
GET https://sqlanalyzer-api.azurewebsites.net/api/version/health

# Main health endpoint
GET https://sqlanalyzer-api.azurewebsites.net/health
```

### API Functionality
```bash
# Get analysis types
GET https://sqlanalyzer-api.azurewebsites.net/api/v1/analysis/types

# Test connection
POST https://sqlanalyzer-api.azurewebsites.net/api/v1/analysis/test-connection
```

## Verification Checklist

### Pre-Deployment
- [ ] Local build succeeds
- [ ] All tests pass
- [ ] Version endpoint works locally
- [ ] Current production version noted

### During Deployment
- [ ] GitHub Actions workflow starts
- [ ] Build completes successfully
- [ ] Tests pass in CI
- [ ] Deployment package created
- [ ] Azure deployment succeeds

### Post-Deployment
- [ ] Version endpoint returns new version
- [ ] Deployment ID matches expected
- [ ] Health checks pass
- [ ] API endpoints respond
- [ ] Performance is acceptable
- [ ] GitHub release created

## Troubleshooting

### Deployment Verification Fails

1. **Check deployment logs**:
   ```bash
   az webapp log tail --name sqlanalyzer-api --resource-group rg-sqlanalyzer
   ```

2. **Check app settings**:
   ```bash
   az webapp config appsettings list --name sqlanalyzer-api --resource-group rg-sqlanalyzer
   ```

3. **Restart app service**:
   ```bash
   az webapp restart --name sqlanalyzer-api --resource-group rg-sqlanalyzer
   ```

### Version Mismatch

1. Check if deployment completed:
   - Azure Portal > App Service > Deployment Center
   - Check deployment status

2. Verify environment variables:
   - BUILD_TIMESTAMP
   - DEPLOYMENT_ID
   - GITHUB_SHA

3. Check version.json file:
   ```bash
   curl https://sqlanalyzer-api.azurewebsites.net/version.json
   ```

### Performance Issues

1. Check app service metrics:
   - CPU usage
   - Memory usage
   - Response times

2. Review application logs:
   ```bash
   az webapp log download --name sqlanalyzer-api --resource-group rg-sqlanalyzer
   ```

3. Scale up if needed:
   ```bash
   az appservice plan update --name asp-sqlanalyzer-linux --resource-group rg-sqlanalyzer --sku B2
   ```

## Setting Up GitHub Secrets

Required secrets for CI/CD:

1. **AZURE_WEBAPP_PUBLISH_PROFILE**:
   ```bash
   az webapp deployment list-publishing-profiles \
     --name sqlanalyzer-api \
     --resource-group rg-sqlanalyzer \
     --xml > publish-profile.xml
   ```
   Copy contents to GitHub secret

2. **AZURE_CREDENTIALS** (for app settings):
   ```json
   {
     "clientId": "xxx",
     "clientSecret": "xxx",
     "subscriptionId": "xxx",
     "tenantId": "xxx"
   }
   ```

## Best Practices

1. **Always test locally first** using `test-cicd-locally.ps1`
2. **Note current version** before deploying
3. **Use deployment ID** for tracking specific deployments
4. **Monitor the deployment** in real-time via GitHub Actions
5. **Verify immediately** after deployment completes
6. **Keep version history** through GitHub releases

## Example Test Scenario

1. Make code changes
2. Run local test:
   ```powershell
   ./test-cicd-locally.ps1
   ```
3. Commit and push:
   ```bash
   git add .
   git commit -m "feat: Add new analysis feature"
   git push origin main
   ```
4. Monitor GitHub Actions
5. After deployment, verify:
   ```powershell
   ./verify-deployment.ps1
   ```
6. Run integration tests:
   ```bash
   gh workflow run test-deployment.yml -f test_type=full
   ```

## Version History Tracking

Each deployment creates:
- GitHub release with version tag
- Deployment summary in Actions
- Version endpoint showing current deployment
- Audit trail through Azure deployment history

This comprehensive system ensures every deployment is tracked, verified, and can be audited.