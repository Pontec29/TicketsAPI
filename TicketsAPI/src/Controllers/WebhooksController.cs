using Microsoft.AspNetCore.Mvc;
using TicketsAPI.Dtos;
using TicketsAPI.Services;

namespace TicketsAPI.Controllers;

[ApiController]
[Route("api/webhooks")]
public class WebhooksController(ITicketService service) : ControllerBase
{
    // POST api/webhooks/mensajes
    // 201 si el mensaje crea un ticket nuevo; 200 si el mensajeId ya fue procesado.
    // La deduplicación está en ITicketService.RegisterWhatsAppMessageAsync.
    [HttpPost("mensajes")]
    public async Task<ActionResult<WebhookResponse>> ReceiveMessage([FromBody] WhatsAppMessageRequest request, CancellationToken ct)
    {
        var result = await service.RegisterWhatsAppMessageAsync(request, ct);

        if (result.Created)
        {
            return CreatedAtRoute(TicketsController.GetByIdRoute, new { id = result.TicketId },
                new WebhookResponse { Estado = "creado", TicketId = result.TicketId });
        }

        return Ok(new WebhookResponse { Estado = "ya_procesado", TicketId = result.TicketId });
    }
}
