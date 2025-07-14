# CORS Fix Instructions for SQL Analyzer

## The Problem
Azure Portal CORS settings are overriding the application's CORS configuration, preventing the Static Web App from accessing the API.

## Quick Fix (Via Azure Portal)

### Option 1: Remove Azure CORS (Recommended)
1. Go to [Azure Portal](https://portal.azure.com)
2. Navigate to: **Resource Groups** → **rg-sqlanalyzer** → **sqlanalyzer-api**
3. Click on **CORS** under Settings
4. **Remove ALL entries** (click the X on each origin)
5. Click **Save**
6. Go to **Overview** and click **Restart**

This allows the application's CORS configuration to take effect, which already includes the correct origins.

### Option 2: Add Origins to Azure CORS
If removing doesn't work, add these origins in Azure Portal:
- `https://black-desert-02d93d30f.2.azurestaticapps.net`
- `https://sqlanalyzer-web.azurestaticapps.net`
- `http://localhost:5173`
- `http://localhost:3000`

Enable "Access-Control-Allow-Credentials" if available.

## Why This Happens
1. Azure App Service CORS takes precedence over application CORS
2. When Azure CORS is configured, it blocks the app's CORS middleware
3. The app already has correct CORS configuration in Program.cs

## Verification
After applying the fix:
1. Clear browser cache (Ctrl+Shift+R)
2. Open https://black-desert-02d93d30f.2.azurestaticapps.net
3. Open Developer Tools (F12)
4. Check Console - no CORS errors
5. Check Network tab - API calls should succeed

## Current App CORS Configuration
The API code already includes:
```csharp
allowedOrigins.Add("https://sqlanalyzer-web.azurestaticapps.net");
allowedOrigins.Add("https://black-desert-02d93d30f.2.azurestaticapps.net");
```

Plus it reads from app settings:
- `SqlAnalyzer__AllowedOrigins__0`
- `SqlAnalyzer__AllowedOrigins__1`

## If Still Having Issues
1. Check if API is running: https://sqlanalyzer-api.azurewebsites.net/api/version
2. Try in incognito/private browsing mode
3. Check if SignalR specific CORS is needed
4. Verify the API restart completed

## Success Indicators
- No CORS errors in console
- API calls show 200 status
- SignalR connects (or at least attempts to)
- UI loads completely