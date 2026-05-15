using Learly.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Learly.Infrastructure.Data.Configurations;

internal sealed class ContratoTemplateConfiguration : IEntityTypeConfiguration<ContratoTemplate>
{
    public void Configure(EntityTypeBuilder<ContratoTemplate> builder)
    {
        builder.ToTable("contratos_templates");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).HasColumnName("id").ValueGeneratedOnAdd();
        builder.Property(t => t.EscolaId).HasColumnName("escola_id");
        builder.Property(t => t.Nome).HasColumnName("nome").HasMaxLength(200).IsRequired();
        builder.Property(t => t.ConteudoHtml).HasColumnName("conteudo_html").HasColumnType("LONGTEXT").IsRequired();
        builder.Property(t => t.Versao).HasColumnName("versao");
        builder.Property(t => t.Ativo).HasColumnName("ativo");
        builder.Property(t => t.CriadoPorUsuarioId).HasColumnName("criado_por_usuario_id");
        builder.Property(t => t.DataCriacao).HasColumnName("data_criacao");

        builder.HasIndex(t => new { t.EscolaId, t.Versao }).IsUnique().HasDatabaseName("uk_ct_escola_versao");
        builder.HasIndex(t => new { t.EscolaId, t.Ativo }).HasDatabaseName("idx_ct_ativo");
    }
}
