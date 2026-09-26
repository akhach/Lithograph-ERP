using System.Text.Json;
using System.Text.Json.Serialization;
using LithographERP.Api.Authentication;
using LithographERP.Api.Errors;
using LithographERP.Api.Health;
using LithographERP.Application.Modules.Authentication;
using LithographERP.Domain.Modules.Authentication;
using LithographERP.Infrastructure;
using LithographERP.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

const string DevelopmentCorsPolicy = "DevelopmentFrontend";

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("LithographDb");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "Database connection configuration missing. Set 'ConnectionStrings:LithographDb' (User Secrets or environment variable).");
}

builder.Services.AddInfrastructure(connectionString, builder.Configuration);

builder.Services
    .AddHealthChecks()
    .AddDbContextCheck<LithographDbContext>(HealthCheckNames.Database);

builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower)))
    .ConfigureApiBehaviorOptions(options =>
        options.InvalidModelStateResponseFactory = context =>
        {
            var errors = context.ModelState
                .Where(entry => entry.Value is { Errors.Count: > 0 })
                .ToDictionary(
                    entry => JsonNamingPolicy.CamelCase.ConvertName(entry.Key),
                    entry => entry.Value!.Errors.Select(error => error.ErrorMessage).ToArray());

            return new BadRequestObjectResult(
                new ApiErrorResponse("VALIDATION_FAILED", "One or more fields are invalid.", errors));
        });

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services
    .AddAuthentication(SessionAuthenticationHandler.SchemeName)
    .AddScheme<AuthenticationSchemeOptions, SessionAuthenticationHandler>(SessionAuthenticationHandler.SchemeName, _ => { });

builder.Services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();
builder.Services.AddSingleton<IAuthorizationMiddlewareResultHandler, ApiAuthorizationResultHandler>();
builder.Services.AddAuthorization(options =>
{
    foreach (var permission in PermissionCatalog.All)
    {
        options.AddPolicy(permission.Code, policy =>
            policy.RequireAuthenticatedUser().AddRequirements(new PermissionRequirement(permission.Code)));
    }
});

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
if (allowedOrigins.Length > 0)
{
    builder.Services.AddCors(options =>
        options.AddPolicy(DevelopmentCorsPolicy, policy =>
        {
            policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod().AllowCredentials();
        }));
}

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();
}

var app = builder.Build();

app.UseExceptionHandler(_ => { });

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

if (allowedOrigins.Length > 0)
{
    app.UseCors(DevelopmentCorsPolicy);
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

if (!EF.IsDesignTime)
{
    await using var scope = app.Services.CreateAsyncScope();
    var bootstrap = scope.ServiceProvider.GetRequiredService<IAuthenticationBootstrap>();
    if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Testing"))
    {
        var db = scope.ServiceProvider.GetRequiredService<LithographDbContext>();
        await db.Database.MigrateAsync();
    }

    await bootstrap.SynchronizeAsync();
}

app.Run();
