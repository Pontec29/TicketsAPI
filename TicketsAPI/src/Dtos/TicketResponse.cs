namespace TicketsAPI.Dtos;

// Representación pública de un ticket. Nunca se expone la entidad directamente.
public class TicketResponse
{
    public Guid Id { get; init; }
    public string Titulo { get; init; } = string.Empty;
    public string? Descripcion { get; init; }
    public string Canal { get; init; } = string.Empty;
    public string Estado { get; init; } = string.Empty;
    public string Prioridad { get; init; } = string.Empty;
    public string? MensajeExternoId { get; init; }
    public DateTime CreadoEn { get; init; }
    public DateTime ActualizadoEn { get; init; }
}
