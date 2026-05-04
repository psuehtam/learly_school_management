using System.Data;
using System.Data.Common;
using Learly.Domain.Entities;
using Learly.Domain.Interfaces.Repositories;
using Learly.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Learly.Infrastructure.Repositories;

internal sealed class MatriculaRepository(LearlyDbContext db) : RepositoryBase<Matricula, int>(db), IMatriculaRepository
{
    public Task<Matricula?> ObterPorIdEEscolaAsync(int matriculaId, int escolaId, CancellationToken cancellationToken = default)
    {
        return Db.Matriculas
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == matriculaId && m.EscolaId == escolaId, cancellationToken);
    }

    public Task<Matricula?> ObterRastreadaPorIdEEscolaAsync(int matriculaId, int escolaId, CancellationToken cancellationToken = default)
    {
        return Db.Matriculas
            .FirstOrDefaultAsync(m => m.Id == matriculaId && m.EscolaId == escolaId, cancellationToken);
    }

    public Task<bool> ExisteAlunoNaEscolaAsync(int alunoId, int escolaId, CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT COUNT(1) FROM alunos WHERE id = @alunoId AND escola_id = @escolaId";
        return ExecutarExistsSqlAsync(sql, (cmd) =>
        {
            AddParameter(cmd, "@alunoId", alunoId);
            AddParameter(cmd, "@escolaId", escolaId);
        }, cancellationToken);
    }

    public Task<bool> ExisteDuplicidadeAsync(int escolaId, int alunoId, int? turmaId, CancellationToken cancellationToken = default)
    {
        return Db.Matriculas.AsNoTracking().AnyAsync(
            m => m.EscolaId == escolaId
                 && m.AlunoId == alunoId
                 && m.TurmaId == turmaId,
            cancellationToken);
    }

    public Task<bool> ExisteMatriculaEmEsperaSemTurmaAsync(int escolaId, int alunoId, CancellationToken cancellationToken = default)
    {
        return Db.Matriculas.AsNoTracking().AnyAsync(
            m => m.EscolaId == escolaId
                 && m.AlunoId == alunoId
                 && m.TurmaId == null
                 && m.Status == Matricula.Estados.EmEspera,
            cancellationToken);
    }

    public async Task<IReadOnlyList<Matricula>> ListarPorEscolaComFiltrosAsync(
        int escolaId,
        string? status,
        int? alunoId,
        int? turmaId,
        CancellationToken cancellationToken = default)
    {
        var query = Db.Matriculas.AsNoTracking().Where(m => m.EscolaId == escolaId);

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(m => m.Status == status);
        }

        if (alunoId.HasValue)
        {
            query = query.Where(m => m.AlunoId == alunoId.Value);
        }

        if (turmaId.HasValue)
        {
            query = query.Where(m => m.TurmaId == turmaId.Value);
        }

        return await query
            .OrderByDescending(m => m.DataMatricula)
            .ThenBy(m => m.Id)
            .ToListAsync(cancellationToken);
    }

    private async Task<bool> ExecutarExistsSqlAsync(string sql, Action<DbCommand> configurar, CancellationToken cancellationToken)
    {
        var connection = Db.Database.GetDbConnection();
        var shouldClose = connection.State == ConnectionState.Closed;

        if (shouldClose)
        {
            await connection.OpenAsync(cancellationToken);
        }

        try
        {
            using var command = connection.CreateCommand();
            command.CommandText = sql;
            configurar(command);

            var scalar = await command.ExecuteScalarAsync(cancellationToken);
            var total = scalar is null || scalar == DBNull.Value ? 0 : Convert.ToInt32(scalar);
            return total > 0;
        }
        finally
        {
            if (shouldClose)
            {
                await connection.CloseAsync();
            }
        }
    }

    private static void AddParameter(DbCommand command, string name, object value)
    {
        var p = command.CreateParameter();
        p.ParameterName = name;
        p.Value = value;
        command.Parameters.Add(p);
    }
}
