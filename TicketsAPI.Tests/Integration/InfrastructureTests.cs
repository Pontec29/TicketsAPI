using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute;
using TicketsAPI.Dtos;
using TicketsAPI.Services;

namespace TicketsAPI.Tests.Integration;

[Collection("Api")]
public class InfrastructureTests(ApiFixture factory)
{
    [Fact]
    public async Task Health_Devuelve200()
    {
        var response = await factory.CreateClient().GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Cors_OrigenDelFrontend_RecibeElEncabezadoDeAcceso()
    {
        var response = await Preflight(ApiFixture.OrigenPermitido);

        Assert.True(response.Headers.TryGetValues("Access-Control-Allow-Origin", out var valores));
        Assert.Equal(ApiFixture.OrigenPermitido, valores!.Single());
    }

    [Fact]
    public async Task Cors_OrigenAjeno_NoRecibeElEncabezadoDeAcceso()
    {
        var response = await Preflight("https://sitio-ajeno.example");

        Assert.False(response.Headers.Contains("Access-Control-Allow-Origin"));
    }

    [Fact]
    public async Task OpenApi_EnProduccion_EstaDisponible()
    {
        using var produccion = factory.WithWebHostBuilder(builder => builder.UseEnvironment("Production"));

        var response = await produccion.CreateClient().GetAsync("/openapi/v1.json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task SwaggerUi_EstaDisponible()
    {
        var response = await factory.CreateClient().GetAsync("/swagger/index.html");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ExcepcionNoControlada_Devuelve500ConProblemDetailsSinDetallesInternos()
    {
        var servicioRoto = Substitute.For<ITicketService>();
        servicioRoto.ListAsync(Arg.Any<TicketQuery>(), Arg.Any<CancellationToken>())
            .Returns<Task<PagedResponse<TicketResponse>>>(_ => throw new InvalidOperationException("detalle-interno-secreto"));

        using var conFallo = factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<ITicketService>();
            services.AddScoped(_ => servicioRoto);
        }));

        var response = await conFallo.CreateClient().GetAsync("/api/tickets");
        var cuerpo = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.DoesNotContain("detalle-interno-secreto", cuerpo);
        Assert.DoesNotContain("InvalidOperationException", cuerpo);
        Assert.DoesNotContain("   at ", cuerpo);
    }

    private Task<HttpResponseMessage> Preflight(string origen)
    {
        var request = new HttpRequestMessage(HttpMethod.Options, "/api/tickets");
        request.Headers.Add("Origin", origen);
        request.Headers.Add("Access-Control-Request-Method", "POST");
        return factory.CreateClient().SendAsync(request);
    }
}
