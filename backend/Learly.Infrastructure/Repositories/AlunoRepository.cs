using System.Data;
using System.Data.Common;
using Learly.Domain.Entities;
using Learly.Domain.Interfaces.Repositories;
using Learly.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Learly.Infrastructure.Repositories;

internal sealed class AlunoRepository(LearlyDbContext db) : RepositoryBase<Aluno, int>(db), IAlunoRepository
{
    public Task<Aluno?> ObterPorIdEEscolaAsync(int alunoId, int escolaId, CancellationToken cancellationToken = default)
    {
        return Db.Alunos.AsNoTracking().FirstOrDefaultAsync(a => a.Id == alunoId && a.EscolaId == escolaId, cancellationToken);
    }

    public Task<bool> ExisteCpfNaEscolaAsync(int escolaId, string cpf, CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT COUNT(1) FROM alunos WHERE escola_id = @escolaId AND cpf = @cpf";
        return ExecutarExistsSqlAsync(sql, cmd =>
        {
            AddParameter(cmd, "@escolaId", escolaId);
            AddParameter(cmd, "@cpf", cpf);
        }, cancellationToken);
    }

    public Task<bool> ExisteResponsavelNaEscolaAsync(int escolaId, int responsavelId, CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT COUNT(1) FROM responsaveis WHERE escola_id = @escolaId AND id = @responsavelId";
        return ExecutarExistsSqlAsync(sql, cmd =>
        {
            AddParameter(cmd, "@escolaId", escolaId);
            AddParameter(cmd, "@responsavelId", responsavelId);
        }, cancellationToken);
    }

    public async Task<int?> ObterResponsavelIdPorCpfAsync(int escolaId, string cpfCnpj, CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT id FROM responsaveis WHERE escola_id = @escolaId AND cpf_cnpj = @cpfCnpj LIMIT 1";
        var connection = Db.Database.GetDbConnection();
        var shouldClose = connection.State == ConnectionState.Closed;

        if (shouldClose)
            await connection.OpenAsync(cancellationToken);

        try
        {
            using var command = connection.CreateCommand();
            command.CommandText = sql;
            SetCurrentTransaction(command);
            AddParameter(command, "@escolaId", escolaId);
            AddParameter(command, "@cpfCnpj", cpfCnpj);

            var scalar = await command.ExecuteScalarAsync(cancellationToken);
            if (scalar is null || scalar == DBNull.Value) return null;
            return Convert.ToInt32(scalar);
        }
        finally
        {
            if (shouldClose)
                await connection.CloseAsync();
        }
    }

    public async Task<int> CriarResponsavelMinimoAsync(
        int escolaId,
        string tipoPessoa,
        string cpfCnpj,
        string nome,
        string sobrenome,
        CancellationToken cancellationToken = default)
    {
        const string insertSql = """
            INSERT INTO responsaveis
                (escola_id, tipo_pessoa, cpf_cnpj, nome, sobrenome, status)
            VALUES
                (@escolaId, @tipoPessoa, @cpfCnpj, @nome, @sobrenome, 'Ativo');
            """;
        const string lastIdSql = "SELECT LAST_INSERT_ID();";

        var connection = Db.Database.GetDbConnection();
        var shouldClose = connection.State == ConnectionState.Closed;

        if (shouldClose)
            await connection.OpenAsync(cancellationToken);

        try
        {
            using var command = connection.CreateCommand();
            command.CommandText = insertSql;
            SetCurrentTransaction(command);
            AddParameter(command, "@escolaId", escolaId);
            AddParameter(command, "@tipoPessoa", tipoPessoa);
            AddParameter(command, "@cpfCnpj", cpfCnpj);
            AddParameter(command, "@nome", nome);
            AddParameter(command, "@sobrenome", sobrenome);
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

    public async Task<int> CriarResponsavelFisicoAsync(
        int escolaId,
        string cpf,
        string nome,
        string sobrenome,
        string sexo,
        string cep,
        string tipoLogradouro,
        string logradouro,
        string numero,
        string? complemento,
        string bairro,
        string municipio,
        CancellationToken cancellationToken = default)
    {
        const string insertSql = """
            INSERT INTO responsaveis
                (escola_id, tipo_pessoa, cpf_cnpj, nome, sobrenome, sexo, cep, tipo_logradouro, logradouro, numero, complemento, bairro, municipio, status)
            VALUES
                (@escolaId, 'Fisica', @cpf, @nome, @sobrenome, @sexo, @cep, @tipoLogradouro, @logradouro, @numero, @complemento, @bairro, @municipio, 'Ativo');
            """;
        const string lastIdSql = "SELECT LAST_INSERT_ID();";

        var connection = Db.Database.GetDbConnection();
        var shouldClose = connection.State == ConnectionState.Closed;

        if (shouldClose)
            await connection.OpenAsync(cancellationToken);

        try
        {
            using var command = connection.CreateCommand();
            command.CommandText = insertSql;
            SetCurrentTransaction(command);
            AddParameter(command, "@escolaId", escolaId);
            AddParameter(command, "@cpf", cpf);
            AddParameter(command, "@nome", nome);
            AddParameter(command, "@sobrenome", sobrenome);
            AddParameter(command, "@sexo", sexo);
            AddParameter(command, "@cep", cep);
            AddParameter(command, "@tipoLogradouro", tipoLogradouro);
            AddParameter(command, "@logradouro", logradouro);
            AddParameter(command, "@numero", numero);
            AddParameter(command, "@complemento", complemento ?? (object)DBNull.Value);
            AddParameter(command, "@bairro", bairro);
            AddParameter(command, "@municipio", municipio);
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

    public async Task InserirContatoTelefoneAsync(
        int escolaId,
        string entidade,
        int entidadeId,
        string tipo,
        string numero,
        bool principal,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO contatos_telefone (escola_id, entidade, entidade_id, tipo, numero, principal)
            VALUES (@escolaId, @entidade, @entidadeId, @tipo, @numero, @principal);
            """;

        var connection = Db.Database.GetDbConnection();
        var shouldClose = connection.State == ConnectionState.Closed;

        if (shouldClose)
            await connection.OpenAsync(cancellationToken);

        try
        {
            using var command = connection.CreateCommand();
            command.CommandText = sql;
            SetCurrentTransaction(command);
            AddParameter(command, "@escolaId", escolaId);
            AddParameter(command, "@entidade", entidade);
            AddParameter(command, "@entidadeId", entidadeId);
            AddParameter(command, "@tipo", tipo);
            AddParameter(command, "@numero", numero);
            AddParameter(command, "@principal", principal);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
        finally
        {
            if (shouldClose)
                await connection.CloseAsync();
        }
    }

    private async Task<bool> ExecutarExistsSqlAsync(string sql, Action<DbCommand> configurar, CancellationToken cancellationToken)
    {
        var connection = Db.Database.GetDbConnection();
        var shouldClose = connection.State == ConnectionState.Closed;

        if (shouldClose)
            await connection.OpenAsync(cancellationToken);

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
        IDbContextTransaction? currentTx = Db.Database.CurrentTransaction;
        if (currentTx is not null)
        {
            command.Transaction = currentTx.GetDbTransaction();
        }
    }
}
