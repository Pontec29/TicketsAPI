using System.ComponentModel.DataAnnotations;
using TicketsAPI.Models;

namespace TicketsAPI.Dtos;

// Parámetros de GET api/tickets (se leen desde la query string).
public class TicketQuery
{
    [RegularExpression(TicketValues.EstadosRegex)]
    public string? Estado { get; init; }

    [RegularExpression(TicketValues.CanalesRegex)]
    public string? Canal { get; init; }

    [RegularExpression(TicketValues.PrioridadesRegex)]
    public string? Prioridad { get; init; }

    [Range(1, int.MaxValue)]
    public int Page { get; init; } = 1;

    [Range(1, 100)]
    public int PageSize { get; init; } = 10;
}
