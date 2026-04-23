using QuickBite.BuildingBlocks.Observability;

var builder = WebApplication.CreateBuilder(args);

builder.ConfigureQuickBiteObservability("QuickBite.Gateway");
builder.Services.AddHealthChecks();
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();

app.UseQuickBiteObservability();
app.MapHealthChecks("/health");
app.MapHealthChecks("/health/live");
app.MapHealthChecks("/health/ready");
app.MapGet("/", () => Results.Redirect("/health"));
app.MapReverseProxy();

app.Run();
