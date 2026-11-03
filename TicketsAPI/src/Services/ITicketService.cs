using TicketsAPI.Dtos;

namespace TicketsAPI.Services;

public interface ITicketService
{
    Task<PagedResponse<TicketResponse>> ListAsync(TicketQuery query, CancellationToken ct);

    Task<TicketResponse?> GetByIdAsync(Guid id, CancellationToken ct);

    Task<TicketResponse> CreateAsync(CreateTicketRequest request, CancellationToken ct);

    // null si el ticket no existe.
    Task<TicketResponse?> UpdateAsync(Guid id, UpdateTicketRequest request, CancellationToken ct);

    Task<TicketResponse?> ChangeStatusAsync(Guid id, string estado, CancellationToken ct);

    // false si el ticket no existe.
    Task<bool> DeleteAsync(Guid id, CancellationToken ct);

    Task<WebhookResult> RegisterWhatsAppMessageAsync(WhatsAppMessageRequest request, CancellationToken ct);
}
