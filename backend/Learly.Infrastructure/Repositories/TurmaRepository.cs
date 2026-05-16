using Learly.Domain.Entities;
using Learly.Domain.Interfaces.Repositories;
using Learly.Domain.ReadModels;
using Learly.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Learly.Infrastructure.Repositories;

internal sealed class TurmaRepository(LearlyDbContext db) : RepositoryBase<Turma, int>(db), ITurmaRepository
{
    public async Task<IReadOnlyList<TurmaListagemItem>> ListarDetalhadoPorEscolaAsync(
        int escolaId,
        string? status,
        int? professorId = null,
        CancellationToken cancellationToken = default)
    {
        var turmasQuery = Db.Turmas.AsNoTracking().Where(t => t.EscolaId == escolaId);
        if (professorId.HasValue)
        {
            turmasQuery = turmasQuery.Where(t => t.ProfessorId == professorId.Value);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            turmasQuery = turmasQuery.Where(t => t.Status == status);
        }

        var turmas = await turmasQuery.OrderBy(t => t.Nome).ToListAsync(cancellationToken);
        if (turmas.Count == 0)
        {
            return [];
        }

        var turmaIds = turmas.Select(t => t.Id).ToList();
        var professorIds = turmas.Select(t => t.ProfessorId).Distinct().ToList();
        var livroIds = turmas.Select(t => t.LivroId).Distinct().ToList();

        var professores = await Db.Usuarios.AsNoTracking()
            .Where(u => professorIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.NomeCompleto, cancellationToken);

        var livros = await Db.Livros.AsNoTracking()
            .Where(l => livroIds.Contains(l.Id))
            .ToDictionaryAsync(l => l.Id, l => l.Nome, cancellationToken);

        var diasPorTurma = await Db.TurmasDiasSemana.AsNoTracking()
            .Where(d => turmaIds.Contains(d.TurmaId))
            .GroupBy(d => d.TurmaId)
            .ToDictionaryAsync(g => g.Key, g => (IReadOnlyList<int>)g.Select(x => x.DiaSemana).OrderBy(x => x).ToList(), cancellationToken);

        var alunosPorTurma = await Db.Matriculas.AsNoTracking()
            .Where(m => m.TurmaId != null && turmaIds.Contains(m.TurmaId.Value) && m.Status == Matricula.Estados.Ativo)
            .GroupBy(m => m.TurmaId!.Value)
            .ToDictionaryAsync(g => g.Key, g => g.Count(), cancellationToken);

        return turmas.Select(t =>
        {
            professores.TryGetValue(t.ProfessorId, out var profNome);
            livros.TryGetValue(t.LivroId, out var livroNome);
            diasPorTurma.TryGetValue(t.Id, out var dias);
            alunosPorTurma.TryGetValue(t.Id, out var totalAlunos);
            return new TurmaListagemItem(
                t.Id,
                t.ProfessorId,
                profNome ?? $"Professor {t.ProfessorId}",
                t.LivroId,
                livroNome ?? $"Livro {t.LivroId}",
                t.Nome,
                t.Sala,
                t.Horario,
                t.HorarioFim,
                t.DataInicio,
                t.DataTerminoPrevista,
                t.Status,
                t.Observacoes,
                dias ?? [],
                totalAlunos);
        }).ToList();
    }

    public async Task<IReadOnlyList<Turma>> ListarPorEscolaAsync(int escolaId, CancellationToken cancellationToken = default)
    {
        return await Db.Turmas
            .AsNoTracking()
            .Where(t => t.EscolaId == escolaId)
            .OrderBy(t => t.Nome)
            .ToListAsync(cancellationToken);
    }

    public async Task<Turma?> ObterPorIdEEscolaAsync(int turmaId, int escolaId, CancellationToken cancellationToken = default)
    {
        return await Db.Turmas.AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == turmaId && t.EscolaId == escolaId, cancellationToken);
    }

    public Task<Turma?> ObterRastreadaPorIdEEscolaAsync(int turmaId, int escolaId, CancellationToken cancellationToken = default)
    {
        return Db.Turmas.FirstOrDefaultAsync(t => t.Id == turmaId && t.EscolaId == escolaId, cancellationToken);
    }

    public async Task<IReadOnlyList<int>> ListarDiasSemanaAsync(int turmaId, CancellationToken cancellationToken = default)
    {
        return await Db.TurmasDiasSemana.AsNoTracking()
            .Where(d => d.TurmaId == turmaId)
            .OrderBy(d => d.DiaSemana)
            .Select(d => d.DiaSemana)
            .ToListAsync(cancellationToken);
    }

    public async Task SubstituirDiasSemanaAsync(
        int escolaId,
        int turmaId,
        IReadOnlyList<int> diasSemana,
        CancellationToken cancellationToken = default)
    {
        var existentes = await Db.TurmasDiasSemana.Where(d => d.TurmaId == turmaId).ToListAsync(cancellationToken);
        Db.TurmasDiasSemana.RemoveRange(existentes);

        foreach (var dia in diasSemana.Distinct().OrderBy(d => d))
        {
            if (dia is < 0 or > 6)
            {
                continue;
            }

            Db.TurmasDiasSemana.Add(new TurmaDiaSemana
            {
                EscolaId = escolaId,
                TurmaId = turmaId,
                DiaSemana = dia
            });
        }
    }

    public Task<int> ContarMatriculasAtivasAsync(int turmaId, CancellationToken cancellationToken = default)
    {
        return Db.Matriculas.AsNoTracking().CountAsync(
            m => m.TurmaId == turmaId && m.Status == Matricula.Estados.Ativo,
            cancellationToken);
    }

    public Task<bool> PossuiAulasRealizadasAsync(int turmaId, CancellationToken cancellationToken = default)
    {
        return Db.Aulas.AsNoTracking().AnyAsync(
            a => a.TurmaId == turmaId && a.Status == Aula.Estados.Realizada,
            cancellationToken);
    }

    public async Task AtualizarProfessorAulasAgendadasAsync(int turmaId, int professorId, CancellationToken cancellationToken = default)
    {
        var aulas = await Db.Aulas
            .Where(a => a.TurmaId == turmaId && a.Status == Aula.Estados.Agendada)
            .ToListAsync(cancellationToken);

        foreach (var aula in aulas)
        {
            aula.ProfessorId = professorId;
        }
    }

    public Task<int> ContarTurmasPorLivroAsync(int escolaId, int livroId, CancellationToken cancellationToken = default)
    {
        return Db.Turmas.AsNoTracking().CountAsync(
            t => t.EscolaId == escolaId && t.LivroId == livroId,
            cancellationToken);
    }

    public async Task<bool> ExisteConflitoHorarioProfessorAsync(
        int escolaId,
        int professorId,
        int diaSemana,
        TimeOnly horarioInicio,
        TimeOnly horarioFim,
        int? ignorarTurmaId,
        CancellationToken cancellationToken = default)
    {
        var turmaIdsComDia = await (
            from t in Db.Turmas.AsNoTracking()
            join d in Db.TurmasDiasSemana.AsNoTracking() on t.Id equals d.TurmaId
            where t.EscolaId == escolaId
                  && t.ProfessorId == professorId
                  && t.Status == Turma.Estados.EmAndamento
                  && d.DiaSemana == diaSemana
                  && (ignorarTurmaId == null || t.Id != ignorarTurmaId)
            select t
        ).ToListAsync(cancellationToken);

        foreach (var t in turmaIdsComDia)
        {
            if (t.Horario is null || t.HorarioFim is null)
            {
                continue;
            }

            if (HorariosSobrepoem(t.Horario.Value, t.HorarioFim.Value, horarioInicio, horarioFim))
            {
                return true;
            }
        }

        return false;
    }

    public async Task<IReadOnlyList<int>> ListarIdsTurmasEmAndamentoAsync(int escolaId, CancellationToken cancellationToken = default)
    {
        return await Db.Turmas.AsNoTracking()
            .Where(t => t.EscolaId == escolaId && t.Status == Turma.Estados.EmAndamento)
            .Select(t => t.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task RemoverAulasAgendadasDaTurmaAsync(int turmaId, CancellationToken cancellationToken = default)
    {
        var aulaIds = await Db.Aulas
            .Where(a => a.TurmaId == turmaId && a.Status == Aula.Estados.Agendada)
            .Select(a => a.Id)
            .ToListAsync(cancellationToken);

        if (aulaIds.Count == 0)
        {
            return;
        }

        var presencaIdsOriginais = await Db.Presencas
            .Where(p => aulaIds.Contains(p.AulaId))
            .Select(p => p.Id)
            .ToListAsync(cancellationToken);

        if (presencaIdsOriginais.Count > 0)
        {
            var presencasReposicao = await Db.Presencas
                .Where(p => p.ReposicaoDePresencaId != null
                            && presencaIdsOriginais.Contains(p.ReposicaoDePresencaId.Value))
                .ToListAsync(cancellationToken);
            if (presencasReposicao.Count > 0)
            {
                Db.Presencas.RemoveRange(presencasReposicao);
            }
        }

        var presencas = await Db.Presencas.Where(p => aulaIds.Contains(p.AulaId)).ToListAsync(cancellationToken);
        if (presencas.Count > 0)
        {
            Db.Presencas.RemoveRange(presencas);
        }

        var homeworks = await Db.Homeworks.Where(h => aulaIds.Contains(h.AulaId)).ToListAsync(cancellationToken);
        if (homeworks.Count > 0)
        {
            Db.Homeworks.RemoveRange(homeworks);
        }

        var idList = string.Join(",", aulaIds);
        await Db.Database.ExecuteSqlRawAsync(
            $"UPDATE ocorrencias SET aula_id = NULL WHERE aula_id IN ({idList})",
            cancellationToken);
        await Db.Database.ExecuteSqlRawAsync(
            $"DELETE FROM reposicoes WHERE aula_id IN ({idList})",
            cancellationToken);

        var aulas = await Db.Aulas.Where(a => aulaIds.Contains(a.Id)).ToListAsync(cancellationToken);
        Db.Aulas.RemoveRange(aulas);
    }

    public async Task RemoverProgressoCapitulosAsync(int turmaId, CancellationToken cancellationToken = default)
    {
        var rows = await Db.TurmasCapitulosProgresso.Where(p => p.TurmaId == turmaId).ToListAsync(cancellationToken);
        Db.TurmasCapitulosProgresso.RemoveRange(rows);
    }

    public async Task InicializarProgressoCapitulosAsync(
        int escolaId,
        int turmaId,
        IReadOnlyList<int> capituloIds,
        CancellationToken cancellationToken = default)
    {
        foreach (var capId in capituloIds)
        {
            Db.TurmasCapitulosProgresso.Add(new TurmaCapituloProgresso
            {
                EscolaId = escolaId,
                TurmaId = turmaId,
                CapituloId = capId,
                Concluido = false
            });
        }

        await Task.CompletedTask;
    }

    public async Task<bool> TodosCapitulosConcluidosAsync(int turmaId, CancellationToken cancellationToken = default)
    {
        var total = await Db.TurmasCapitulosProgresso.AsNoTracking()
            .CountAsync(p => p.TurmaId == turmaId, cancellationToken);
        if (total == 0)
        {
            return false;
        }

        var pendentes = await Db.TurmasCapitulosProgresso.AsNoTracking()
            .CountAsync(p => p.TurmaId == turmaId && !p.Concluido, cancellationToken);
        return pendentes == 0;
    }

    public async Task<IReadOnlyDictionary<int, TurmaResumoAgenda>> ObterResumoParaAgendaAsync(
        int escolaId,
        IReadOnlyList<int> turmaIds,
        CancellationToken cancellationToken = default)
    {
        if (turmaIds.Count == 0)
        {
            return new Dictionary<int, TurmaResumoAgenda>();
        }

        var distinct = turmaIds.Distinct().ToList();
        var rows = await (
            from t in Db.Turmas.AsNoTracking()
            join l in Db.Livros.AsNoTracking() on t.LivroId equals l.Id into livroJoin
            from l in livroJoin.DefaultIfEmpty()
            where t.EscolaId == escolaId && distinct.Contains(t.Id)
            select new { t.Id, t.Nome, LivroNome = l != null ? l.Nome : "" }
        ).ToListAsync(cancellationToken);

        return rows.ToDictionary(r => r.Id, r => new TurmaResumoAgenda(r.Nome, r.LivroNome));
    }

    private static bool HorariosSobrepoem(TimeOnly aIni, TimeOnly aFim, TimeOnly bIni, TimeOnly bFim) =>
        aIni < bFim && bIni < aFim;
}
