using Learly.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Learly.Infrastructure.Data.Configurations;

public sealed class TurmaDiaSemanaConfiguration : IEntityTypeConfiguration<TurmaDiaSemana>
{
    public void Configure(EntityTypeBuilder<TurmaDiaSemana> builder)
    {
        builder.ToTable("turmas_dias_semana");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedOnAdd();
        builder.Property(x => x.EscolaId).HasColumnName("escola_id");
        builder.Property(x => x.TurmaId).HasColumnName("turma_id");
        builder.Property(x => x.DiaSemana).HasColumnName("dia_semana");
        builder.HasIndex(x => new { x.TurmaId, x.DiaSemana }).IsUnique();
    }
}
