# Accessing Hangfire in Production

Due to the complexity of integrating Hangfire's dashboard with JWT authentication, here are the recommended approaches:

## Option 1: Use API Endpoints (Recommended)

Instead of accessing the Hangfire dashboard directly, use the API endpoints we've created:

```javascript
// Get Hangfire statistics
const response = await fetch('https://sqlanalyzer-api-win.azurewebsites.net/api/v1/hangfire/stats', {
    headers: {
        'Authorization': `Bearer ${token}`
    }
});
const stats = await response.json();
console.log('Hangfire Stats:', stats);

// Get recent jobs
const jobsResponse = await fetch('https://sqlanalyzer-api-win.azurewebsites.net/api/v1/hangfire/jobs', {
    headers: {
        'Authorization': `Bearer ${token}`
    }
});
const jobs = await jobsResponse.json();
```

## Option 2: Temporary Disable Authentication (Development Only)

In appsettings.Development.json:
```json
{
  "Hangfire": {
    "RequireAuthentication": false
  }
}
```

## Option 3: Access from Azure Portal

1. Go to Azure Portal
2. Navigate to your App Service
3. Use Kudu console (Advanced Tools)
4. Access https://sqlanalyzer-api-win.scm.azurewebsites.net
5. The Hangfire dashboard might be accessible from there

## Option 4: Create Admin Page in Frontend

Add a simple admin page to your Vue app that displays Hangfire stats:

```vue
<template>
  <div class="hangfire-stats">
    <h2>Background Jobs</h2>
    <div class="stats-grid">
      <div class="stat-card">
        <h3>{{ stats.enqueued }}</h3>
        <p>Queued</p>
      </div>
      <div class="stat-card">
        <h3>{{ stats.processing }}</h3>
        <p>Processing</p>
      </div>
      <div class="stat-card">
        <h3>{{ stats.succeeded }}</h3>
        <p>Completed</p>
      </div>
      <div class="stat-card">
        <h3>{{ stats.failed }}</h3>
        <p>Failed</p>
      </div>
    </div>
  </div>
</template>

<script>
export default {
  data() {
    return {
      stats: {
        enqueued: 0,
        processing: 0,
        succeeded: 0,
        failed: 0
      }
    }
  },
  async mounted() {
    const response = await this.$api.get('/hangfire/stats')
    this.stats = response.data
  }
}
</script>
```

## Current Status

The Hangfire dashboard at `/hangfire` is protected by authentication in production. The authorization filter checks for:
1. Local access (localhost)
2. Authenticated users with "Admin" role

Since JWT tokens aren't easily passed to the Hangfire dashboard (it's a separate web application), the API endpoints provide a secure alternative.

## For Immediate Use

To check if Hangfire is working:

1. **Check job processing via API**:
   ```bash
   curl -X GET "https://sqlanalyzer-api-win.azurewebsites.net/api/v1/hangfire/stats" \
        -H "Authorization: Bearer YOUR_JWT_TOKEN"
   ```

2. **Monitor via Application Logs**:
   - Check Azure App Service logs
   - Look for "Hangfire" entries
   - Job processing is logged

3. **Use the Analysis Status endpoint**:
   - When you start an analysis, use the status endpoint to monitor progress
   - This works regardless of Hangfire dashboard access