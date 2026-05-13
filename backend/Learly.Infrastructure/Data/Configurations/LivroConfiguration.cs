using Learly.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Learly.Infrastructure.Data.Configurations;

public sealed class LivroConfiguration : IEntityTypeConfiguration<Livro>
{
    public void Configure(EntityTypeBuilder<Livro> builder)
    {
        builder.ToTable("livros", t => t.ExcludeFromMigrations());

        builder.HasKey(l => l.Id);
        builder.Property(l => l.Id).HasColumnName("id").ValueGeneratedOnAdd();

        builder.Property(l => l.EscolaId).HasColumnName("escola_id");
        builder.Property(l => l.Nome).HasColumnName("nome").HasMaxLength(150).IsRequired();
        builder.Property(l => l.Status)
            .HasColumnName("status")
            .HasColumnType("enum('Ativo', 'Inativo')")
            .HasDefaultValue("Ativo");

        builder.Property(l => l.DataCriacao).HasColumnName("data_criacao");
        builder.Property(l => l.DataAtualizacao).HasColumnName("data_atualizacao");

        builder.HasIndex(l => new { l.EscolaId, l.Status });
    }
}
