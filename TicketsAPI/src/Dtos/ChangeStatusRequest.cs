using System.ComponentModel.DataAnnotations;
using TicketsAPI.Models;

namespace TicketsAPI.Dtos;

// PATCH api/tickets/{id}/estado
public class ChangeStatusRequest
{
    [Required]
    [RegularExpression(TicketValues.EstadosRegex)]
    public string Estado { get; init; } = string.Empty;
}
