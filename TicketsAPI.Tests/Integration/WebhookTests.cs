using System.Net;
using System.Net.Http.Json;
using Microsoft.Data.SqlClient;
using TicketsAPI.Dtos;

namespace TicketsAPI.Tests.Integration;

[Collection("Api")]
public class WebhookTests(ApiFixture factory) : IAsyncLifetime
{
    private readonly HttpClient _client = factory.CreateClient();

    public async Task InitializeAsync() => await factory.VaciarTicketsAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Webhook_MensajeNuevo_Devuelve201YCreaTicketDeWhatsApp()
    {
        var response = await EnviarAsync("wamid.TEST-NUEVO");

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var cuerpo = await response.Content.ReadFromJsonAsync<WebhookResponse>();
        Assert.Equal("creado", cuerpo!.Estado);

        var ticket = await _client.GetFromJsonAsync<TicketResponse>($"/api/tickets/{cuerpo.TicketId}");
        Assert.Equal("whatsapp", ticket!.Canal);
        Assert.Equal("wamid.TEST-NUEVO", ticket.MensajeExternoId);
    }

    [Fact]
    public async Task Webhook_MensajeDuplicado_Devuelve200YNoCreaUnSegundoTicket()
    {
        var primera = await (await EnviarAsync("wamid.TEST-DUP")).Content.ReadFromJsonAsync<WebhookResponse>();

        var segunda = await EnviarAsync("wamid.TEST-DUP");

        Assert.Equal(HttpStatusCode.OK, segunda.StatusCode);
        var cuerpo = await segunda.Content.ReadFromJsonAsync<WebhookResponse>();
        Assert.Equal("ya_procesado", cuerpo!.Estado);
        Assert.Equal(primera!.TicketId, cuerpo.TicketId);
        Assert.Equal(1, await ApiFixture.ContarPorMensajeExternoIdAsync("wamid.TEST-DUP"));
    }

    [Fact]
    public async Task Webhook_EntregasSimultaneasDelMismoMensaje_CreanUnSoloTicket()
    {
        var respuestas = await Task.WhenAll(Enumerable.Range(0, 6).Select(_ => EnviarAsync("wamid.TEST-CARRERA")));

        Assert.All(respuestas, r => Assert.True(r.StatusCode is HttpStatusCode.Created or HttpStatusCode.OK));
        Assert.Equal(1, respuestas.Count(r => r.StatusCode == HttpStatusCode.Created));
        Assert.Equal(1, await ApiFixture.ContarPorMensajeExternoIdAsync("wamid.TEST-CARRERA"));
    }

    [Fact]
    public async Task Webhook_PayloadInvalido_Devuelve400()
    {
        var payload = new
        {
            evento = "otro.evento",
            mensajeId = "",
            remitente = new { telefono = "" },
            texto = "",
            timestamp = DateTime.UtcNow,
        };

        var response = await _client.PostAsJsonAsync("/api/webhooks/mensajes", payload);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task BaseDeDatos_RechazaUnSegundoTicketConElMismoMensajeExternoId()
    {
        const string insertar = "INSERT INTO dbo.Tickets (Titulo, Canal, MensajeExternoId) VALUES ('Duplicado', 'whatsapp', 'wamid.TEST-INDICE')";
        await ApiFixture.EjecutarAsync(insertar);

        var ex = await Assert.ThrowsAsync<SqlException>(() => ApiFixture.EjecutarAsync(insertar));

        // 2601 = índice único violado, 2627 = restricción única violada.
        Assert.Contains(ex.Number, new[] { 2601, 2627 });
    }

    private Task<HttpResponseMessage> EnviarAsync(string mensajeId) =>
        _client.PostAsJsonAsync("/api/webhooks/mensajes", new
        {
            evento = "mensaje.recibido",
            mensajeId,
            remitente = new { telefono = "+51987654321", nombre = "Maria Perez" },
            texto = "Hola, no puedo ingresar a mi cuenta",
            timestamp = "2026-10-22T14:30:00Z",
        });
}
