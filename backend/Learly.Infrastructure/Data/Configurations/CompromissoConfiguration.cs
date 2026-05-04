using Learly.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Learly.Infrastructure.Data.Configurations;

public sealed class CompromissoConfiguration : IEntityTypeConfiguration<Compromisso>
{
    public void Configure(EntityTypeBuilder<Compromisso> builder)
    {
        builder.ToTable("compromissos");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).HasColumnName("id").ValueGeneratedOnAdd();

        builder.Property(c => c.EscolaId).HasColumnName("escola_id");
        builder.Property(c => c.UsuarioId).HasColumnName("usuario_id");
        builder.Property(c => c.Titulo).HasColumnName("titulo").HasMaxLength(255).IsRequired();
        builder.Property(c => c.Descricao).HasColumnName("descricao");
        builder.Property(c => c.DataInicio).HasColumnName("data_inicio");
        builder.Property(c => c.DataFim).HasColumnName("data_fim");
        builder.Property(c => c.Local).HasColumnName("local").HasMaxLength(255);
        builder.Property(c => c.Tipo).HasColumnName("tipo").HasMaxLength(32).IsRequired();
        builder.Property(c => c.Prioridade).HasColumnName("prioridade").HasMaxLength(16).IsRequired();
        builder.Property(c => c.Status).HasColumnName("status").HasMaxLength(32).IsRequired();
        builder.Property(c => c.LembreteMinutos).HasColumnName("lembrete_minutos");
        builder.Property(c => c.Cor).HasColumnName("cor").HasMaxLength(7);

        builder.HasOne<Escola>().WithMany().HasForeignKey(c => c.EscolaId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Usuario>().WithMany().HasForeignKey(c => c.UsuarioId).OnDelete(DeleteBehavior.Restrict);
    }
}
