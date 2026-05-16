using Learly.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Learly.Infrastructure.Data.Configurations;

public sealed class PresencaConfiguration : IEntityTypeConfiguration<Presenca>
{
    public void Configure(EntityTypeBuilder<Presenca> builder)
    {
        builder.ToTable("presencas");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasColumnName("id").ValueGeneratedOnAdd();
        builder.Property(p => p.EscolaId).HasColumnName("escola_id");
        builder.Property(p => p.AulaId).HasColumnName("aula_id");
        builder.Property(p => p.AlunoId).HasColumnName("aluno_id");
        builder.Property(p => p.StatusPresenca).HasColumnName("status_presenca").HasMaxLength(4).IsRequired();
        builder.Property(p => p.ReposicaoDePresencaId).HasColumnName("reposicao_de_presenca_id");
        builder.HasIndex(p => new { p.AulaId, p.AlunoId }).IsUnique();
    }
}
