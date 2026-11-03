using System.ComponentModel.DataAnnotations;

namespace TicketsAPI.Dtos;

public record PageRequest(
    [Range(1, int.MaxValue)] int Page = 1,
    [Range(1, 100)] int PageSize = 10)
{
    public int Skip => (Page - 1) * PageSize;
}
