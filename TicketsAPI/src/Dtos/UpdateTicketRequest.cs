using System.ComponentModel.DataAnnotations;
using TicketsAPI.Models;

namespace TicketsAPI.Dtos;

// PUT: reemplaza todos los campos editables, por eso Estado y Prioridad son obligatorios.
public class UpdateTicketRequest
{
    [Required]
    [StringLength(120, MinimumLength = 5)]
    public string Titulo { get; init; } = string.Empty;

    [StringLength(1000)]
    public string? Descripcion { get; init; }

    [Required]
    [RegularExpression(TicketValues.CanalesRegex)]
    public string Canal { get; init; } = string.Empty;

    [Required]
    [RegularExpression(TicketValues.EstadosRegex)]
    public string Estado { get; init; } = string.Empty;

    [Required]
    [RegularExpression(TicketValues.PrioridadesRegex)]
    public string Prioridad { get; init; } = string.Empty;
}
