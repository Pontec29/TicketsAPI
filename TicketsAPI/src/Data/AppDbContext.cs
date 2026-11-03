using Microsoft.EntityFrameworkCore;
using TicketsAPI.Data.Configurations;
using TicketsAPI.Models;

namespace TicketsAPI.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Ticket> Tickets => Set<Ticket>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new TicketConfiguration());
    }
}
