using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using TicketsAPI.Dtos;
using TicketsAPI.Models;
using TicketsAPI.Repository;
using TicketsAPI.Services;

namespace TicketsAPI.Tests.Unit;

// Pruebas unitarias: el repositorio se simula con NSubstitute, así que no se usa la base de datos.
public class TicketServiceTests
{
    private readonly ITicketRepository _repository = Substitute.For<ITicketRepository>();
    private readonly TicketService _service;

    public TicketServiceTests()
    {
        _service = new TicketService(_repository, NullLogger<TicketService>.Instance);
    }

    [Fact]
    public async Task CreateAsync_SinEstadoNiPrioridad_UsaAbiertoYMedia()
    {
        Ticket? guardado = null;
        _repository.When(r => r.Add(Arg.Any<Ticket>())).Do(call => guardado = call.Arg<Ticket>());

        await _service.CreateAsync(new CreateTicketRequest { Titulo = "Problema de login", Canal = "web" }, default);

        Assert.Equal("abierto", guardado!.Estado);
        Assert.Equal("media", guardado.Prioridad);
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ListAsync_ArmaFiltroYPaginaDesdeLaConsulta_YDevuelveElTotal()
    {
        var tickets = new List<Ticket> { new() { Id = Guid.NewGuid(), Titulo = "Uno", Estado = "cerrado" } };
        _repository.GetPageAsync(Arg.Any<TicketFilter>(), Arg.Any<PageRequest>(), Arg.Any<CancellationToken>())
            .Returns((tickets, 25));

        var result = await _service.ListAsync(new TicketQuery { Estado = "cerrado", Page = 2, PageSize = 10 }, default);

        Assert.Equal(25, result.Total);
        Assert.Equal(2, result.Page);
        Assert.Single(result.Items);
        await _repository.Received(1).GetPageAsync(
            Arg.Is<TicketFilter>(f => f.Estado == "cerrado" && f.Canal == null),
            Arg.Is<PageRequest>(p => p.Page == 2 && p.PageSize == 10 && p.Skip == 10),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ChangeStatusAsync_TicketExistente_CambiaEstadoYActualizaFecha()
    {
        var antes = DateTime.UtcNow.AddDays(-1);
        var ticket = new Ticket { Id = Guid.NewGuid(), Estado = "abierto", ActualizadoEn = antes };
        _repository.GetForUpdateAsync(ticket.Id, Arg.Any<CancellationToken>()).Returns(ticket);

        var result = await _service.ChangeStatusAsync(ticket.Id, "cerrado", default);

        Assert.Equal("cerrado", result!.Estado);
        Assert.True(ticket.ActualizadoEn > antes);
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ChangeStatusAsync_TicketInexistente_DevuelveNullSinGuardar()
    {
        _repository.GetForUpdateAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Ticket?)null);

        var result = await _service.ChangeStatusAsync(Guid.NewGuid(), "cerrado", default);

        Assert.Null(result);
        await _repository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RegisterWhatsApp_MensajeYaProcesado_NoInsertaYDevuelveElTicketOriginal()
    {
        var existente = new Ticket { Id = Guid.NewGuid(), MensajeExternoId = "wamid.1" };
        _repository.GetByMensajeExternoIdAsync("wamid.1", Arg.Any<CancellationToken>()).Returns(existente);

        var result = await _service.RegisterWhatsAppMessageAsync(Mensaje("wamid.1"), default);

        Assert.False(result.Created);
        Assert.Equal(existente.Id, result.TicketId);
        _repository.DidNotReceive().Add(Arg.Any<Ticket>());
        await _repository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RegisterWhatsApp_MensajeNuevo_CreaTicketConCanalWhatsApp()
    {
        _repository.GetByMensajeExternoIdAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((Ticket?)null);
        Ticket? guardado = null;
        _repository.When(r => r.Add(Arg.Any<Ticket>())).Do(call => guardado = call.Arg<Ticket>());

        var result = await _service.RegisterWhatsAppMessageAsync(Mensaje("wamid.2"), default);

        Assert.True(result.Created);
        Assert.Equal("whatsapp", guardado!.Canal);
        Assert.Equal("wamid.2", guardado.MensajeExternoId);
        Assert.Contains("Maria Perez", guardado.Titulo);
    }

    [Fact]
    public async Task RegisterWhatsApp_TextoLargo_TruncaElTituloA120YConservaLaDescripcion()
    {
        _repository.GetByMensajeExternoIdAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((Ticket?)null);
        Ticket? guardado = null;
        _repository.When(r => r.Add(Arg.Any<Ticket>())).Do(call => guardado = call.Arg<Ticket>());

        await _service.RegisterWhatsAppMessageAsync(Mensaje("wamid.3", new string('a', 1000)), default);

        Assert.Equal(120, guardado!.Titulo.Length);
        Assert.Equal(1000, guardado.Descripcion!.Length);
    }

    private static WhatsAppMessageRequest Mensaje(string mensajeId, string texto = "Hola, no puedo ingresar") => new()
    {
        Evento = "mensaje.recibido",
        MensajeId = mensajeId,
        Remitente = new WhatsAppSender { Telefono = "+51987654321", Nombre = "Maria Perez" },
        Texto = texto,
        Timestamp = DateTime.UtcNow,
    };
}
