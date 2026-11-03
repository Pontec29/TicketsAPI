namespace TicketsAPI.Repository;

// Filtros opcionales del listado. Null significa "sin filtrar" por ese campo.
public record TicketFilter(string? Estado, string? Canal, string? Prioridad);
