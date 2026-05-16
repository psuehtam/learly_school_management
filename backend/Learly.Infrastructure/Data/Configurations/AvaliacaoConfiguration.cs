using Learly.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Learly.Infrastructure.Data.Configurations;

public sealed class AvaliacaoConfiguration : IEntityTypeConfiguration<Avaliacao>
{
    public void Configure(EntityTypeBuilder<Avaliacao> builder)
    {
        builder.ToTable("avaliacoes");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).HasColumnName("id").ValueGeneratedOnAdd();
        builder.Property(a => a.EscolaId).HasColumnName("escola_id");
        builder.Property(a => a.TurmaId).HasColumnName("turma_id");
        builder.Property(a => a.AlunoId).HasColumnName("aluno_id");
        builder.Property(a => a.TipoAvaliacao).HasColumnName("tipo_avaliacao").HasMaxLength(64).IsRequired();
        builder.Property(a => a.Nota).HasColumnName("nota").HasPrecision(4, 2);
        builder.HasIndex(a => new { a.TurmaId, a.AlunoId, a.TipoAvaliacao }).IsUnique();
    }
}
