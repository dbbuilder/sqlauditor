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

            // Add production origins
            var corsOrigins = builder.Configuration.GetSection("SqlAnalyzer:AllowedOrigins").Get<string[]>();
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
    var authSettings = builder.Configuration.GetSection("Authentication").Get<AuthSettings>() ?? new AuthSettings();
    
    // Generate a default JWT secret if not configured
    if (string.IsNullOrEmpty(authSettings.JwtSecret))
    {
        authSettings.JwtSecret = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(64));
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
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(authSettings.JwtSecret)),
            ValidateIssuer = false,
            ValidateAudience = false,
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

    // Add SignalR for real-time updates
    builder.Services.AddSignalR();

    // Add response compression
    builder.Services.AddResponseCompression();

    // Add API versioning
    builder.Services.AddApiVersioning();

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
    app.MapHub<AnalysisHub>("/hubs/analysis").RequireCors("VueApp");
    app.MapHealthChecks("/health");

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