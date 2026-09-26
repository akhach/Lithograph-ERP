using System.Text.Json;
using System.Text.Json.Serialization;
using LithographERP.Api.Errors;
using LithographERP.Api.Health;
using LithographERP.Infrastructure;
using LithographERP.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;

const string DevelopmentCorsPolicy = "DevelopmentFrontend";

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("LithographDb");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "Database connection configuration missing. Set 'ConnectionStrings:LithographDb' (User Secrets or environment variable).");
}

builder.Services.AddInfrastructure(connectionString);

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

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
if (allowedOrigins.Length > 0)
{
    builder.Services.AddCors(options =>
        options.AddPolicy(DevelopmentCorsPolicy, policy =>
            policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod()));
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

app.MapControllers();

app.Run();
