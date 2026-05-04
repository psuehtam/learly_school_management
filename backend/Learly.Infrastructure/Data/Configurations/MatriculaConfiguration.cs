using Learly.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Learly.Infrastructure.Data.Configurations;

public sealed class MatriculaConfiguration : IEntityTypeConfiguration<Matricula>
{
    public void Configure(EntityTypeBuilder<Matricula> builder)
    {
        builder.ToTable("matriculas");

        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).HasColumnName("id").ValueGeneratedOnAdd();

        builder.Property(m => m.EscolaId).HasColumnName("escola_id");
        builder.Property(m => m.AlunoId).HasColumnName("aluno_id");
        builder.Property(m => m.TurmaId).HasColumnName("turma_id");

        builder.Property(m => m.DataMatricula).HasColumnName("data_matricula");

        // MySQL ENUM alinhado ao banco (sem acentuação nos valores, conforme documentação do projeto).
        builder.Property(m => m.Status)
            .HasColumnName("status")
            .HasColumnType("enum('Ativo','Concluido','Trancado','Cancelado','Em Espera')")
            .IsRequired();

        builder.Property(m => m.DataCriacao).HasColumnName("data_criacao");
        builder.Property(m => m.DataAtualizacao).HasColumnName("data_atualizacao");

        builder.HasOne<Escola>()
            .WithMany()
            .HasForeignKey(m => m.EscolaId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Turma>()
            .WithMany()
            .HasForeignKey(m => m.TurmaId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        // MySQL: em índices UNIQUE, NULL em turma_id não colide com outro NULL — várias linhas (escola, aluno, NULL) são permitidas.
        // Regra "no máximo uma matrícula sem turma por aluno" deve ser aplicada na camada de serviço, se necessário.
        builder.HasIndex(m => new { m.EscolaId, m.AlunoId, m.TurmaId })
            .IsUnique()
            .HasDatabaseName("uq_matriculas_escola_aluno_turma");
    }
}
