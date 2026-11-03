namespace TicketsAPI.Services;

// Created = true si se creó el ticket; false si el mensaje ya había sido procesado.
public record WebhookResult(Guid TicketId, bool Created);
