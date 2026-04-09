using QuickBite.BuildingBlocks.Observability;

var builder = WebApplication.CreateBuilder(args);

builder.ConfigureQuickBiteObservability("QuickBite.Gateway");
builder.Services.AddHealthChecks();
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();

app.UseQuickBiteObservability();
app.MapHealthChecks("/health");
app.MapGet("/", () => Results.Redirect("/health"));
app.MapReverseProxy();

app.Run();
