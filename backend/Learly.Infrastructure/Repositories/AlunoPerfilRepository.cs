using System.Data;
using System.Data.Common;
using Learly.Domain.Interfaces.Repositories;
using Learly.Domain.ReadModels;
using Learly.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Learly.Infrastructure.Repositories;

internal sealed class AlunoPerfilRepository(LearlyDbContext db) : IAlunoPerfilRepository
{
    public async Task<IReadOnlyList<AlunoOcorrenciaItem>> ListarOcorrenciasAsync(
        int escolaId,
        int alunoId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                o.id,
                o.tipo,
                o.data_ocorrencia,
                o.hora_ocorrencia,
                o.descricao,
                o.resolucao,
                u.nome_completo,
                COALESCE(a.conteudo_dado, '') AS aula_titulo
            FROM ocorrencias o
            INNER JOIN usuarios u ON u.id = o.usuario_id AND u.escola_id = o.escola_id
            LEFT JOIN aulas a ON a.id = o.aula_id AND a.escola_id = o.escola_id
            WHERE o.escola_id = @escolaId AND o.aluno_id = @alunoId
            ORDER BY o.data_ocorrencia DESC, o.hora_ocorrencia DESC, o.id DESC
            """;

        var itens = new List<AlunoOcorrenciaItem>();
        await using var reader = await ExecuteReaderAsync(sql, command =>
        {
            AddParameter(command, "@escolaId", escolaId);
            AddParameter(command, "@alunoId", alunoId);
        }, cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            var data = reader.GetDateTime(reader.GetOrdinal("data_ocorrencia"));
            var horaRaw = reader.GetValue(reader.GetOrdinal("hora_ocorrencia"));
            var hora = horaRaw switch
            {
                TimeSpan ts => TimeOnly.FromTimeSpan(ts),
                DateTime dt => TimeOnly.FromDateTime(dt),
                _ => TimeOnly.MinValue
            };

            itens.Add(new AlunoOcorrenciaItem(
                reader.GetInt32(reader.GetOrdinal("id")),
                reader.GetString(reader.GetOrdinal("tipo")),
                DateOnly.FromDateTime(data),
                hora,
                reader.GetString(reader.GetOrdinal("descricao")),
                reader.IsDBNull(reader.GetOrdinal("resolucao"))
                    ? null
                    : reader.GetString(reader.GetOrdinal("resolucao")),
                reader.GetString(reader.GetOrdinal("nome_completo")),
                reader.IsDBNull(reader.GetOrdinal("aula_titulo"))
                    ? null
                    : reader.GetString(reader.GetOrdinal("aula_titulo"))));
        }

        return itens;
    }

    public async Task<int> CriarOcorrenciaAsync(
        int escolaId,
        int alunoId,
        int usuarioId,
        string tipo,
        DateOnly dataOcorrencia,
        TimeOnly horaOcorrencia,
        string descricao,
        string? resolucao,
        int? aulaId,
        CancellationToken cancellationToken = default)
    {
        const string insertSql = """
            INSERT INTO ocorrencias
                (escola_id, aluno_id, aula_id, tipo, data_ocorrencia, hora_ocorrencia, descricao, resolucao, usuario_id)
            VALUES
                (@escolaId, @alunoId, @aulaId, @tipo, @dataOcorrencia, @horaOcorrencia, @descricao, @resolucao, @usuarioId);
            """;
        const string lastIdSql = "SELECT LAST_INSERT_ID();";

        var connection = db.Database.GetDbConnection();
        var shouldClose = connection.State == ConnectionState.Closed;

        if (shouldClose)
            await connection.OpenAsync(cancellationToken);

        try
        {
            using var command = connection.CreateCommand();
            command.CommandText = insertSql;
            SetCurrentTransaction(command);
            AddParameter(command, "@escolaId", escolaId);
            AddParameter(command, "@alunoId", alunoId);
            AddParameter(command, "@aulaId", aulaId ?? (object)DBNull.Value);
            AddParameter(command, "@tipo", tipo);
            AddParameter(command, "@dataOcorrencia", dataOcorrencia.ToString("yyyy-MM-dd"));
            AddParameter(command, "@horaOcorrencia", horaOcorrencia.ToString("HH:mm:ss"));
            AddParameter(command, "@descricao", descricao);
            AddParameter(command, "@resolucao", resolucao ?? (object)DBNull.Value);
            AddParameter(command, "@usuarioId", usuarioId);
            await command.ExecuteNonQueryAsync(cancellationToken);

            using var commandLastId = connection.CreateCommand();
            commandLastId.CommandText = lastIdSql;
            SetCurrentTransaction(commandLastId);
            var scalar = await commandLastId.ExecuteScalarAsync(cancellationToken);
            return Convert.ToInt32(scalar);
        }
        finally
        {
            if (shouldClose)
                await connection.CloseAsync();
        }
    }

    public async Task<bool> AtualizarOcorrenciaAsync(
        int escolaId,
        int alunoId,
        int ocorrenciaId,
        string tipo,
        DateOnly dataOcorrencia,
        TimeOnly horaOcorrencia,
        string descricao,
        string? resolucao,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE ocorrencias
            SET tipo = @tipo,
                data_ocorrencia = @dataOcorrencia,
                hora_ocorrencia = @horaOcorrencia,
                descricao = @descricao,
                resolucao = @resolucao
            WHERE id = @ocorrenciaId AND escola_id = @escolaId AND aluno_id = @alunoId
            """;

        var linhas = await ExecuteNonQueryAsync(sql, command =>
        {
            AddParameter(command, "@tipo", tipo);
            AddParameter(command, "@dataOcorrencia", dataOcorrencia.ToString("yyyy-MM-dd"));
            AddParameter(command, "@horaOcorrencia", horaOcorrencia.ToString("HH:mm:ss"));
            AddParameter(command, "@descricao", descricao);
            AddParameter(command, "@resolucao", resolucao ?? (object)DBNull.Value);
            AddParameter(command, "@ocorrenciaId", ocorrenciaId);
            AddParameter(command, "@escolaId", escolaId);
            AddParameter(command, "@alunoId", alunoId);
        }, cancellationToken);

        return linhas > 0;
    }

    public async Task<IReadOnlyList<AlunoFaltaItem>> ListarFaltasAsync(
        int escolaId,
        int alunoId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                p.id,
                p.status_presenca,
                a.data_aula,
                t.nome AS turma_nome,
                COALESCE(NULLIF(TRIM(a.conteudo_dado), ''), CONCAT('Aula ', a.numero_aula)) AS aula_titulo
            FROM presencas p
            INNER JOIN aulas a ON a.id = p.aula_id AND a.escola_id = p.escola_id
            INNER JOIN turmas t ON t.id = a.turma_id AND t.escola_id = p.escola_id
            WHERE p.escola_id = @escolaId
              AND p.aluno_id = @alunoId
              AND p.status_presenca IN ('F', 'FJ')
            ORDER BY a.data_aula DESC, a.horario_inicio DESC, p.id DESC
            """;

        var itens = new List<AlunoFaltaItem>();
        await using var reader = await ExecuteReaderAsync(sql, command =>
        {
            AddParameter(command, "@escolaId", escolaId);
            AddParameter(command, "@alunoId", alunoId);
        }, cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            var data = reader.GetDateTime(reader.GetOrdinal("data_aula"));
            itens.Add(new AlunoFaltaItem(
                reader.GetInt32(reader.GetOrdinal("id")),
                reader.GetString(reader.GetOrdinal("status_presenca")),
                DateOnly.FromDateTime(data),
                reader.GetString(reader.GetOrdinal("turma_nome")),
                reader.GetString(reader.GetOrdinal("aula_titulo"))));
        }

        return itens;
    }

    public async Task<bool> JustificarFaltaAsync(
        int escolaId,
        int alunoId,
        int presencaId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE presencas
            SET status_presenca = 'FJ'
            WHERE id = @presencaId
              AND escola_id = @escolaId
              AND aluno_id = @alunoId
              AND status_presenca = 'F'
            """;

        var linhas = await ExecuteNonQueryAsync(sql, command =>
        {
            AddParameter(command, "@presencaId", presencaId);
            AddParameter(command, "@escolaId", escolaId);
            AddParameter(command, "@alunoId", alunoId);
        }, cancellationToken);

        return linhas > 0;
    }

    private async Task<DbDataReader> ExecuteReaderAsync(
        string sql,
        Action<DbCommand> configurar,
        CancellationToken cancellationToken)
    {
        var connection = db.Database.GetDbConnection();
        var shouldClose = connection.State == ConnectionState.Closed;

        if (shouldClose)
            await connection.OpenAsync(cancellationToken);

        var command = connection.CreateCommand();
        command.CommandText = sql;
        SetCurrentTransaction(command);
        configurar(command);

        var reader = await command.ExecuteReaderAsync(
            shouldClose ? CommandBehavior.CloseConnection : CommandBehavior.Default,
            cancellationToken);

        return reader;
    }

    private async Task<int> ExecuteNonQueryAsync(
        string sql,
        Action<DbCommand> configurar,
        CancellationToken cancellationToken)
    {
        var connection = db.Database.GetDbConnection();
        var shouldClose = connection.State == ConnectionState.Closed;

        if (shouldClose)
            await connection.OpenAsync(cancellationToken);

        try
        {
            using var command = connection.CreateCommand();
            command.CommandText = sql;
            SetCurrentTransaction(command);
            configurar(command);
            return await command.ExecuteNonQueryAsync(cancellationToken);
        }
        finally
        {
            if (shouldClose)
                await connection.CloseAsync();
        }
    }

    private static void AddParameter(DbCommand command, string name, object value)
    {
        var p = command.CreateParameter();
        p.ParameterName = name;
        p.Value = value;
        command.Parameters.Add(p);
    }

    private void SetCurrentTransaction(DbCommand command)
    {
        IDbContextTransaction? currentTx = db.Database.CurrentTransaction;
        if (currentTx is not null)
            command.Transaction = currentTx.GetDbTransaction();
    }
}
