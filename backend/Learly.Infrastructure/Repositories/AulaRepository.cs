using Learly.Domain.Entities;
using Learly.Domain.Interfaces.Repositories;
using Learly.Domain.ReadModels;
using Learly.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Learly.Infrastructure.Repositories;

internal sealed class AulaRepository(LearlyDbContext db) : RepositoryBase<Aula, int>(db), IAulaRepository
{
    private sealed class AulaReposicaoSqlRow
    {
        public int AulaReposicaoId { get; set; }
        public string AlunoNomeCompleto { get; set; } = "";
        public int NumeroAulaOriginal { get; set; }
        public DateOnly DataAulaOriginal { get; set; }
    }

    public async Task<IReadOnlyDictionary<int, AulaReposicaoAgendaContexto>> ObterContextoReposicaoPorAulaIdsAsync(
        int escolaId,
        IReadOnlyList<int> aulaIdsReposicao,
        CancellationToken cancellationToken = default)
    {
        if (aulaIdsReposicao.Count == 0)
        {
            return new Dictionary<int, AulaReposicaoAgendaContexto>();
        }

        var idList = string.Join(
            ",",
            aulaIdsReposicao.Distinct().Select(i => i.ToString(System.Globalization.CultureInfo.InvariantCulture)));

        var sql =
            "SELECT p_rep.aula_id AS AulaReposicaoId, "
            + "TRIM(CONCAT(COALESCE(al.nome,''), ' ', COALESCE(al.sobrenome,''))) AS AlunoNomeCompleto, "
            + "orig.numero_aula AS NumeroAulaOriginal, orig.data_aula AS DataAulaOriginal "
            + "FROM presencas p_rep "
            + "INNER JOIN presencas p_orig ON p_rep.reposicao_de_presenca_id = p_orig.id "
            + "INNER JOIN aulas orig ON p_orig.aula_id = orig.id "
            + "INNER JOIN alunos al ON p_rep.aluno_id = al.id "
            + "WHERE p_rep.escola_id = {0} AND p_rep.reposicao_de_presenca_id IS NOT NULL "
            + "AND p_rep.aula_id IN (" + idList + ")";

        var rows = await Db.Database
            .SqlQueryRaw<AulaReposicaoSqlRow>(sql, escolaId)
            .ToListAsync(cancellationToken);

        return rows
            .GroupBy(r => r.AulaReposicaoId)
            .ToDictionary(
                g => g.Key,
                g =>
                {
                    var r = g.First();
                    return new AulaReposicaoAgendaContexto(
                        r.AlunoNomeCompleto.Trim(),
                        r.NumeroAulaOriginal,
                        r.DataAulaOriginal);
                });
    }

    public async Task<IReadOnlyList<Aula>> ListarPorTurmaAsync(int turmaId, CancellationToken cancellationToken = default)
    {
        return await Db.Aulas
            .AsNoTracking()
            .Where(a => a.TurmaId == turmaId)
            .OrderByDescending(a => a.DataAula)
            .ThenBy(a => a.HorarioInicio)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Aula>> ListarPorEscolaEFiltroProfessorAsync(
        int escolaId,
        int? filtrarProfessorId,
        CancellationToken cancellationToken = default)
    {
        var query = Db.Aulas.AsNoTracking().Where(a => a.EscolaId == escolaId);
        if (filtrarProfessorId.HasValue)
        {
            query = query.Where(a => a.ProfessorId == filtrarProfessorId.Value);
        }

        return await query
            .OrderByDescending(a => a.DataAula)
            .ThenBy(a => a.HorarioInicio)
            .ToListAsync(cancellationToken);
    }

    public async Task<Aula?> ObterSemRastreioPorIdEEscolaAsync(
        int id,
        int escolaId,
        int? filtrarProfessorId,
        CancellationToken cancellationToken = default)
    {
        var query = Db.Aulas.AsNoTracking().Where(a => a.Id == id && a.EscolaId == escolaId);
        if (filtrarProfessorId.HasValue)
        {
            query = query.Where(a => a.ProfessorId == filtrarProfessorId.Value);
        }

        return await query.FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<Aula?> ObterRastreadaPorIdEEscolaAsync(
        int id,
        int escolaId,
        CancellationToken cancellationToken = default)
    {
        return await Db.Aulas
            .Where(a => a.Id == id && a.EscolaId == escolaId)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
