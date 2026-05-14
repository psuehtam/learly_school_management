using Learly.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Learly.Infrastructure.Data.Configurations;

public sealed class CompromissoParticipanteConfiguration : IEntityTypeConfiguration<CompromissoParticipante>
{
    public void Configure(EntityTypeBuilder<CompromissoParticipante> builder)
    {
        builder.ToTable("compromissos_participantes");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).HasColumnName("id").ValueGeneratedOnAdd();
        builder.Property(c => c.CompromissoId).HasColumnName("compromisso_id");
        builder.Property(c => c.UsuarioId).HasColumnName("usuario_id");
        builder.Property(c => c.Confirmacao).HasColumnName("confirmacao").HasMaxLength(16).IsRequired();

        builder.HasIndex(c => new { c.CompromissoId, c.UsuarioId }).IsUnique();

        builder.HasOne<Compromisso>().WithMany().HasForeignKey(c => c.CompromissoId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<Usuario>().WithMany().HasForeignKey(c => c.UsuarioId).OnDelete(DeleteBehavior.Restrict);
    }
}
