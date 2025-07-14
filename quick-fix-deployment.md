# Quick Fix for SQL Analyzer Deployment

## Current Status

### ✅ Fixed Issues
1. **PrimeVue Toast Error**: Added `ToastService` to main.js
2. **CORS Configuration**: Added specific SWA URL to allowed origins

### 🚀 UI Status
- **URL**: https://black-desert-02d93d30f.2.azurestaticapps.net
- **Status**: Deployed with fixes
- **New JS Bundle**: index-BwL4jy3_.js (deployed)

### ⚠️ API Status
- **URL**: https://sqlanalyzer-api.azurewebsites.net
- **CORS Fix**: Added but needs deployment
- **Issue**: Deployment failing with 400 error

## Quick Fix Steps

### Option 1: Use App Settings for CORS (Immediate Fix)
```bash
# Add the SWA URL to allowed origins via app settings
az webapp config appsettings set \
  --name sqlanalyzer-api \
  --resource-group rg-sqlanalyzer \
  --settings \
    SqlAnalyzer__AllowedOrigins__0=https://black-desert-02d93d30f.2.azurestaticapps.net \
    SqlAnalyzer__AllowedOrigins__1=https://sqlanalyzer-web.azurestaticapps.net

# Restart the API
az webapp restart --name sqlanalyzer-api --resource-group rg-sqlanalyzer
```

### Option 2: Deploy via GitHub Actions
1. Commit the CORS changes
2. Push to trigger GitHub Actions deployment
3. Monitor deployment in Actions tab

### Option 3: Manual Deployment via Kudu
1. Go to: https://sqlanalyzer-api.scm.azurewebsites.net
2. Debug Console → CMD
3. Navigate to site/wwwroot
4. Upload files manually

## Testing the Fix

1. **Open the UI**: https://black-desert-02d93d30f.2.azurestaticapps.net
2. **Check Console**: Should no longer show PrimeVue Toast error
3. **Check Network**: CORS errors should be resolved after API fix

## Expected Result

Once both fixes are deployed:
- ✅ No PrimeVue Toast error
- ✅ No CORS errors
- ✅ SignalR connects successfully
- ✅ UI renders properly

## Notes

- The UI fix is already deployed
- Only the API CORS fix is pending
- Using app settings is the quickest fix without redeployment