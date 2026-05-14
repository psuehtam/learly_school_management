using Learly.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Learly.Infrastructure.Data.Configurations;

/// <summary>Modelo reduzido: colunas usadas em formulários/listagens; persistência ampla pode usar SQL direto.</summary>
public sealed class ResponsavelConfiguration : IEntityTypeConfiguration<Responsavel>
{
    public void Configure(EntityTypeBuilder<Responsavel> builder)
    {
        builder.ToTable("responsaveis", t => t.ExcludeFromMigrations());

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).HasColumnName("id").ValueGeneratedOnAdd();

        builder.Property(r => r.EscolaId).HasColumnName("escola_id");
        builder.Property(r => r.TipoPessoa)
            .HasColumnName("tipo_pessoa")
            .HasColumnType("enum('Fisica', 'Juridica')")
            .IsRequired();

        builder.Property(r => r.CpfCnpj).HasColumnName("cpf_cnpj").HasMaxLength(20).IsRequired();
        builder.Property(r => r.Nome).HasColumnName("nome").HasMaxLength(100).IsRequired();
        builder.Property(r => r.Sobrenome).HasColumnName("sobrenome").HasMaxLength(100).IsRequired();
        builder.Property(r => r.Status)
            .HasColumnName("status")
            .HasColumnType("enum('Ativo', 'Inativo')")
            .HasDefaultValue("Ativo");

        builder.HasIndex(r => new { r.EscolaId, r.CpfCnpj }).IsUnique();
    }
}
