using QuickBite.BuildingBlocks.Api;
using QuickBite.BuildingBlocks.Observability;
using QuickBite.Delivery.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.ConfigureQuickBiteObservability("QuickBite.Delivery.Api");
builder.Services.AddDeliveryInfrastructure(builder.Configuration);
builder.Services.AddQuickBiteApiDefaults();
builder.Services.AddHealthChecks()
    .AddQuickBiteDbContextReadiness<DeliveryDbContext>("delivery-db");
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

await app.Services.EnsureDeliveryDatabaseAsync();

app.UseQuickBiteExceptionHandling(app.Environment);
app.UseQuickBiteObservability();
app.UseQuickBiteApiVersionHeader();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();
app.MapHealthChecks("/health", ObservabilityExtensions.QuickBiteHealthCheckOptions());
app.MapHealthChecks("/health/live", ObservabilityExtensions.QuickBiteHealthCheckOptions(_ => false));
app.MapHealthChecks("/health/ready", ObservabilityExtensions.QuickBiteHealthCheckOptions(check => check.Tags.Contains("ready")));

app.Run();
