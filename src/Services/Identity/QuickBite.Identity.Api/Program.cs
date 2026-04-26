using System.Text;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using QuickBite.BuildingBlocks.Api;
using QuickBite.BuildingBlocks.Common;
using QuickBite.BuildingBlocks.Observability;
using QuickBite.Identity.Api.Controllers;
using QuickBite.Identity.Application;
using QuickBite.Identity.Domain;
using QuickBite.Identity.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.ConfigureQuickBiteObservability("QuickBite.Identity.Api");
builder.Services.AddIdentityInfrastructure(builder.Configuration);
builder.Services.AddQuickBiteApiDefaults();
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddScoped<IValidator<RegisterRequest>, RegisterRequestValidator>();
builder.Services.AddScoped<IValidator<LoginRequest>, LoginRequestValidator>();
builder.Services.AddScoped<IValidator<RefreshTokenRequest>, RefreshTokenRequestValidator>();
builder.Services.AddScoped<IValidator<RevokeRefreshTokenRequest>, RevokeRefreshTokenRequestValidator>();
builder.Services.AddHealthChecks();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var jwt = new JwtOptions
{
    Issuer = ConfigurationGuard.GetRequiredValue(builder.Configuration, "Jwt:Issuer"),
    Audience = ConfigurationGuard.GetRequiredValue(builder.Configuration, "Jwt:Audience"),
    Key = ConfigurationGuard.GetRequiredValue(builder.Configuration, "Jwt:Key")
};

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ValidIssuer = jwt.Issuer,
            ValidAudience = jwt.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key))
        };
    });
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Customers", policy => policy.RequireRole(IdentityRoles.Customer, IdentityRoles.PlatformAdmin));
    options.AddPolicy("RestaurantAdmins", policy => policy.RequireRole(IdentityRoles.RestaurantAdmin, IdentityRoles.PlatformAdmin));
    options.AddPolicy("Couriers", policy => policy.RequireRole(IdentityRoles.Courier, IdentityRoles.PlatformAdmin));
    options.AddPolicy("PlatformAdmins", policy => policy.RequireRole(IdentityRoles.PlatformAdmin));
});

var app = builder.Build();

await app.Services.EnsureIdentityDatabaseAsync();

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
app.MapHealthChecks("/health");
app.MapHealthChecks("/health/live");
app.MapHealthChecks("/health/ready");

app.Run();
