using Serilog;
using PostalIdempotencyDemo.Api.Data;
using PostalIdempotencyDemo.Api.Repositories;
using PostalIdempotencyDemo.Api.Services;
using PostalIdempotencyDemo.Api.Services.Interfaces;
using PostalIdempotencyDemo.Api.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register database and repositories
builder.Services.AddScoped<IDbConnectionFactory, SqlServerConnectionFactory>();
builder.Services.AddScoped<IShipmentRepository, ShipmentRepository>();
builder.Services.AddScoped<IIdempotencyRepository, IdempotencyRepository>();
builder.Services.AddScoped<IDeliveryRepository, DeliveryRepository>();
builder.Services.AddScoped<IMetricsRepository, MetricsRepository>();
builder.Services.AddScoped<ISettingsRepository, SettingsRepository>();
builder.Services.AddScoped<IDataCleanupRepository, DataCleanupRepository>();
builder.Services.AddScoped<ISqlExecutor, SqlExecutor>();

// Register services
builder.Services.AddScoped<IShipmentService, ShipmentService>();
builder.Services.AddScoped<IIdempotencyService, IdempotencyService>();
builder.Services.AddScoped<IMetricsService, MetricsService>();
builder.Services.AddScoped<IIdempotencyOrchestrationService, IdempotencyOrchestrationService>();
builder.Services.AddScoped<IDeliveryService, DeliveryService>();
builder.Services.AddScoped<IChaosService, ChaosService>();
builder.Services.AddScoped<IRetryService, RetryService>();
builder.Services.AddScoped<IDataCleanupService, DataCleanupService>();

// Register HttpClient and HttpClientService
builder.Services.AddHttpClient<IHttpClientService, HttpClientService>();

// CORS configuration
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularApp", policy =>
    {
        policy.WithOrigins("http://localhost:4200", "http://localhost:4201")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
app.UseMiddleware<PostalIdempotencyDemo.Api.Middleware.GlobalExceptionMiddleware>();
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<MaintenanceModeMiddleware>();
app.UseMiddleware<ResponseTimeMiddleware>();
app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();
app.UseCors("AllowAngularApp");
app.UseAuthorization();
app.MapControllers();

app.Run();
