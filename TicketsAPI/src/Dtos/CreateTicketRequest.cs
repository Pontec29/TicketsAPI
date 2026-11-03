using System.ComponentModel.DataAnnotations;
using TicketsAPI.Models;

namespace TicketsAPI.Dtos;

public class CreateTicketRequest
{
    [Required]
    [StringLength(120, MinimumLength = 5)]
    public string Titulo { get; init; } = string.Empty;

    [StringLength(1000)]
    public string? Descripcion { get; init; }

    [Required]
    [RegularExpression(TicketValues.CanalesRegex)]
    public string Canal { get; init; } = string.Empty;

    // Opcional: si no se envía, el servicio usa "abierto".
    [RegularExpression(TicketValues.EstadosRegex)]
    public string? Estado { get; init; }

    // Opcional: si no se envía, el servicio usa "media".
    [RegularExpression(TicketValues.PrioridadesRegex)]
    public string? Prioridad { get; init; }
}
