using QuickBite.BuildingBlocks.Observability;
using QuickBite.Payments.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.ConfigureQuickBiteObservability("QuickBite.Payments.Api");
builder.Services.AddPaymentsInfrastructure(builder.Configuration);
builder.Services.AddControllers();
builder.Services.AddHealthChecks();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

await app.Services.EnsurePaymentsDatabaseAsync();

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
