using Learly.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Learly.Infrastructure.Data.Configurations;

internal sealed class ContratoGeradoConfiguration : IEntityTypeConfiguration<ContratoGerado>
{
    public void Configure(EntityTypeBuilder<ContratoGerado> builder)
    {
        builder.ToTable("contratos_gerados");
        builder.HasKey(g => g.Id);
        builder.Property(g => g.Id).HasColumnName("id").ValueGeneratedOnAdd();
        builder.Property(g => g.EscolaId).HasColumnName("escola_id");
        builder.Property(g => g.PreAlunoId).HasColumnName("pre_aluno_id");
        builder.Property(g => g.TemplateId).HasColumnName("template_id");
        builder.Property(g => g.ConteudoGeradoHtml).HasColumnName("conteudo_gerado_html").HasColumnType("LONGTEXT").IsRequired();
        builder.Property(g => g.GeradoPorUsuarioId).HasColumnName("gerado_por_usuario_id");
        builder.Property(g => g.DataGeracao).HasColumnName("data_geracao");

        builder.HasIndex(g => g.EscolaId).HasDatabaseName("idx_cg_escola");
        builder.HasIndex(g => g.PreAlunoId).HasDatabaseName("idx_cg_pre_aluno");
        builder.HasIndex(g => g.TemplateId).HasDatabaseName("idx_cg_template");
    }
}
