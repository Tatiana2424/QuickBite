using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using QuickBite.BuildingBlocks.Api;
using QuickBite.BuildingBlocks.Common;
using QuickBite.BuildingBlocks.Observability;
using QuickBite.Catalog.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.ConfigureQuickBiteObservability("QuickBite.Catalog.Api");
builder.Services.AddCatalogInfrastructure(builder.Configuration);
builder.Services.AddQuickBiteApiDefaults();
builder.Services.AddHealthChecks()
    .AddQuickBiteDbContextReadiness<CatalogDbContext>("catalog-db");
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var jwtIssuer = ConfigurationGuard.GetRequiredValue(builder.Configuration, "Jwt:Issuer");
var jwtAudience = ConfigurationGuard.GetRequiredValue(builder.Configuration, "Jwt:Audience");
var jwtKey = ConfigurationGuard.GetRequiredValue(builder.Configuration, "Jwt:Key");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });
builder.Services.AddAuthorization();

var app = builder.Build();

await app.Services.EnsureCatalogDatabaseAsync();

app.UseQuickBiteExceptionHandling(app.Environment);
app.UseQuickBiteObservability();
app.UseQuickBiteApiVersionHeader();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health", ObservabilityExtensions.QuickBiteHealthCheckOptions());
app.MapHealthChecks("/health/live", ObservabilityExtensions.QuickBiteHealthCheckOptions(_ => false));
app.MapHealthChecks("/health/ready", ObservabilityExtensions.QuickBiteHealthCheckOptions(check => check.Tags.Contains("ready")));

app.Run();
