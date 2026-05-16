using Learly.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Learly.Infrastructure.Data.Configurations;

public sealed class TurmaCapituloProgressoConfiguration : IEntityTypeConfiguration<TurmaCapituloProgresso>
{
    public void Configure(EntityTypeBuilder<TurmaCapituloProgresso> builder)
    {
        builder.ToTable("turmas_capitulos_progresso");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedOnAdd();
        builder.Property(x => x.EscolaId).HasColumnName("escola_id");
        builder.Property(x => x.TurmaId).HasColumnName("turma_id");
        builder.Property(x => x.CapituloId).HasColumnName("capitulo_id");
        builder.Property(x => x.Concluido).HasColumnName("concluido");
        builder.Property(x => x.DataConclusao).HasColumnName("data_conclusao");
        builder.HasIndex(x => new { x.TurmaId, x.CapituloId }).IsUnique();
    }
}
