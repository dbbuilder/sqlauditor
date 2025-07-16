# Hangfire Integration for SQL Analyzer

## Overview

We've integrated Hangfire to provide robust background job processing for database analysis tasks. This makes the analysis process more responsive and resilient.

## Key Benefits

1. **Persistent Job Storage**
   - Jobs survive application restarts
   - In-memory storage for development
   - SQL Server storage for production

2. **Better Performance**
   - Immediate response to user (returns job ID instantly)
   - Background processing doesn't block the UI
   - Multiple workers can process jobs concurrently

3. **Reliability**
   - Automatic retries for failed jobs
   - Job history and monitoring
   - Graceful shutdown handling

4. **Monitoring**
   - Built-in dashboard at `/hangfire`
   - Real-time job progress tracking
   - Historical data and statistics

## Architecture

```
User Request → API Controller → Hangfire Queue → Background Worker
                     ↓                                    ↓
                Return Job ID                    Process Analysis
                     ↓                                    ↓
              SignalR Updates ← ← ← ← ← ← ← ← Progress Updates
```

## Configuration

### Development (appsettings.Development.json)
```json
{
  "Hangfire": {
    "Enabled": true,
    "UseInMemoryStorage": true,
    "WorkerCount": 4
  }
}
```

### Production (Azure App Service)
```json
{
  "Hangfire": {
    "Enabled": true,
    "UseInMemoryStorage": false,
    "WorkerCount": 8
  },
  "ConnectionStrings": {
    "HangfireConnection": "Server=...; Database=HangfireDB; ..."
  }
}
```

## Usage

### Starting an Analysis
```javascript
// Frontend
const response = await api.post('/api/v1/analysis/start', {
  connectionString: '...',
  analysisType: 'comprehensive',
  options: { ... }
});

const jobId = response.data.jobId;
// Job starts immediately in background
```

### Monitoring Progress
```javascript
// Via SignalR (real-time)
connection.on("AnalysisProgress", (status) => {
  console.log(`Progress: ${status.progressPercentage}%`);
  console.log(`Current Step: ${status.currentStep}`);
});

// Via API (polling)
const status = await api.get(`/api/v1/analysis/status/${jobId}`);
```

## Hangfire Dashboard

Access the dashboard at: `https://your-api-url/hangfire`

Features:
- View all running, scheduled, and completed jobs
- Monitor job processing times
- Retry failed jobs manually
- View detailed error messages
- Check server health and worker status

## Comparison: Hangfire vs In-Memory

| Feature | In-Memory (Old) | Hangfire (New) |
|---------|-----------------|----------------|
| Job Persistence | ❌ Lost on restart | ✅ Survives restarts |
| Scalability | ❌ Single server | ✅ Multi-server |
| Monitoring | ❌ Basic | ✅ Full dashboard |
| Retries | ❌ Manual | ✅ Automatic |
| History | ❌ Limited | ✅ Complete history |
| Performance | ✅ Fast | ✅ Fast + reliable |

## Migration Notes

- Existing in-memory analysis service is still available
- Controlled by `Hangfire:Enabled` configuration
- Seamless fallback if Hangfire is disabled
- No changes required to frontend code

## Best Practices

1. **Job Design**
   - Keep jobs idempotent (can be safely retried)
   - Log progress at key milestones
   - Handle cancellation gracefully

2. **Error Handling**
   - Let exceptions bubble up for Hangfire to retry
   - Use structured logging for debugging
   - Send failure notifications via email

3. **Performance**
   - Configure appropriate worker count
   - Use queues for job prioritization
   - Monitor dashboard for bottlenecks

## Future Enhancements

1. **Scheduled Analysis**
   - Daily/weekly automated scans
   - Recurring health checks

2. **Job Chaining**
   - Multi-step analysis workflows
   - Conditional job execution

3. **Advanced Queuing**
   - Priority queues for premium users
   - Resource-based job throttling