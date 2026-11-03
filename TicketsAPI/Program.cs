using Azure.Monitor.OpenTelemetry.AspNetCore;
using Microsoft.EntityFrameworkCore;
using OpenTelemetry;
using TicketsAPI.Common;
using TicketsAPI.Data;
using TicketsAPI.Repository;
using TicketsAPI.Services;

var builder = WebApplication.CreateBuilder(args);

// En Azure, ConnectionStrings__TicketsDb llega como App Setting con referencia a Key Vault.
var connectionString = builder.Configuration.GetConnectionString("TicketsDb")
    ?? throw new InvalidOperationException("Falta la cadena de conexión ConnectionStrings:TicketsDb.");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure()));

builder.Services.AddScoped<ITicketRepository, TicketRepository>();
builder.Services.AddScoped<ITicketService, TicketService>();

builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

// CORS: solo se aceptan los orígenes de Cors:AllowedOrigins (el frontend), nunca cualquiera.
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options => options.AddPolicy("Frontend", policy =>
    policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod()));

builder.Services.AddHealthChecks();

// Telemetría solo si hay cadena de Application Insights (App Service la inyecta como App Setting).
if (!string.IsNullOrWhiteSpace(builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"]))
{
    builder.Services.AddOpenTelemetry().UseAzureMonitor();
}

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

app.UseExceptionHandler();
app.UseHttpsRedirection();
app.UseCors("Frontend");

// OpenAPI se publica en todos los entornos para que sea accesible desde Azure.
app.MapOpenApi();
app.MapControllers();
app.MapHealthChecks("/health");

app.Run();

// Permite que WebApplicationFactory<Program> levante la API en las pruebas de integración.
public partial class Program { }
