using Microsoft.AspNetCore.SignalR;
using SqlAnalyzer.Api.Models;

namespace SqlAnalyzer.Api.Hubs
{
    public class AnalysisHub : Hub
    {
        private readonly ILogger<AnalysisHub> _logger;

        public AnalysisHub(ILogger<AnalysisHub> logger)
        {
            _logger = logger;
        }

        public override async Task OnConnectedAsync()
        {
            _logger.LogInformation("Client connected: {ConnectionId}", Context.ConnectionId);
            await Clients.Caller.SendAsync("Connected", Context.ConnectionId);
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            _logger.LogInformation("Client disconnected: {ConnectionId}", Context.ConnectionId);
            await base.OnDisconnectedAsync(exception);
        }

        public async Task SubscribeToJob(string jobId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"job-{jobId}");
            _logger.LogInformation("Client {ConnectionId} subscribed to job {JobId}", 
                Context.ConnectionId, jobId);
        }

        public async Task UnsubscribeFromJob(string jobId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"job-{jobId}");
            _logger.LogInformation("Client {ConnectionId} unsubscribed from job {JobId}", 
                Context.ConnectionId, jobId);
        }
    }
}