using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using TicketsAPI.Dtos;
using TicketsAPI.Models;
using TicketsAPI.Repository;

namespace TicketsAPI.Services;

public class TicketService(ITicketRepository repository, ILogger<TicketService> logger) : ITicketService
{
    // Errores de SQL Server por violar un índice o restricción única.
    private const int UniqueIndexViolation = 2601;
    private const int UniqueConstraintViolation = 2627;

    public async Task<PagedResponse<TicketResponse>> ListAsync(TicketQuery query, CancellationToken ct)
    {
        var filter = new TicketFilter(query.Estado, query.Canal, query.Prioridad);
        var page = new PageRequest(query.Page, query.PageSize);
        var (items, total) = await repository.GetPageAsync(filter, page, ct);

        return new PagedResponse<TicketResponse>
        {
            Items = items.Select(ToResponse).ToList(),
            Total = total,
            Page = page.Page,
            PageSize = page.PageSize,
        };
    }

    public async Task<TicketResponse?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var ticket = await repository.GetByIdAsync(id, ct);
        return ticket is null ? null : ToResponse(ticket);
    }

    public async Task<TicketResponse> CreateAsync(CreateTicketRequest request, CancellationToken ct)
    {
        var ticket = new Ticket
        {
            Titulo = request.Titulo,
            Descripcion = request.Descripcion,
            Canal = request.Canal,
            Estado = request.Estado ?? TicketValues.Abierto,
            Prioridad = request.Prioridad ?? TicketValues.Media,
        };

        repository.Add(ticket);
        await repository.SaveChangesAsync(ct);

        logger.LogInformation("Ticket {TicketId} creado desde el canal {Canal}", ticket.Id, ticket.Canal);
        return ToResponse(ticket);
    }

    public async Task<TicketResponse?> UpdateAsync(Guid id, UpdateTicketRequest request, CancellationToken ct)
    {
        var ticket = await repository.GetForUpdateAsync(id, ct);
        if (ticket is null) return null;

        ticket.Titulo = request.Titulo;
        ticket.Descripcion = request.Descripcion;
        ticket.Canal = request.Canal;
        ticket.Estado = request.Estado;
        ticket.Prioridad = request.Prioridad;
        ticket.ActualizadoEn = DateTime.UtcNow;

        await repository.SaveChangesAsync(ct);
        return ToResponse(ticket);
    }

    public async Task<TicketResponse?> ChangeStatusAsync(Guid id, string estado, CancellationToken ct)
    {
        var ticket = await repository.GetForUpdateAsync(id, ct);
        if (ticket is null) return null;

        ticket.Estado = estado;
        ticket.ActualizadoEn = DateTime.UtcNow;

        await repository.SaveChangesAsync(ct);
        return ToResponse(ticket);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct)
    {
        var ticket = await repository.GetForUpdateAsync(id, ct);
        if (ticket is null) return false;

        repository.Remove(ticket);
        await repository.SaveChangesAsync(ct);
        return true;
    }

    // Idempotente por MensajeExternoId: el chequeo previo evita el INSERT en el caso normal;
    // el índice único filtrado de la base cubre la carrera entre dos entregas simultáneas del mismo mensaje.
    public async Task<WebhookResult> RegisterWhatsAppMessageAsync(WhatsAppMessageRequest request, CancellationToken ct)
    {
        var existing = await repository.GetByMensajeExternoIdAsync(request.MensajeId, ct);
        if (existing is not null)
        {
            logger.LogInformation("Webhook duplicado para mensaje {MensajeId}; ticket existente {TicketId}",
                request.MensajeId, existing.Id);
            return new WebhookResult(existing.Id, Created: false);
        }

        var sender = request.Remitente.Nombre ?? request.Remitente.Telefono;
        var ticket = new Ticket
        {
            Titulo = Truncate($"WhatsApp de {sender}: {request.Texto}", 120),
            Descripcion = request.Texto,
            Canal = TicketValues.WhatsApp,
            Estado = TicketValues.Abierto,
            Prioridad = TicketValues.Media,
            MensajeExternoId = request.MensajeId,
        };

        repository.Add(ticket);
        try
        {
            await repository.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            var winner = await repository.GetByMensajeExternoIdAsync(request.MensajeId, ct)
                ?? throw new InvalidOperationException(
                    $"Violación única sin ticket existente para el mensaje {request.MensajeId}.", ex);

            logger.LogInformation("Webhook duplicado (carrera) para mensaje {MensajeId}; ticket existente {TicketId}",
                request.MensajeId, winner.Id);
            return new WebhookResult(winner.Id, Created: false);
        }

        logger.LogInformation(
            "Ticket {TicketId} creado desde webhook de WhatsApp (mensaje {MensajeId}, enviado {Timestamp})",
            ticket.Id, request.MensajeId, request.Timestamp);
        return new WebhookResult(ticket.Id, Created: true);
    }

    private static TicketResponse ToResponse(Ticket ticket) => new()
    {
        Id = ticket.Id,
        Titulo = ticket.Titulo,
        Descripcion = ticket.Descripcion,
        Canal = ticket.Canal,
        Estado = ticket.Estado,
        Prioridad = ticket.Prioridad,
        MensajeExternoId = ticket.MensajeExternoId,
        CreadoEn = DateTime.SpecifyKind(ticket.CreadoEn, DateTimeKind.Utc),
        ActualizadoEn = DateTime.SpecifyKind(ticket.ActualizadoEn, DateTimeKind.Utc),
    };

    private static bool IsUniqueViolation(DbUpdateException ex) =>
        ex.InnerException is SqlException { Number: UniqueIndexViolation or UniqueConstraintViolation };

    private static string Truncate(string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..maxLength];
}
