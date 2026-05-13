using Learly.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Learly.Infrastructure.Data.Configurations;

public sealed class EscolaHorarioFuncionamentoConfiguration : IEntityTypeConfiguration<EscolaHorarioFuncionamento>
{
    public void Configure(EntityTypeBuilder<EscolaHorarioFuncionamento> builder)
    {
        builder.ToTable("escolas_horarios_funcionamento", t => t.ExcludeFromMigrations());

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").ValueGeneratedOnAdd();
        builder.Property(e => e.EscolaId).HasColumnName("escola_id");
        builder.Property(e => e.DiaSemana).HasColumnName("dia_semana");
        builder.Property(e => e.Aberto).HasColumnName("aberto");
        builder.Property(e => e.HorarioAbertura).HasColumnName("horario_abertura");
        builder.Property(e => e.HorarioFechamento).HasColumnName("horario_fechamento");

        builder.HasIndex(e => new { e.EscolaId, e.DiaSemana }).IsUnique();

        builder.HasOne<Escola>()
            .WithMany()
            .HasForeignKey(e => e.EscolaId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
