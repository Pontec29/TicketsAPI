using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TicketsAPI.Models;

namespace TicketsAPI.Data.Configurations;

// Mapeo de Ticket a la tabla Tickets
public class TicketConfiguration : IEntityTypeConfiguration<Ticket>
{
    public void Configure(EntityTypeBuilder<Ticket> builder)
    {
        builder.ToTable("Tickets");
        builder.HasKey(t => t.Id);

        
        builder.Property(t => t.Id).ValueGeneratedOnAdd().HasDefaultValueSql("NEWID()");
        builder.Property(t => t.Titulo).HasMaxLength(120).IsRequired();
        builder.Property(t => t.Descripcion).HasMaxLength(1000);
        builder.Property(t => t.Canal).HasMaxLength(20).IsUnicode(false).IsRequired();
        builder.Property(t => t.Estado).HasMaxLength(20).IsUnicode(false).IsRequired();
        builder.Property(t => t.Prioridad).HasMaxLength(10).IsUnicode(false).IsRequired();
        builder.Property(t => t.MensajeExternoId).HasMaxLength(100).IsUnicode(false);
        builder.Property(t => t.CreadoEn).HasColumnType("datetime2")
            .ValueGeneratedOnAdd().HasDefaultValueSql("SYSUTCDATETIME()");
        builder.Property(t => t.ActualizadoEn).HasColumnType("datetime2")
            .ValueGeneratedOnAdd().HasDefaultValueSql("SYSUTCDATETIME()");

        // Coincide con UQ_Tickets_MensajeExternoId (idempotencia del webhook).
        builder.HasIndex(t => t.MensajeExternoId)
            .IsUnique()
            .HasFilter("[MensajeExternoId] IS NOT NULL")
            .HasDatabaseName("UQ_Tickets_MensajeExternoId");

        // Coincide con IX_Tickets_Estado_CreadoEn
        builder.HasIndex(t => new { t.Estado, t.CreadoEn })
            .HasDatabaseName("IX_Tickets_Estado_CreadoEn");
    }
}
