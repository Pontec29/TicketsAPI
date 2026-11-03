using TicketsAPI.Dtos;
using TicketsAPI.Models;

namespace TicketsAPI.Repository;

public interface ITicketRepository
{
    Task<(List<Ticket> Items, int Total)> GetPageAsync(TicketFilter filter, PageRequest page, CancellationToken ct);

    // Solo lectura: sin seguimiento de cambios.
    Task<Ticket?> GetByIdAsync(Guid id, CancellationToken ct);

    // Para escritura: el ticket queda seguido por el contexto y se guarda con SaveChangesAsync.
    Task<Ticket?> GetForUpdateAsync(Guid id, CancellationToken ct);

    Task<Ticket?> GetByMensajeExternoIdAsync(string mensajeExternoId, CancellationToken ct);

    void Add(Ticket ticket);

    void Remove(Ticket ticket);

    Task<int> SaveChangesAsync(CancellationToken ct);
}
