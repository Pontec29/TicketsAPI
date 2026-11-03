namespace TicketsAPI.Dtos;

// Respuesta del webhook: "creado" (201) o "ya_procesado" (200 ante un mensajeId repetido).
public class WebhookResponse
{
    public string Estado { get; init; } = string.Empty;
    public Guid TicketId { get; init; }
}
