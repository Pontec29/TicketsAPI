using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;

namespace TicketsAPI.Tests.Integration;

// Levanta la API completa contra SQL Server real. Usa TICKETS_TEST_CONNECTION_STRING si existe,
// o la base local TicketsFrefity_Tests por defecto. La base se crea y el esquema se aplica desde /db.
public class ApiFixture : WebApplicationFactory<Program>
{
    public const string OrigenPermitido = "http://localhost:5173";

    public static readonly string ConnectionString =
        Environment.GetEnvironmentVariable("TICKETS_TEST_CONNECTION_STRING")
        ?? "Server=localhost;Database=TicketsFrefity_Tests;Trusted_Connection=True;TrustServerCertificate=True";

    public ApiFixture()
    {
        CrearBaseSiNoExisteAsync().GetAwaiter().GetResult();
        EjecutarScriptAsync("01_schema.sql").GetAwaiter().GetResult();
        EjecutarScriptAsync("02_seed.sql").GetAwaiter().GetResult();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("ConnectionStrings:TicketsDb", ConnectionString);
        builder.UseSetting("Cors:AllowedOrigins:0", OrigenPermitido);
    }

    public Task VaciarTicketsAsync() => EjecutarAsync("DELETE FROM dbo.Tickets;");

    public static async Task EjecutarAsync(string sql)
    {
        await using var conn = new SqlConnection(ConnectionString);
        await conn.OpenAsync();
        await using var cmd = new SqlCommand(sql, conn);
        await cmd.ExecuteNonQueryAsync();
    }

    public static async Task<int> ContarPorMensajeExternoIdAsync(string mensajeExternoId)
    {
        await using var conn = new SqlConnection(ConnectionString);
        await conn.OpenAsync();
        await using var cmd = new SqlCommand("SELECT COUNT(*) FROM dbo.Tickets WHERE MensajeExternoId = @id", conn);
        cmd.Parameters.AddWithValue("@id", mensajeExternoId);
        return (int)(await cmd.ExecuteScalarAsync())!;
    }

    private static async Task CrearBaseSiNoExisteAsync()
    {
        var baseDeDatos = new SqlConnectionStringBuilder(ConnectionString).InitialCatalog;
        var master = new SqlConnectionStringBuilder(ConnectionString) { InitialCatalog = "master" };

        await using var conn = new SqlConnection(master.ConnectionString);
        await conn.OpenAsync();
        await using var cmd = new SqlCommand($"IF DB_ID(N'{baseDeDatos}') IS NULL CREATE DATABASE [{baseDeDatos}];", conn);
        await cmd.ExecuteNonQueryAsync();
    }

    // Ejecuta un script de /db separando por líneas "GO", como sqlcmd.
    private static async Task EjecutarScriptAsync(string nombre)
    {
        var script = await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "Scripts", nombre));
        var lotes = Regex.Split(script, @"^\s*GO\s*$", RegexOptions.Multiline);

        await using var conn = new SqlConnection(ConnectionString);
        await conn.OpenAsync();
        foreach (var lote in lotes.Where(l => !string.IsNullOrWhiteSpace(l)))
        {
            await using var cmd = new SqlCommand(lote, conn);
            await cmd.ExecuteNonQueryAsync();
        }
    }
}

[CollectionDefinition("Api")]
public class ApiCollection : ICollectionFixture<ApiFixture>;
