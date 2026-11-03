using Azure.Identity;
using Azure.Monitor.OpenTelemetry.AspNetCore;
using Microsoft.EntityFrameworkCore;
using OpenTelemetry;
using TicketsAPI.Common;
using TicketsAPI.Data;
using TicketsAPI.Repository;
using TicketsAPI.Services;

var builder = WebApplication.CreateBuilder(args);

// En Development la cadena sale de appsettings.Development.json (arranque sin fricción).
// Fuera de Development, si nadie la inyectó, se lee de Key Vault: el secreto
// "ConnectionStrings--DefaultConnection" se convierte en ConnectionStrings:DefaultConnection
// y DefaultAzureCredential usa la Managed Identity del App Service.
var keyVaultUri = builder.Configuration["KeyVault:Uri"];
if (!builder.Environment.IsDevelopment()
    && string.IsNullOrWhiteSpace(builder.Configuration.GetConnectionString("DefaultConnection"))
    && !string.IsNullOrWhiteSpace(keyVaultUri))
{
    builder.Configuration.AddAzureKeyVault(new Uri(keyVaultUri), new DefaultAzureCredential());
}

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "No se pudo obtener ConnectionStrings:DefaultConnection. Configura KeyVault:Uri con la URI del vault.");

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

// OpenAPI y Swagger UI se publican en todos los entornos para que sean accesibles desde Azure.
app.MapOpenApi();
app.UseSwaggerUI(options => options.SwaggerEndpoint("/openapi/v1.json", "TicketsAPI v1"));
app.MapControllers();
app.MapHealthChecks("/health");

app.Run();

// Permite que WebApplicationFactory<Program> levante la API en las pruebas de integración.
public partial class Program { }
