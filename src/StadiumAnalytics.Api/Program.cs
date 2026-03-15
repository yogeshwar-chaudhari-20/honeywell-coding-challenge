using Microsoft.EntityFrameworkCore;
using StadiumAnalytics.Core.Events;
using StadiumAnalytics.Core.Services;
using StadiumAnalytics.Infrastructure.Data;
using StadiumAnalytics.Infrastructure.Events;
using StadiumAnalytics.Infrastructure.Services;
using StadiumAnalytics.Infrastructure.Simulation;

var builder = WebApplication.CreateBuilder(args);

// Database
builder.Services.AddDbContext<StadiumDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("StadiumDb") ?? "Data Source=stadium.db"));

// Event channel (singleton -- shared between producer and consumer)
builder.Services.AddSingleton<GateEventChannel>();
builder.Services.AddSingleton<IGateEventChannel>(sp => sp.GetRequiredService<GateEventChannel>());

// Background services
builder.Services.AddHostedService<EventConsumerService>();
builder.Services.AddHostedService<EventSimulatorService>();

// Simulation options
builder.Services.AddOptions<EventSimulationOptions>()
    .BindConfiguration(EventSimulationOptions.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart();

// Seeder
builder.Services.AddTransient<DatabaseSeeder>();

// Application services
builder.Services.AddScoped<IAnalyticsQueryService, AnalyticsQueryService>();
builder.Services.AddScoped<IEventIngestionService, EventIngestionService>();

// Controllers
builder.Services.AddControllers();

// Problem Details
builder.Services.AddProblemDetails();

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Stadium Analytics API",
        Version = "v1",
        Description = "Event-driven API for stadium gate people-flow analytics"
    });
});

// Health checks
builder.Services.AddHealthChecks()
    .AddDbContextCheck<StadiumDbContext>("database", tags: new[] { "ready" });

var app = builder.Build();

// Apply migrations and seed on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<StadiumDbContext>();

    if (db.Database.IsSqlite() && db.Database.GetConnectionString()?.Contains(":memory:", StringComparison.OrdinalIgnoreCase) == true)
        await db.Database.EnsureCreatedAsync();
    else
        await db.Database.MigrateAsync();

    var seeder = scope.ServiceProvider.GetRequiredService<DatabaseSeeder>();
    await seeder.SeedAsync();
}

// Middleware pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Stadium Analytics API v1");
    });
}

app.UseExceptionHandler();
app.UseStatusCodePages();

app.UseHttpsRedirection();

app.MapControllers();

// Health check endpoints
app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = _ => false // no checks, just confirms the process is alive
});

app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});

app.Run();

public partial class Program { }
