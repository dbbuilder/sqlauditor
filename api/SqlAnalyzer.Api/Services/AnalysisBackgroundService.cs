using System.Threading.Channels;

namespace SqlAnalyzer.Api.Services
{
    public class AnalysisBackgroundService : BackgroundService
    {
        private readonly ILogger<AnalysisBackgroundService> _logger;
        private readonly Channel<AnalysisJob> _queue;

        public AnalysisBackgroundService(ILogger<AnalysisBackgroundService> logger)
        {
            _logger = logger;
            _queue = Channel.CreateUnbounded<AnalysisJob>();
        }

        public async Task QueueAnalysisAsync(AnalysisJob job)
        {
            await _queue.Writer.WriteAsync(job);
            _logger.LogInformation("Analysis job {JobId} queued", job.Id);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Analysis background service started");

            await foreach (var job in _queue.Reader.ReadAllAsync(stoppingToken))
            {
                try
                {
                    _logger.LogInformation("Processing analysis job {JobId}", job.Id);
                    // Job processing is handled by AnalysisService
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing job {JobId}", job.Id);
                }
            }

            _logger.LogInformation("Analysis background service stopped");
        }

        public class AnalysisJob
        {
            public string Id { get; set; } = string.Empty;
            public string ConnectionString { get; set; } = string.Empty;
            public string AnalysisType { get; set; } = string.Empty;
        }
    }
}