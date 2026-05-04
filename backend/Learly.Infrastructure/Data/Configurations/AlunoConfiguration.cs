using Learly.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Learly.Infrastructure.Data.Configurations;

public sealed class AlunoConfiguration : IEntityTypeConfiguration<Aluno>
{
    public void Configure(EntityTypeBuilder<Aluno> builder)
    {
        builder.ToTable("alunos");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).HasColumnName("id").ValueGeneratedOnAdd();

        builder.Property(a => a.EscolaId).HasColumnName("escola_id");
        builder.Property(a => a.ResponsavelId).HasColumnName("responsavel_id");
        builder.Property(a => a.EProprioResponsavel).HasColumnName("e_proprio_responsavel");

        builder.Property(a => a.Nome).HasColumnName("nome").HasMaxLength(100).IsRequired();
        builder.Property(a => a.Sobrenome).HasColumnName("sobrenome").HasMaxLength(100).IsRequired();
        builder.Property(a => a.Sexo).HasColumnName("sexo").HasColumnType("enum('Masculino', 'Feminino', 'Outro')").IsRequired();
        builder.Property(a => a.DataNascimento).HasColumnName("data_nascimento");
        builder.Property(a => a.DataIngresso).HasColumnName("data_ingresso");
        builder.Property(a => a.Cpf).HasColumnName("cpf").HasMaxLength(20);
        builder.Property(a => a.Cep).HasColumnName("cep").HasMaxLength(10).IsRequired();
        builder.Property(a => a.TipoLogradouro).HasColumnName("tipo_logradouro").HasColumnType("enum('Rua', 'Avenida', 'Travessa', 'Alameda', 'Estrada', 'Rodovia', 'Outro')").IsRequired();
        builder.Property(a => a.Logradouro).HasColumnName("logradouro").HasMaxLength(150).IsRequired();
        builder.Property(a => a.Numero).HasColumnName("numero").HasMaxLength(20).IsRequired();
        builder.Property(a => a.Complemento).HasColumnName("complemento").HasMaxLength(100);
        builder.Property(a => a.Bairro).HasColumnName("bairro").HasMaxLength(100).IsRequired();
        builder.Property(a => a.Municipio).HasColumnName("municipio").HasMaxLength(100).IsRequired();
        builder.Property(a => a.Status).HasColumnName("status").HasColumnType("enum('Ativo', 'Inativo', 'Trancado')").IsRequired();
        builder.Property(a => a.DataCriacao).HasColumnName("data_criacao");
        builder.Property(a => a.DataAtualizacao).HasColumnName("data_atualizacao");

        builder.HasOne<Escola>()
            .WithMany()
            .HasForeignKey(a => a.EscolaId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(a => new { a.EscolaId, a.Cpf }).IsUnique();
    }
}
