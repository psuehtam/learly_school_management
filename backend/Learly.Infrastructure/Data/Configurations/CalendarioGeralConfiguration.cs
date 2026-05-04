using Learly.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Learly.Infrastructure.Data.Configurations;

public sealed class CalendarioGeralConfiguration : IEntityTypeConfiguration<CalendarioGeral>
{
    public void Configure(EntityTypeBuilder<CalendarioGeral> builder)
    {
        builder.ToTable("calendario_geral");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).HasColumnName("id").ValueGeneratedOnAdd();

        builder.Property(c => c.EscolaId).HasColumnName("escola_id");
        builder.Property(c => c.DataEvento).HasColumnName("data_evento");
        builder.Property(c => c.TipoEvento)
            .HasColumnName("tipo_evento")
            .HasMaxLength(16)
            .IsRequired();
        builder.Property(c => c.Descricao)
            .HasColumnName("descricao")
            .HasMaxLength(255);
        builder.Property(c => c.SuspendeAula).HasColumnName("suspende_aula");
        builder.Property(c => c.UsuarioId).HasColumnName("usuario_id");
        builder.Property(c => c.DataCriacao).HasColumnName("data_criacao");

        builder.HasIndex(c => new { c.EscolaId, c.DataEvento })
            .IsUnique();

        builder.HasOne<Escola>()
            .WithMany()
            .HasForeignKey(c => c.EscolaId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Usuario>()
            .WithMany()
            .HasForeignKey(c => c.UsuarioId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
