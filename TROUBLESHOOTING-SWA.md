# Troubleshooting Azure Static Web App Deployment

## Current Issue
The Azure Static Web App at https://black-desert-02d93d30f.2.azurestaticapps.net is serving the correct HTML but the Vue app is not rendering.

## Diagnostic Steps

### 1. Check Browser Console
Open the URL in a browser and check the Developer Console (F12) for:
- JavaScript errors
- Failed network requests
- CORS issues
- Missing resources

### 2. Common Issues and Solutions

#### Issue: Blank Page with No Errors
**Possible Causes:**
1. JavaScript module loading failure
2. SignalR connection failing on startup
3. API connection issues preventing app initialization

**Solutions:**
```javascript
// Check if running in production
console.log(import.meta.env.VITE_API_URL)
console.log(import.meta.env.VITE_ENABLE_MOCK_DATA)
```

#### Issue: CORS Errors
**Error:** `Access to fetch at 'https://sqlanalyzer-api.azurewebsites.net' from origin 'https://black-desert-02d93d30f.2.azurestaticapps.net' has been blocked by CORS policy`

**Solution:** API needs to be redeployed with updated CORS settings that include the SWA URL.

#### Issue: SignalR Connection Failure
**Error:** `Failed to start the connection: Error`

**Solution:** The app might be failing to initialize due to SignalR. Check if the API SignalR endpoint is accessible.

### 3. Quick Fixes to Try

#### A. Redeploy with Error Handling
Add error handling to prevent app crash:
```javascript
// In App.vue
onMounted(() => {
  try {
    analysisStore.initializeSignalR()
  } catch (error) {
    console.error('SignalR initialization failed:', error)
  }
})
```

#### B. Check Asset Loading
Verify all assets are loading:
```bash
curl -I https://black-desert-02d93d30f.2.azurestaticapps.net/assets/index-Cqh46ZPP.js
curl -I https://black-desert-02d93d30f.2.azurestaticapps.net/assets/index-CgDbBy7C.css
```

#### C. Test with Mock Mode
Temporarily enable mock mode to isolate API issues:
1. Set `VITE_ENABLE_MOCK_DATA=true`
2. Rebuild and redeploy
3. If it works, the issue is API-related

### 4. Manual Verification Steps

1. **Check Deployment Files:**
   ```bash
   ls -la dist/
   cat dist/index.html
   ```

2. **Verify SWA Configuration:**
   ```bash
   cat dist/staticwebapp.config.json
   ```

3. **Check Build Output:**
   Look for warnings during `npm run build`

### 5. Redeployment Steps

```bash
# Clean build
cd src/SqlAnalyzer.Web
rm -rf dist node_modules/.cache

# Install and build
npm install
npm run build

# Copy config
cp staticwebapp.config.json dist/

# Deploy with SWA CLI
npx @azure/static-web-apps-cli deploy ./dist \
  --deployment-token <token> \
  --env production
```

### 6. Alternative: Deploy Simple Test Page

To verify SWA is working:
```bash
echo '<h1>Test Page</h1>' > dist/test.html
# Deploy and check https://[your-swa].azurestaticapps.net/test.html
```

### 7. Check API Accessibility
```bash
# From the SWA domain
curl https://sqlanalyzer-api.azurewebsites.net/api/version
```

## Root Cause Analysis

The most likely causes:
1. **CORS Configuration**: API not accepting requests from SWA domain
2. **SignalR Initialization**: App crashes when SignalR fails to connect
3. **Environment Variables**: Build not using correct production values
4. **Module Loading**: ES modules not loading correctly

## Recommended Next Steps

1. Open browser console and check for specific errors
2. Temporarily disable SignalR initialization
3. Verify API CORS includes SWA URL
4. Check if running in mock mode would work
5. Review Vite build output for warnings