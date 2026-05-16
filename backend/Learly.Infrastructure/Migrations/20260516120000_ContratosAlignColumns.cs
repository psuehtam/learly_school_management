using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Learly.Infrastructure.Migrations;

/// <summary>
/// Alinha colunas de contratos quando o banco foi criado por setup.sql legado (template / conteudo_gerado).
/// </summary>
public partial class ContratosAlignColumns : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            SET @db = DATABASE();

            SET @has_nome = (
              SELECT COUNT(*) FROM information_schema.COLUMNS
              WHERE TABLE_SCHEMA = @db AND TABLE_NAME = 'contratos_templates' AND COLUMN_NAME = 'nome'
            );
            SET @sql = IF(
              @has_nome = 0,
              'ALTER TABLE contratos_templates ADD COLUMN nome VARCHAR(200) NULL AFTER escola_id',
              'SELECT 1'
            );
            PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

            SET @has_html = (
              SELECT COUNT(*) FROM information_schema.COLUMNS
              WHERE TABLE_SCHEMA = @db AND TABLE_NAME = 'contratos_templates' AND COLUMN_NAME = 'conteudo_html'
            );
            SET @sql = IF(
              @has_html = 0,
              'ALTER TABLE contratos_templates ADD COLUMN conteudo_html LONGTEXT NULL AFTER nome',
              'SELECT 1'
            );
            PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

            SET @has_template = (
              SELECT COUNT(*) FROM information_schema.COLUMNS
              WHERE TABLE_SCHEMA = @db AND TABLE_NAME = 'contratos_templates' AND COLUMN_NAME = 'template'
            );
            SET @sql = IF(
              @has_template > 0,
              'UPDATE contratos_templates SET conteudo_html = template WHERE (conteudo_html IS NULL OR conteudo_html = '''') AND template IS NOT NULL',
              'SELECT 1'
            );
            PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

            UPDATE contratos_templates
            SET nome = CONCAT('Template v', versao)
            WHERE nome IS NULL OR TRIM(nome) = '';

            UPDATE contratos_templates
            SET conteudo_html = '<p></p>'
            WHERE conteudo_html IS NULL OR TRIM(conteudo_html) = '';

            ALTER TABLE contratos_templates
              MODIFY nome VARCHAR(200) NOT NULL,
              MODIFY conteudo_html LONGTEXT NOT NULL;

            SET @sql = IF(
              @has_template > 0,
              'ALTER TABLE contratos_templates DROP COLUMN template',
              'SELECT 1'
            );
            PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

            SET @has_old = (
              SELECT COUNT(*) FROM information_schema.COLUMNS
              WHERE TABLE_SCHEMA = @db AND TABLE_NAME = 'contratos_gerados' AND COLUMN_NAME = 'conteudo_gerado'
            );
            SET @has_new = (
              SELECT COUNT(*) FROM information_schema.COLUMNS
              WHERE TABLE_SCHEMA = @db AND TABLE_NAME = 'contratos_gerados' AND COLUMN_NAME = 'conteudo_gerado_html'
            );
            SET @sql = IF(
              @has_old > 0 AND @has_new = 0,
              'ALTER TABLE contratos_gerados CHANGE conteudo_gerado conteudo_gerado_html LONGTEXT NOT NULL',
              'SELECT 1'
            );
            PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Irreversível com segurança (dados legados).
    }
}
