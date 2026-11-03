namespace TicketsAPI.Models;

public class Ticket
{
    public Guid Id { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public string Canal { get; set; } = string.Empty;
    public string Estado { get; set; } = TicketValues.Abierto;
    public string Prioridad { get; set; } = TicketValues.Media;
    public string? MensajeExternoId { get; set; }
    public DateTime CreadoEn { get; set; }
    public DateTime ActualizadoEn { get; set; }
}

// Valores permitidos. Deben coincidir con los CHECK de db/01_schema.sql.
public static class TicketValues
{
    public const string Web = "web";
    public const string WhatsApp = "whatsapp";
    public const string Email = "email";

    public const string Abierto = "abierto";
    public const string EnProceso = "en_proceso";
    public const string Cerrado = "cerrado";

    public const string Baja = "baja";
    public const string Media = "media";
    public const string Alta = "alta";

    public const string CanalesRegex = "^(web|whatsapp|email)$";
    public const string EstadosRegex = "^(abierto|en_proceso|cerrado)$";
    public const string PrioridadesRegex = "^(baja|media|alta)$";
}
