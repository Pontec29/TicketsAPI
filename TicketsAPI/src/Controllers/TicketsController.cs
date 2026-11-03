using Microsoft.AspNetCore.Mvc;
using TicketsAPI.Dtos;
using TicketsAPI.Services;

namespace TicketsAPI.Controllers;

[ApiController]
[Route("api/tickets")]
public class TicketsController(ITicketService service) : ControllerBase
{
    public const string GetByIdRoute = "GetTicketById";

    // GET api/tickets?estado=&canal=&prioridad=&page=&pageSize=
    [HttpGet]
    public async Task<ActionResult<PagedResponse<TicketResponse>>> List([FromQuery] TicketQuery query, CancellationToken ct) =>
        Ok(await service.ListAsync(query, ct));

    // GET api/tickets/{id}
    [HttpGet("{id:guid}", Name = GetByIdRoute)]
    public async Task<ActionResult<TicketResponse>> GetById(Guid id, CancellationToken ct)
    {
        var ticket = await service.GetByIdAsync(id, ct);
        if (ticket is null) return NotFoundTicket(id);

        return Ok(ticket);
    }

    // POST api/tickets: 201 con Location apuntando a GET api/tickets/{id}.
    [HttpPost]
    public async Task<ActionResult<TicketResponse>> Create([FromBody] CreateTicketRequest request, CancellationToken ct)
    {
        var ticket = await service.CreateAsync(request, ct);
        return CreatedAtRoute(GetByIdRoute, new { id = ticket.Id }, ticket);
    }

    // PUT api/tickets/{id}: reemplaza los campos editables.
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<TicketResponse>> Update(Guid id, [FromBody] UpdateTicketRequest request, CancellationToken ct)
    {
        var ticket = await service.UpdateAsync(id, request, ct);
        if (ticket is null) return NotFoundTicket(id);

        return Ok(ticket);
    }

    // PATCH api/tickets/{id}/estado: cambio rápido de estado.
    [HttpPatch("{id:guid}/estado")]
    public async Task<ActionResult<TicketResponse>> ChangeStatus(Guid id, [FromBody] ChangeStatusRequest request, CancellationToken ct)
    {
        var ticket = await service.ChangeStatusAsync(id, request.Estado, ct);
        if (ticket is null) return NotFoundTicket(id);

        return Ok(ticket);
    }

    // DELETE api/tickets/{id}: 204 si se eliminó, 404 si no existe.
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var deleted = await service.DeleteAsync(id, ct);
        return deleted ? NoContent() : NotFoundTicket(id);
    }

    private ObjectResult NotFoundTicket(Guid id) =>
        Problem(statusCode: StatusCodes.Status404NotFound, title: "Ticket no encontrado", detail: $"No existe un ticket con Id {id}.");
}
