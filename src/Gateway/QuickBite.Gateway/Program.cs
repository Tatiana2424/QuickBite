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
app.MapHealthChecks("/health", ObservabilityExtensions.QuickBiteHealthCheckOptions());
app.MapHealthChecks("/health/live", ObservabilityExtensions.QuickBiteHealthCheckOptions(_ => false));
app.MapHealthChecks("/health/ready", ObservabilityExtensions.QuickBiteHealthCheckOptions());
app.MapGet("/", () => Results.Redirect("/health"));
app.MapReverseProxy();

app.Run();
