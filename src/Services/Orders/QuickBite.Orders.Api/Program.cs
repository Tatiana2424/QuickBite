using FluentValidation;
using FluentValidation.AspNetCore;
using QuickBite.BuildingBlocks.Api;
using QuickBite.BuildingBlocks.Observability;
using QuickBite.Orders.Api.Controllers;
using QuickBite.Orders.Application;
using QuickBite.Orders.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.ConfigureQuickBiteObservability("QuickBite.Orders.Api");
builder.Services.AddOrdersInfrastructure(builder.Configuration);
builder.Services.AddQuickBiteApiDefaults();
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddScoped<IValidator<CreateOrderRequest>, CreateOrderRequestValidator>();
builder.Services.AddHealthChecks()
    .AddQuickBiteDbContextReadiness<OrdersDbContext>("orders-db");
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

await app.Services.EnsureOrdersDatabaseAsync();

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
