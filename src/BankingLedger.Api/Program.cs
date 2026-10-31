using System.Reflection;
using System.Text.Json.Serialization;
using BankingLedger.Api.Middleware;
using BankingLedger.Application;
using BankingLedger.Infrastructure;
using BankingLedger.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

// 1. Configure Web API & JSON Serialization
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Serialize enums as strings (e.g. "credit" / "debit") for readable client responses
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });

// 2. Configure Swagger / OpenAPI Documentation
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Banking Ledger API",
        Version = "v1",
        Description = "Domain-Driven Design (DDD) Banking Ledger API for managing customer accounts, immutable transactions, and atomic money transfers.",
        Contact = new OpenApiContact
        {
            Name = "Engineering Team",
            Email = "engineering@bankingledger.local"
        }
    });

    // Include XML comments for swagger documentation
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }
});

// 3. Register Application & Infrastructure Layers (Clean Architecture / DDD)
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

// 4. Exception Handling Middleware (RFC 7807 Problem Details)
app.UseMiddleware<ExceptionHandlingMiddleware>();

// 5. Enable Swagger in Development & Local Container Environments
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Banking Ledger API v1");
    c.RoutePrefix = string.Empty; // Serve Swagger UI directly at the application root URL (http://localhost:5000)
});

app.UseAuthorization();
app.MapControllers();

// Health check endpoint for container orchestrators and monitoring
app.MapGet("/health", () => Results.Ok(new { status = "Healthy", timestamp = DateTimeOffset.UtcNow }));

// 6. Automatic Database Migration Helper on Startup
// Why this helps developers and PMs:
// When running locally with 'docker compose up -d', this automatically verifies the connection
// and applies pending EF Core migrations without manual intervention!
using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    var dbContext = scope.ServiceProvider.GetRequiredService<BankingDbContext>();

    try
    {
        logger.LogInformation("Verifying PostgreSQL database connection and checking migrations...");
        
        // Wait and retry briefly if PostgreSQL container is still completing initialization
        var canConnect = false;
        for (var attempt = 1; attempt <= 5; attempt++)
        {
            if (await dbContext.Database.CanConnectAsync())
            {
                canConnect = true;
                break;
            }
            logger.LogInformation("Database not ready yet. Waiting 2 seconds (attempt {Attempt}/5)...", attempt);
            await Task.Delay(2000);
        }

        if (canConnect)
        {
            logger.LogInformation("PostgreSQL connected! Applying code-first migrations...");
            await dbContext.Database.MigrateAsync();
            logger.LogInformation("Database migrations applied successfully. Ledger is ready to accept requests.");
        }
        else
        {
            logger.LogWarning("PostgreSQL is not reachable at the configured connection string. Run 'docker compose up -d' to start PostgreSQL locally.");
        }
    }
    catch (Exception ex)
    {
        logger.LogWarning(ex, "Database migration check skipped or failed on startup. Ensure PostgreSQL is running via 'docker compose up -d'.");
    }
}

app.Run();
