using Hangfire;
using Hangfire.InMemory;
using Hangfire.SqlServer;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using SqlAnalyzer.Api.Extensions;
using SqlAnalyzer.Api.Hubs;
using SqlAnalyzer.Api.Middleware;
using SqlAnalyzer.Api.Models;
using SqlAnalyzer.Api.Services;
using System.Text;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/sqlanalyzer-api-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // Add Serilog
    builder.Host.UseSerilog();

    // Add services
    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new() { Title = "SQL Analyzer API", Version = "v1" });

        // Add JWT authentication to Swagger
        c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token",
            Name = "Authorization",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.ApiKey,
            Scheme = "Bearer"
        });

        c.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                },
                Array.Empty<string>()
            }
        });
    });

    // Add CORS for Vue.js frontend
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("VueApp", policy =>
        {
            var allowedOrigins = new List<string>
            {
                "http://localhost:5173",  // Vite default
                "http://localhost:3000",  // Alternative
                "http://localhost:8080"   // Vue CLI default
            };

            // Add production origins from Cors:AllowedOrigins
            var corsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();
            if (corsOrigins != null)
            {
                allowedOrigins.AddRange(corsOrigins);
            }

            // Add Azure Static Web App origins
            allowedOrigins.Add("https://sqlanalyzer-web.azurestaticapps.net");
            allowedOrigins.Add("https://black-desert-02d93d30f.2.azurestaticapps.net");
            // Note: Wildcard origins don't work with credentials

            policy.WithOrigins(allowedOrigins.ToArray())
                .AllowAnyMethod()
                .AllowAnyHeader()
                .AllowCredentials()
                .WithExposedHeaders("*");
        });
    });

    // Add authentication
    var jwtKey = builder.Configuration["Jwt:Key"];
    var jwtIssuer = builder.Configuration["Jwt:Issuer"];
    var jwtAudience = builder.Configuration["Jwt:Audience"];

    // Use JWT key if available, otherwise fall back to Authentication:JwtSecret
    if (string.IsNullOrEmpty(jwtKey))
    {
        var authSettings = builder.Configuration.GetSection("Authentication").Get<AuthSettings>() ?? new AuthSettings();
        jwtKey = authSettings.JwtSecret;

        if (string.IsNullOrEmpty(jwtKey))
        {
            jwtKey = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(64));
            Log.Warning("No JWT secret configured. Using generated secret. Configure 'Jwt:Key' or 'Authentication:JwtSecret' for production.");
        }
    }

    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(jwtKey)),
            ValidateIssuer = !string.IsNullOrEmpty(jwtIssuer),
            ValidIssuer = jwtIssuer,
            ValidateAudience = !string.IsNullOrEmpty(jwtAudience),
            ValidAudience = jwtAudience,
            ClockSkew = TimeSpan.Zero
        };

        // Support SignalR authentication
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;

                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                {
                    context.Token = accessToken;
                }

                return Task.CompletedTask;
            }
        };
    });

    builder.Services.AddAuthorization();

    // Add JWT service
    builder.Services.AddScoped<IJwtService, JwtService>();

    // Add Email service
    builder.Services.AddScoped<IEmailService, EmailService>();

    // Configure Hangfire
    var hangfireConnectionString = builder.Configuration.GetConnectionString("HangfireConnection")
        ?? builder.Configuration.GetConnectionString("DefaultConnection");

    if (builder.Environment.IsDevelopment() || string.IsNullOrEmpty(hangfireConnectionString))
    {
        // Use in-memory storage for development
        builder.Services.AddHangfire(config => config
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UseInMemoryStorage());
    }
    else
    {
        // Use SQL Server for production
        builder.Services.AddHangfire(config => config
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UseSqlServerStorage(hangfireConnectionString, new SqlServerStorageOptions
            {
                CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
                SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
                QueuePollInterval = TimeSpan.Zero,
                UseRecommendedIsolationLevel = true,
                DisableGlobalLocks = true
            }));
    }

    // Add Hangfire server
    builder.Services.AddHangfireServer(options =>
    {
        options.WorkerCount = Environment.ProcessorCount * 2;
        options.Queues = new[] { "analysis", "default" };
    });

    // Register Hangfire analysis service
    builder.Services.AddScoped<IHangfireAnalysisService, HangfireAnalysisService>();

    // Add SignalR for real-time updates
    var signalRSettings = builder.Configuration.GetSection("SignalR").Get<SignalRSettings>() ?? new SignalRSettings();
    builder.Services.AddSignalR(options =>
    {
        options.EnableDetailedErrors = signalRSettings.EnableDetailedErrors;
        options.KeepAliveInterval = TimeSpan.FromSeconds(signalRSettings.KeepAliveInterval);
        options.ClientTimeoutInterval = TimeSpan.FromSeconds(signalRSettings.ClientTimeoutInterval);
    });

    // Add response compression
    builder.Services.AddResponseCompression();

    // Add API versioning (disabled for now due to issues)
    // builder.Services.AddApiVersioning();

    // Add health checks
    builder.Services.AddHealthChecks()
        .AddCheck<DatabaseHealthCheck>("database");

    // Configure SQL Analyzer services
    builder.Services.ConfigureSqlAnalyzer(builder.Configuration);

    // Add background services
    builder.Services.AddHostedService<AnalysisBackgroundService>();

    // Add memory cache for results
    builder.Services.AddMemoryCache();

    // Add distributed cache (Redis) for scaling
    if (builder.Configuration.GetValue<bool>("Redis:Enabled"))
    {
        builder.Services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = builder.Configuration.GetConnectionString("Redis");
        });
    }

    var app = builder.Build();

    // Log startup information
    var assembly = System.Reflection.Assembly.GetExecutingAssembly();
    var version = assembly.GetName().Version?.ToString() ?? "1.0.0.0";
    var buildDate = File.GetLastWriteTimeUtc(assembly.Location).ToString("yyyy-MM-dd HH:mm:ss UTC");

    app.Logger.LogInformation("==============================================");
    app.Logger.LogInformation("SQL Analyzer API Starting");
    app.Logger.LogInformation("Version: {Version}", version);
    app.Logger.LogInformation("Build Date: {BuildDate}", buildDate);
    app.Logger.LogInformation("Environment: {Environment}", app.Environment.EnvironmentName);
    app.Logger.LogInformation("Commit SHA: {CommitSha}", Environment.GetEnvironmentVariable("GITHUB_SHA")?.Substring(0, 7) ?? "local");
    app.Logger.LogInformation("==============================================");

    // Configure pipeline
    app.UseSerilogRequestLogging();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseHttpsRedirection();
    app.UseCors("VueApp");
    app.UseResponseCompression();

    // Custom middleware
    app.UseMiddleware<ErrorHandlingMiddleware>();
    app.UseMiddleware<RequestLoggingMiddleware>();

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();

    // Map SignalR hub only if enabled
    if (signalRSettings.Enabled)
    {
        app.MapHub<AnalysisHub>("/hubs/analysis").RequireCors("VueApp");
    }

    app.MapHealthChecks("/health");

    // Add Hangfire Dashboard (protected by authentication)
    app.UseHangfireDashboard("/hangfire", new DashboardOptions
    {
        Authorization = new[] { new HangfireAuthorizationFilter() },
        DisplayStorageConnectionString = false,
        DashboardTitle = "SQL Analyzer Background Jobs"
    });

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}