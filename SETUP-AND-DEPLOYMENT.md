# SQL Analyzer - Complete Setup and Deployment Guide

## Quick Start

### Local Development
```bash
# Windows PowerShell
.\setup-local.ps1
.\run-local.ps1

# Linux/WSL
./setup-local.sh
./run-local.sh
```

### Full Azure Deployment
```bash
# Windows PowerShell
.\deploy-complete.ps1 -AdminPassword "YourSecurePassword"

# Linux/WSL
./deploy-complete.sh --admin-password "YourSecurePassword"
```

## Authentication System

### Default Credentials
- **Username**: `admin`
- **Password**: `SqlAnalyzer2024!`

### Security Features
- JWT token authentication (24-hour expiry)
- All API endpoints protected
- Secure password storage
- CORS configured for production URLs

## Scripts Overview

### 1. Setup Scripts

#### `setup-local.ps1` / `setup-local.sh`
- Installs all dependencies (backend & frontend)
- Creates local configuration files
- Sets up development environment
- Creates run scripts

#### `run-local.ps1` / `run-local.sh`
- Starts both API and frontend
- API runs on http://localhost:5274
- UI runs on http://localhost:5173
- Shows login credentials

### 2. Deployment Scripts

#### `deploy-complete.ps1` / `deploy-complete.sh`
Complete deployment script that:
- Builds backend and frontend
- Runs tests (optional with `-SkipTests`)
- Deploys API to Azure App Service
- Configures authentication settings
- Verifies deployment
- Generates secure JWT secret

**Options:**
- `--jwt-secret`: Provide your own JWT secret (64+ chars)
- `--admin-password`: Change admin password
- `--skip-tests`: Skip running tests

**Example:**
```bash
# PowerShell
.\deploy-complete.ps1 -AdminPassword "MySecurePassword123!" -SkipTests

# Bash
./deploy-complete.sh --admin-password "MySecurePassword123!" --skip-tests
```

### 3. Testing Scripts

#### `test-auth.ps1`
Tests authentication locally:
- Verifies API health
- Tests login endpoint
- Validates token generation
- Tests protected endpoints
- Verifies wrong credentials are rejected

#### `verify-azure-deployment.ps1`
Verifies Azure deployment:
- Checks API health
- Tests authentication
- Validates CORS configuration
- Confirms UI accessibility

## Step-by-Step Setup

### Local Development

1. **Clone the repository**
   ```bash
   git clone [your-repo]
   cd sqlauditor
   ```

2. **Run setup**
   ```bash
   .\setup-local.ps1
   ```

3. **Start the application**
   ```bash
   .\run-local.ps1
   ```

4. **Open browser**
   - Navigate to http://localhost:5173
   - Login with `admin` / `SqlAnalyzer2024!`

### Production Deployment

1. **Configure Azure credentials**
   Create `.env.azure.local`:
   ```
   AZURE_CLIENT_ID=your-client-id
   AZURE_CLIENT_SECRET=your-client-secret
   AZURE_SUBSCRIPTION_ID=your-subscription-id
   AZURE_TENANT_ID=your-tenant-id
   ```

2. **Run deployment**
   ```bash
   .\deploy-complete.ps1 -AdminPassword "YourSecurePassword"
   ```

3. **Wait for deployment**
   - API deploys via ZIP deploy
   - Frontend deploys via GitHub Actions
   - Takes approximately 5-10 minutes

4. **Verify deployment**
   ```bash
   .\verify-azure-deployment.ps1
   ```

5. **Access the application**
   - Open https://black-desert-02d93d30f.2.azurestaticapps.net
   - Login with your configured credentials

## Configuration

### API Settings (appsettings.json)
```json
{
  "Authentication": {
    "JwtSecret": "your-secret-key",
    "JwtExpirationHours": 24,
    "DefaultUsername": "admin",
    "DefaultPassword": "SqlAnalyzer2024!"
  }
}
```

### Azure App Settings
Configure in Azure Portal or via CLI:
- `Authentication__JwtSecret`
- `Authentication__DefaultPassword`
- `Authentication__JwtExpirationHours`

### Frontend Environment (.env)
```
VITE_API_URL=https://sqlanalyzer-api.azurewebsites.net
VITE_ENABLE_MOCK_DATA=false
```

## Troubleshooting

### Common Issues

1. **"Unauthorized" errors**
   - Check token hasn't expired (24 hours)
   - Verify credentials are correct
   - Clear browser cache/localStorage

2. **CORS errors**
   - Remove Azure Portal CORS entries
   - Ensure app CORS includes your URL
   - Restart API after changes

3. **Login fails**
   - Verify API is running
   - Check password configuration
   - Review API logs for errors

4. **Can't access protected endpoints**
   - Ensure token is included in requests
   - Check token hasn't expired
   - Verify [Authorize] attribute on controllers

### Logs and Diagnostics

#### View API logs
```bash
az webapp log tail --name sqlanalyzer-api --resource-group rg-sqlanalyzer
```

#### Test authentication
```bash
.\test-auth.ps1  # Local
.\verify-azure-deployment.ps1  # Production
```

## Security Best Practices

1. **Change default password** immediately in production
2. **Use strong JWT secret** (64+ characters)
3. **Enable HTTPS** only in production
4. **Rotate credentials** regularly
5. **Monitor access logs** for suspicious activity

## Next Steps

### Expand Authentication
- Add user registration
- Implement password reset
- Add role-based access control
- Enable multi-factor authentication

### Integration Options
- Azure Active Directory
- OAuth2/OpenID Connect
- External identity providers

## Support

For issues or questions:
1. Check the troubleshooting section
2. Review API logs
3. Run verification scripts
4. Check GitHub Actions for deployment status