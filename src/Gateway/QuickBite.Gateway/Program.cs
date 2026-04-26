using QuickBite.BuildingBlocks.Api;
using QuickBite.BuildingBlocks.Observability;

var builder = WebApplication.CreateBuilder(args);

builder.ConfigureQuickBiteObservability("QuickBite.Gateway");
ReverseProxyConfigurationValidator.Validate(builder.Configuration);
builder.Services.AddHealthChecks();
builder.Services.AddQuickBiteGatewaySecurity(builder.Configuration);
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();

app.UseQuickBiteExceptionHandling(app.Environment);
app.UseQuickBiteObservability();
app.UseQuickBiteApiVersionHeader();
app.UseQuickBiteGatewaySecurity();
app.MapHealthChecks("/health");
app.MapHealthChecks("/health/live");
app.MapHealthChecks("/health/ready");
app.MapGet("/", () => Results.Redirect("/health"));
app.MapReverseProxy();

app.Run();
