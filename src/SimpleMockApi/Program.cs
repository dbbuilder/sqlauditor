var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure pipeline
app.UseSwagger();
app.UseSwaggerUI();
app.UseCors("AllowAll");
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

// Health check endpoint
app.MapGet("/health", () => new { status = "healthy", timestamp = DateTime.UtcNow });

// Mock API endpoints
app.MapGet("/api/v1/analysis/types", () => new[]
{
    new { id = "quick", name = "Quick Analysis", description = "Basic health check" },
    new { id = "performance", name = "Performance Analysis", description = "Index and query analysis" },
    new { id = "security", name = "Security Audit", description = "Permission and vulnerability check" },
    new { id = "comprehensive", name = "Comprehensive", description = "Full database analysis" }
});

app.MapPost("/api/v1/analysis/test-connection", (ConnectionTestRequest request) => 
    new { success = true, databaseName = "TestDB", serverVersion = "SQL Server 2019" });

app.MapPost("/api/v1/analysis/start", (AnalysisRequest request) => 
    new { jobId = Guid.NewGuid().ToString(), status = "Started", message = "Analysis started" });

app.MapGet("/api/v1/analysis/status/{jobId}", (string jobId) => 
    new { jobId, status = "Completed", progressPercentage = 100, currentStep = "Complete" });

app.Run();

// Request DTOs
record ConnectionTestRequest(string ConnectionString, string DatabaseType);
record AnalysisRequest(string ConnectionString, string DatabaseType, string AnalysisType);