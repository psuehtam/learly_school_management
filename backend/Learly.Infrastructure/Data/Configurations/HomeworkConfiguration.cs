using Learly.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Learly.Infrastructure.Data.Configurations;

public sealed class HomeworkConfiguration : IEntityTypeConfiguration<Homework>
{
    public void Configure(EntityTypeBuilder<Homework> builder)
    {
        builder.ToTable("homeworks");
        builder.HasKey(h => h.Id);
        builder.Property(h => h.Id).HasColumnName("id").ValueGeneratedOnAdd();
        builder.Property(h => h.EscolaId).HasColumnName("escola_id");
        builder.Property(h => h.AulaId).HasColumnName("aula_id");
        builder.Property(h => h.AlunoId).HasColumnName("aluno_id");
        builder.Property(h => h.Nota).HasColumnName("nota").HasPrecision(4, 2);
        builder.HasIndex(h => new { h.AulaId, h.AlunoId }).IsUnique();
    }
}
