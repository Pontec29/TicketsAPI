using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using TicketsAPI.Dtos;

namespace TicketsAPI.Tests.Integration;

[Collection("Api")]
public class TicketsEndpointsTests(ApiFixture factory) : IAsyncLifetime
{
    private readonly HttpClient _client = factory.CreateClient();

    public async Task InitializeAsync() => await factory.VaciarTicketsAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Create_DatosValidos_Devuelve201ConLocation()
    {
        var response = await _client.PostAsJsonAsync("/api/tickets",
            new CreateTicketRequest { Titulo = "Problema de login", Descripcion = "No puedo entrar", Canal = "web" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);

        var ticket = await response.Content.ReadFromJsonAsync<TicketResponse>();
        Assert.NotEqual(Guid.Empty, ticket!.Id);
        Assert.Equal("abierto", ticket.Estado);
        Assert.Equal("media", ticket.Prioridad);

        var consulta = await _client.GetAsync(response.Headers.Location);
        Assert.Equal(HttpStatusCode.OK, consulta.StatusCode);
    }

    [Fact]
    public async Task Create_DatosInvalidos_Devuelve400ConErroresPorCampo()
    {
        var response = await _client.PostAsJsonAsync("/api/tickets", new { titulo = "abc", canal = "fax" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problema = await response.Content.ReadFromJsonAsync<JsonElement>();
        var errores = problema.GetProperty("errors");
        Assert.True(errores.TryGetProperty("Titulo", out _));
        Assert.True(errores.TryGetProperty("Canal", out _));
    }

    [Fact]
    public async Task GetById_Inexistente_Devuelve404ConProblemDetails()
    {
        var response = await _client.GetAsync($"/api/tickets/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task List_FiltraPorEstadoYPagina_DevuelveElTotalReal()
    {
        for (var i = 0; i < 3; i++) await CrearAsync($"Ticket cerrado {i}", estado: "cerrado");
        await CrearAsync("Ticket abierto", estado: "abierto");

        var pagina = await _client.GetFromJsonAsync<PagedResponse<TicketResponse>>("/api/tickets?estado=cerrado&pageSize=2");

        Assert.Equal(3, pagina!.Total);
        Assert.Equal(2, pagina.Items.Count);
        Assert.All(pagina.Items, t => Assert.Equal("cerrado", t.Estado));
    }

    [Fact]
    public async Task List_PageSizeFueraDeRango_Devuelve400()
    {
        var response = await _client.GetAsync("/api/tickets?pageSize=500");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Update_TicketExistente_Devuelve200ConLosCambios()
    {
        var creado = await CrearAsync("Ticket original");

        var response = await _client.PutAsJsonAsync($"/api/tickets/{creado.Id}", new UpdateTicketRequest
        {
            Titulo = "Ticket editado",
            Canal = "email",
            Estado = "en_proceso",
            Prioridad = "alta",
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var editado = await response.Content.ReadFromJsonAsync<TicketResponse>();
        Assert.Equal("Ticket editado", editado!.Titulo);
        Assert.Equal("en_proceso", editado.Estado);
        Assert.Equal("alta", editado.Prioridad);
    }

    [Fact]
    public async Task Update_TicketInexistente_Devuelve404()
    {
        var response = await _client.PutAsJsonAsync($"/api/tickets/{Guid.NewGuid()}", new UpdateTicketRequest
        {
            Titulo = "Ticket editado",
            Canal = "web",
            Estado = "abierto",
            Prioridad = "media",
        });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ChangeStatus_EstadoValido_Devuelve200()
    {
        var creado = await CrearAsync("Ticket para estado");

        var response = await _client.PatchAsJsonAsync($"/api/tickets/{creado.Id}/estado", new ChangeStatusRequest { Estado = "cerrado" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var ticket = await response.Content.ReadFromJsonAsync<TicketResponse>();
        Assert.Equal("cerrado", ticket!.Estado);
    }

    [Fact]
    public async Task ChangeStatus_EstadoInvalido_Devuelve400()
    {
        var creado = await CrearAsync("Ticket para estado");

        var response = await _client.PatchAsJsonAsync($"/api/tickets/{creado.Id}/estado", new ChangeStatusRequest { Estado = "pendiente" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Delete_TicketExistente_Devuelve204YLuego404()
    {
        var creado = await CrearAsync("Ticket a eliminar");

        var primera = await _client.DeleteAsync($"/api/tickets/{creado.Id}");
        var segunda = await _client.DeleteAsync($"/api/tickets/{creado.Id}");

        Assert.Equal(HttpStatusCode.NoContent, primera.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, segunda.StatusCode);
    }

    private async Task<TicketResponse> CrearAsync(string titulo, string? estado = null)
    {
        var response = await _client.PostAsJsonAsync("/api/tickets",
            new CreateTicketRequest { Titulo = titulo, Canal = "web", Estado = estado });
        return (await response.Content.ReadFromJsonAsync<TicketResponse>())!;
    }
}
