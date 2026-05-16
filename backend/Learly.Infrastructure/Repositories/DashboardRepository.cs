using Learly.Domain.Entities;
using Learly.Domain.Interfaces.Repositories;
using Learly.Domain.ReadModels;
using Learly.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Learly.Infrastructure.Repositories;

internal sealed class DashboardRepository(LearlyDbContext db) : IDashboardRepository
{
    private sealed class ParcelaVencidaSqlRow
    {
        public int ParcelaId { get; set; }
        public string AlunoNome { get; set; } = "";
        public string TurmaNome { get; set; } = "";
        public DateOnly DataVencimento { get; set; }
        public decimal Valor { get; set; }
    }

    private sealed class AtividadeSqlRow
    {
        public string Acao { get; set; } = "";
        public string Detalhe { get; set; } = "";
        public DateTime OcorridoEm { get; set; }
        public string Tipo { get; set; } = "";
    }

    private sealed class ScalarIntRow
    {
        public int Value { get; set; }
    }

    public async Task<DashboardDadosEscola> ObterDadosEscolaAsync(
        int escolaId,
        DateOnly dataReferencia,
        CancellationToken cancellationToken = default)
    {
        var resumo = await ObterResumoAsync(escolaId, dataReferencia, cancellationToken);
        var aulasHoje = await ObterAulasHojeAsync(escolaId, dataReferencia, cancellationToken);
        var parcelasVencidas = await ObterParcelasVencidasAsync(escolaId, dataReferencia, cancellationToken);
        var atividade = await ObterAtividadeRecenteAsync(escolaId, cancellationToken);

        return new DashboardDadosEscola(resumo, aulasHoje, parcelasVencidas, atividade);
    }

    private async Task<DashboardResumoDados> ObterResumoAsync(
        int escolaId,
        DateOnly dataReferencia,
        CancellationToken cancellationToken)
    {
        var alunosAtivos = await db.Matriculas.AsNoTracking()
            .Where(m => m.EscolaId == escolaId && m.Status == Matricula.Estados.Ativo)
            .Select(m => m.AlunoId)
            .Distinct()
            .CountAsync(cancellationToken);

        var turmasAtivas = await db.Turmas.AsNoTracking()
            .CountAsync(
                t => t.EscolaId == escolaId && t.Status == Turma.Estados.EmAndamento,
                cancellationToken);

        var professores = await (
            from u in db.Usuarios.AsNoTracking()
            join p in db.Perfis.AsNoTracking() on u.PerfilId equals p.Id
            where u.EscolaId == escolaId
                  && u.Status == Usuario.Estados.Ativo
                  && p.Nome == "Professor"
            select u.Id
        ).CountAsync(cancellationToken);

        var parcelasEmAberto = await ContarParcelasEmAbertoAsync(escolaId, dataReferencia, cancellationToken);

        return new DashboardResumoDados(alunosAtivos, turmasAtivas, professores, parcelasEmAberto);
    }

    private async Task<int> ContarParcelasEmAbertoAsync(
        int escolaId,
        DateOnly dataReferencia,
        CancellationToken cancellationToken)
    {
        try
        {
            var dataStr = dataReferencia.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
            var sql =
                "SELECT COUNT(*) AS Value FROM parcelas p "
                + "WHERE p.escola_id = {0} "
                + "AND p.status IN ('Pendente','Vencido') "
                + "AND (p.status = 'Vencido' OR p.data_vencimento < {1})";

            var row = await db.Database
                .SqlQueryRaw<ScalarIntRow>(sql, escolaId, dataStr)
                .FirstOrDefaultAsync(cancellationToken);
            return row?.Value ?? 0;
        }
        catch
        {
            return 0;
        }
    }

    private async Task<IReadOnlyList<DashboardAulaHojeDados>> ObterAulasHojeAsync(
        int escolaId,
        DateOnly dataReferencia,
        CancellationToken cancellationToken)
    {
        var aulas = await (
            from a in db.Aulas.AsNoTracking()
            join t in db.Turmas.AsNoTracking() on a.TurmaId equals t.Id
            join u in db.Usuarios.AsNoTracking() on a.ProfessorId equals u.Id
            where a.EscolaId == escolaId
                  && a.DataAula == dataReferencia
                  && a.Status != Aula.Estados.Cancelada
            orderby a.HorarioInicio
            select new
            {
                a.Id,
                TurmaNome = t.Nome,
                ProfessorNome = u.NomeCompleto,
                a.HorarioInicio,
                a.TurmaId,
            }
        ).ToListAsync(cancellationToken);

        if (aulas.Count == 0)
        {
            return [];
        }

        var turmaIds = aulas.Select(a => a.TurmaId).Distinct().ToList();
        var alunosPorTurma = await db.Matriculas.AsNoTracking()
            .Where(m => m.TurmaId != null
                        && turmaIds.Contains(m.TurmaId.Value)
                        && m.Status == Matricula.Estados.Ativo)
            .GroupBy(m => m.TurmaId!.Value)
            .Select(g => new { TurmaId = g.Key, Total = g.Count() })
            .ToDictionaryAsync(x => x.TurmaId, x => x.Total, cancellationToken);

        return aulas.Select(a => new DashboardAulaHojeDados(
            a.Id,
            a.TurmaNome,
            a.ProfessorNome,
            a.HorarioInicio,
            alunosPorTurma.GetValueOrDefault(a.TurmaId, 0))).ToList();
    }

    private async Task<IReadOnlyList<DashboardParcelaVencidaDados>> ObterParcelasVencidasAsync(
        int escolaId,
        DateOnly dataReferencia,
        CancellationToken cancellationToken)
    {
        try
        {
            var dataStr = dataReferencia.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
            var sql =
                "SELECT p.id AS ParcelaId, "
                + "TRIM(CONCAT(COALESCE(al.nome,''), ' ', COALESCE(al.sobrenome,''))) AS AlunoNome, "
                + "t.nome AS TurmaNome, "
                + "p.data_vencimento AS DataVencimento, "
                + "p.valor_com_desconto AS Valor "
                + "FROM parcelas p "
                + "INNER JOIN matriculas m ON p.matricula_id = m.id "
                + "INNER JOIN alunos al ON m.aluno_id = al.id "
                + "INNER JOIN turmas t ON p.turma_id = t.id "
                + "WHERE p.escola_id = {0} "
                + "AND p.status IN ('Pendente','Vencido') "
                + "AND p.data_vencimento < {1} "
                + "ORDER BY p.data_vencimento ASC "
                + "LIMIT 10";

            var rows = await db.Database
                .SqlQueryRaw<ParcelaVencidaSqlRow>(sql, escolaId, dataStr)
                .ToListAsync(cancellationToken);

            return rows.Select(r => new DashboardParcelaVencidaDados(
                r.ParcelaId,
                r.AlunoNome.Trim(),
                r.TurmaNome.Trim(),
                r.DataVencimento,
                r.Valor)).ToList();
        }
        catch
        {
            return [];
        }
    }

    private async Task<IReadOnlyList<DashboardAtividadeDados>> ObterAtividadeRecenteAsync(
        int escolaId,
        CancellationToken cancellationToken)
    {
        var desde = DateTime.UtcNow.AddDays(-14);
        var eventos = new List<DashboardAtividadeDados>();

        var matriculas = await (
            from m in db.Matriculas.AsNoTracking()
            join a in db.Alunos.AsNoTracking() on m.AlunoId equals a.Id
            where m.EscolaId == escolaId && m.DataCriacao >= desde
            orderby m.DataCriacao descending
            select new
            {
                m.DataCriacao,
                Nome = (a.Nome + " " + a.Sobrenome).Trim(),
            }
        ).Take(8).ToListAsync(cancellationToken);

        eventos.AddRange(matriculas.Select(m => new DashboardAtividadeDados(
            "Nova matricula criada",
            m.Nome,
            m.DataCriacao,
            "matricula")));

        var calendario = await db.CalendariosGerais.AsNoTracking()
            .Where(c => c.EscolaId == escolaId && c.DataCriacao >= desde)
            .OrderByDescending(c => c.DataCriacao)
            .Take(5)
            .ToListAsync(cancellationToken);

        foreach (var c in calendario)
        {
            var acao = c.TipoEvento switch
            {
                CalendarioGeral.TiposEvento.Feriado => "Feriado marcado no calendario",
                CalendarioGeral.TiposEvento.Recesso => "Recesso marcado no calendario",
                CalendarioGeral.TiposEvento.SemAula => "Dia sem aula no calendario",
                _ => "Evento no calendario escolar",
            };
            var detalhe = c.Descricao?.Trim()
                            ?? c.DataEvento.ToString("dd/MM/yyyy", System.Globalization.CultureInfo.InvariantCulture);
            eventos.Add(new DashboardAtividadeDados(acao, detalhe, c.DataCriacao, "calendario"));
        }

        try
        {
            var atividadeSql =
                "SELECT * FROM ("
                + "SELECT 'Turma agendada' AS Acao, t.nome AS Detalhe, t.data_atualizacao AS OcorridoEm, 'turma' AS Tipo "
                + "FROM turmas t WHERE t.escola_id = {0} AND t.status = 'Em Andamento' AND t.data_atualizacao >= {1} "
                + "UNION ALL "
                + "SELECT 'Novo usuario cadastrado' AS Acao, "
                + "CONCAT(u.nome_completo, ' (', p.nome, ')') AS Detalhe, "
                + "u.data_criacao AS OcorridoEm, 'usuario' AS Tipo "
                + "FROM usuarios u INNER JOIN perfis p ON u.perfil_id = p.id "
                + "WHERE u.escola_id = {0} AND u.data_criacao >= {1} "
                + "UNION ALL "
                + "SELECT 'Parcela baixada' AS Acao, "
                + "CONCAT(TRIM(CONCAT(COALESCE(al.nome,''), ' ', COALESCE(al.sobrenome,''))), ' — R$ ', "
                + "CAST(ROUND(p.valor_pago, 2) AS CHAR)) AS Detalhe, "
                + "CAST(p.data_pagamento AS DATETIME) AS OcorridoEm, 'financeiro' AS Tipo "
                + "FROM parcelas p "
                + "INNER JOIN matriculas m ON p.matricula_id = m.id "
                + "INNER JOIN alunos al ON m.aluno_id = al.id "
                + "WHERE p.escola_id = {0} AND p.status = 'Pago' AND p.data_pagamento IS NOT NULL "
                + "AND p.data_ultima_modificacao >= {1}"
                + ") sub ORDER BY OcorridoEm DESC LIMIT 15";

            var sqlRows = await db.Database
                .SqlQueryRaw<AtividadeSqlRow>(atividadeSql, escolaId, desde)
                .ToListAsync(cancellationToken);

            eventos.AddRange(sqlRows.Select(r => new DashboardAtividadeDados(
                r.Acao,
                r.Detalhe,
                r.OcorridoEm,
                r.Tipo)));
        }
        catch
        {
            /* parcelas ou colunas podem não existir em bases antigas */
        }

        return eventos
            .OrderByDescending(e => e.OcorridoEm)
            .Take(12)
            .ToList();
    }
}
