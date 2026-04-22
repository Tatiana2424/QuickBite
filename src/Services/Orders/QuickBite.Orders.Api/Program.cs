using FluentValidation;
using FluentValidation.AspNetCore;
using QuickBite.BuildingBlocks.Observability;
using QuickBite.Orders.Api.Controllers;
using QuickBite.Orders.Application;
using QuickBite.Orders.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.ConfigureQuickBiteObservability("QuickBite.Orders.Api");
builder.Services.AddOrdersInfrastructure(builder.Configuration);
builder.Services.AddControllers();
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddScoped<IValidator<CreateOrderRequest>, CreateOrderRequestValidator>();
builder.Services.AddHealthChecks();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

await app.Services.EnsureOrdersDatabaseAsync();

app.UseQuickBiteObservability();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();
app.MapHealthChecks("/health");
app.MapHealthChecks("/health/live");
app.MapHealthChecks("/health/ready");

app.Run();
