using System.ComponentModel.DataAnnotations;

namespace TicketsAPI.Dtos;

// Payload que envía el proveedor de mensajería a POST api/webhooks/mensajes.
public class WhatsAppMessageRequest : IValidatableObject
{
    [Required]
    [RegularExpression("^mensaje\\.recibido$")]
    public string Evento { get; init; } = string.Empty;

    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string MensajeId { get; init; } = string.Empty;

    [Required]
    public WhatsAppSender Remitente { get; init; } = new();

    [Required]
    [StringLength(1000, MinimumLength = 1)]
    public string Texto { get; init; } = string.Empty;

    [Required]
    public DateTime Timestamp { get; init; }

    // DataAnnotations no valida objetos anidados sin atributo específico: se valida Remitente aquí.
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(Remitente, new ValidationContext(Remitente), results, validateAllProperties: true);
        return results;
    }
}

public class WhatsAppSender
{
    [Required]
    [StringLength(20, MinimumLength = 1)]
    public string Telefono { get; init; } = string.Empty;

    [StringLength(100)]
    public string? Nombre { get; init; }
}
