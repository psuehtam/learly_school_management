using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Learly.Infrastructure.Migrations;

/// <summary>
/// Ajusta matriculas para fluxo comercial: turma opcional (aguardando sala) e status "Em Espera".
/// Exige que a tabela matriculas já exista (ex.: database/db.sql). Ambientes criados só pelas migrations
/// anteriores deste repositório não incluem matriculas — nesse caso crie a tabela a partir de db.sql antes.
/// </summary>
public partial class MatriculasTurmaOpcionalStatusEmEspera : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            ALTER TABLE matriculas
            MODIFY COLUMN turma_id INT NULL,
            MODIFY COLUMN status ENUM('Ativo','Concluido','Trancado','Cancelado','Em Espera') NOT NULL DEFAULT 'Ativo';
            """);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            UPDATE matriculas SET status = 'Ativo' WHERE status = 'Em Espera';
            """);

        migrationBuilder.Sql(
            """
            ALTER TABLE matriculas
            MODIFY COLUMN status ENUM('Ativo','Concluido','Trancado','Cancelado') NOT NULL DEFAULT 'Ativo';
            """);

        // Só força turma_id NOT NULL se não existir matrícula sem turma (reversão sem inventar FK).
        migrationBuilder.Sql(
            """
            SET @nulls := (SELECT COUNT(*) FROM matriculas WHERE turma_id IS NULL);
            SET @sql := IF(@nulls = 0,
              'ALTER TABLE matriculas MODIFY COLUMN turma_id INT NOT NULL',
              'SELECT 1');
            PREPARE stmt FROM @sql;
            EXECUTE stmt;
            DEALLOCATE PREPARE stmt;
            """);
    }
}
