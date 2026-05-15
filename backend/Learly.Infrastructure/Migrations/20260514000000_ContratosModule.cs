using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Learly.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ContratosModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "contratos_templates",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    escola_id = table.Column<int>(type: "int", nullable: false),
                    nome = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    conteudo_html = table.Column<string>(type: "LONGTEXT", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    versao = table.Column<int>(type: "int", nullable: false),
                    ativo = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    criado_por_usuario_id = table.Column<int>(type: "int", nullable: false),
                    data_criacao = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_contratos_templates", x => x.id);
                    table.ForeignKey(
                        name: "FK_contratos_templates_escolas_escola_id",
                        column: x => x.escola_id,
                        principalTable: "escolas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_contratos_templates_usuarios_criado_por_usuario_id",
                        column: x => x.criado_por_usuario_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "contratos_gerados",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    escola_id = table.Column<int>(type: "int", nullable: false),
                    pre_aluno_id = table.Column<int>(type: "int", nullable: false),
                    template_id = table.Column<int>(type: "int", nullable: false),
                    conteudo_gerado_html = table.Column<string>(type: "LONGTEXT", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    gerado_por_usuario_id = table.Column<int>(type: "int", nullable: false),
                    data_geracao = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_contratos_gerados", x => x.id);
                    table.ForeignKey(
                        name: "FK_contratos_gerados_escolas_escola_id",
                        column: x => x.escola_id,
                        principalTable: "escolas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_contratos_gerados_pre_alunos_pre_aluno_id",
                        column: x => x.pre_aluno_id,
                        principalTable: "pre_alunos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_contratos_gerados_contratos_templates_template_id",
                        column: x => x.template_id,
                        principalTable: "contratos_templates",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_contratos_gerados_usuarios_gerado_por_usuario_id",
                        column: x => x.gerado_por_usuario_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "uk_ct_escola_versao",
                table: "contratos_templates",
                columns: new[] { "escola_id", "versao" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_ct_ativo",
                table: "contratos_templates",
                columns: new[] { "escola_id", "ativo" });

            migrationBuilder.CreateIndex(
                name: "idx_cg_escola",
                table: "contratos_gerados",
                column: "escola_id");

            migrationBuilder.CreateIndex(
                name: "idx_cg_pre_aluno",
                table: "contratos_gerados",
                column: "pre_aluno_id");

            migrationBuilder.CreateIndex(
                name: "idx_cg_template",
                table: "contratos_gerados",
                column: "template_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "contratos_gerados");
            migrationBuilder.DropTable(name: "contratos_templates");
        }
    }
}
