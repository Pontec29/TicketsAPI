using Microsoft.EntityFrameworkCore;
using TicketsAPI.Data;
using TicketsAPI.Dtos;
using TicketsAPI.Models;

namespace TicketsAPI.Repository;

public class TicketRepository(AppDbContext db) : ITicketRepository
{
    public async Task<(List<Ticket> Items, int Total)> GetPageAsync(TicketFilter filter, PageRequest page, CancellationToken ct)
    {
        IQueryable<Ticket> query = db.Tickets.AsNoTracking();

        if (filter.Estado is not null) query = query.Where(t => t.Estado == filter.Estado);
        if (filter.Canal is not null) query = query.Where(t => t.Canal == filter.Canal);
        if (filter.Prioridad is not null) query = query.Where(t => t.Prioridad == filter.Prioridad);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(t => t.CreadoEn)
            .Skip(page.Skip)
            .Take(page.PageSize)
            .ToListAsync(ct);

        return (items, total);
    }

    public Task<Ticket?> GetByIdAsync(Guid id, CancellationToken ct) =>
        db.Tickets.AsNoTracking().FirstOrDefaultAsync(t => t.Id == id, ct);

    public Task<Ticket?> GetForUpdateAsync(Guid id, CancellationToken ct) =>
        db.Tickets.FirstOrDefaultAsync(t => t.Id == id, ct);

    public Task<Ticket?> GetByMensajeExternoIdAsync(string mensajeExternoId, CancellationToken ct) =>
        db.Tickets.AsNoTracking().FirstOrDefaultAsync(t => t.MensajeExternoId == mensajeExternoId, ct);

    public void Add(Ticket ticket) => db.Tickets.Add(ticket);

    public void Remove(Ticket ticket) => db.Tickets.Remove(ticket);

    public Task<int> SaveChangesAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
}
