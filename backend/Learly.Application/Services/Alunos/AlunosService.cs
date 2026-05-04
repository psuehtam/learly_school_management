using Learly.Application.Contracts.Alunos.Requests;
using Learly.Application.Contracts.Alunos.Responses;
using Learly.Application.Services.Common;
using Learly.Domain.Entities;
using Learly.Domain.Interfaces.Persistence;
using Learly.Domain.Interfaces.Repositories;

namespace Learly.Application.Services.Alunos;

public sealed class AlunosService : IAlunosService
{
    private readonly IAlunoRepository _alunos;
    private readonly IMatriculaRepository _matriculas;
    private readonly IEscolaRepository _escolas;
    private readonly IUnitOfWork _unitOfWork;

    public AlunosService(
        IAlunoRepository alunos,
        IMatriculaRepository matriculas,
        IEscolaRepository escolas,
        IUnitOfWork unitOfWork)
    {
        _alunos = alunos;
        _matriculas = matriculas;
        _escolas = escolas;
        _unitOfWork = unitOfWork;
    }

    public async Task<CriarAlunoResultado> CriarAlunoAsync(
        CriarAlunoRequest request,
        AppUserContext uc,
        CancellationToken cancellationToken = default)
    {
        var escolaId = await ObterIdEscolaAtivaPorCodigoAsync(uc.CodigoEscola, cancellationToken);
        if (!escolaId.HasValue)
            return new CriarAlunoResultado(false, null, null, "Acesso negado.", CriarAlunoFalha.AcessoNegado);

        if (request.DataNascimento == default)
            return new CriarAlunoResultado(false, null, null, "Data de nascimento e obrigatoria.", CriarAlunoFalha.Validacao);

        if (request.DataIngresso == default)
            return new CriarAlunoResultado(false, null, null, "Data de ingresso e obrigatoria.", CriarAlunoFalha.Validacao);

        if (request.DataNascimento > DateOnly.FromDateTime(DateTime.UtcNow))
            return new CriarAlunoResultado(false, null, null, "Data de nascimento invalida.", CriarAlunoFalha.Validacao);

        if (string.IsNullOrWhiteSpace(request.Nome) || string.IsNullOrWhiteSpace(request.Sobrenome))
            return new CriarAlunoResultado(false, null, null, "Nome e sobrenome sao obrigatorios.", CriarAlunoFalha.Validacao);

        if (string.IsNullOrWhiteSpace(request.Sexo))
            return new CriarAlunoResultado(false, null, null, "Sexo e obrigatorio.", CriarAlunoFalha.Validacao);

        if (!EnderecoAlunoValido(request))
            return new CriarAlunoResultado(false, null, null, "Endereco do aluno incompleto. Preencha CEP, tipo/logradouro, numero, bairro e municipio.", CriarAlunoFalha.Validacao);

        var cpf = string.IsNullOrWhiteSpace(request.Cpf) ? null : request.Cpf.Trim();
        if (!string.IsNullOrWhiteSpace(cpf))
        {
            var cpfDuplicado = await _alunos.ExisteCpfNaEscolaAsync(escolaId.Value, cpf, cancellationToken);
            if (cpfDuplicado)
                return new CriarAlunoResultado(false, null, null, "Ja existe aluno com este CPF nesta escola.", CriarAlunoFalha.Conflito);
        }

        int responsavelIdResolvido;
        if (request.EProprioResponsavel)
        {
            if (string.IsNullOrWhiteSpace(cpf))
                return new CriarAlunoResultado(false, null, null, "CPF e obrigatorio quando o aluno e o proprio responsavel.", CriarAlunoFalha.Validacao);

            var responsavelExistente = await _alunos.ObterResponsavelIdPorCpfAsync(escolaId.Value, cpf, cancellationToken);
            responsavelIdResolvido = responsavelExistente
                ?? await _alunos.CriarResponsavelFisicoAsync(
                    escolaId.Value,
                    cpf,
                    request.Nome.Trim(),
                    request.Sobrenome.Trim(),
                    NormalizarSexo(request.Sexo),
                    request.Cep.Trim(),
                    request.TipoLogradouro.Trim(),
                    request.Logradouro.Trim(),
                    request.Numero.Trim(),
                    string.IsNullOrWhiteSpace(request.Complemento) ? null : request.Complemento.Trim(),
                    request.Bairro.Trim(),
                    request.Municipio.Trim(),
                    cancellationToken);
        }
        else
        {
            var responsavelCpf = request.ResponsavelCpf?.Trim();
            var responsavelNome = request.ResponsavelNome?.Trim();
            var responsavelSobrenome = request.ResponsavelSobrenome?.Trim();
            var responsavelCep = request.ResponsavelCep?.Trim();
            var responsavelTipoLogradouro = request.ResponsavelTipoLogradouro?.Trim();
            var responsavelLogradouro = request.ResponsavelLogradouro?.Trim();
            var responsavelNumero = request.ResponsavelNumero?.Trim();
            var responsavelBairro = request.ResponsavelBairro?.Trim();
            var responsavelMunicipio = request.ResponsavelMunicipio?.Trim();
            if (string.IsNullOrWhiteSpace(responsavelCpf)
                || string.IsNullOrWhiteSpace(responsavelNome)
                || string.IsNullOrWhiteSpace(responsavelSobrenome))
            {
                return new CriarAlunoResultado(
                    false,
                    null,
                    null,
                    "Dados do responsavel (nome, sobrenome e CPF) sao obrigatorios para aluno menor de idade.",
                    CriarAlunoFalha.Validacao);
            }

            if (string.IsNullOrWhiteSpace(responsavelCep)
                || string.IsNullOrWhiteSpace(responsavelTipoLogradouro)
                || string.IsNullOrWhiteSpace(responsavelLogradouro)
                || string.IsNullOrWhiteSpace(responsavelNumero)
                || string.IsNullOrWhiteSpace(responsavelBairro)
                || string.IsNullOrWhiteSpace(responsavelMunicipio))
            {
                return new CriarAlunoResultado(
                    false,
                    null,
                    null,
                    "Endereco do responsavel incompleto. Preencha CEP, tipo/logradouro, numero, bairro e municipio.",
                    CriarAlunoFalha.Validacao);
            }

            var responsavelExistente = await _alunos.ObterResponsavelIdPorCpfAsync(escolaId.Value, responsavelCpf, cancellationToken);
            responsavelIdResolvido = responsavelExistente
                ?? await _alunos.CriarResponsavelFisicoAsync(
                    escolaId.Value,
                    responsavelCpf,
                    responsavelNome,
                    responsavelSobrenome,
                    NormalizarSexo(request.ResponsavelSexo),
                    responsavelCep,
                    responsavelTipoLogradouro,
                    responsavelLogradouro,
                    responsavelNumero,
                    string.IsNullOrWhiteSpace(request.ResponsavelComplemento) ? null : request.ResponsavelComplemento.Trim(),
                    responsavelBairro,
                    responsavelMunicipio,
                    cancellationToken);
        }

        int alunoId = 0;
        int matriculaId = 0;

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var agoraUtc = DateTime.UtcNow;

            var aluno = new Aluno
            {
                EscolaId = escolaId.Value,
                ResponsavelId = responsavelIdResolvido,
                EProprioResponsavel = request.EProprioResponsavel,
                Nome = request.Nome,
                Sobrenome = request.Sobrenome,
                Sexo = request.Sexo,
                DataNascimento = request.DataNascimento,
                DataIngresso = request.DataIngresso,
                Cpf = cpf,
                Cep = request.Cep.Trim(),
                TipoLogradouro = request.TipoLogradouro.Trim(),
                Logradouro = request.Logradouro.Trim(),
                Numero = request.Numero.Trim(),
                Complemento = string.IsNullOrWhiteSpace(request.Complemento) ? null : request.Complemento.Trim(),
                Bairro = request.Bairro.Trim(),
                Municipio = request.Municipio.Trim(),
                Status = Aluno.Estados.Ativo,
                DataCriacao = agoraUtc,
                DataAtualizacao = agoraUtc
            };

            _alunos.Adicionar(aluno);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            alunoId = aluno.Id;

            var matricula = new Matricula
            {
                EscolaId = escolaId.Value,
                AlunoId = aluno.Id,
                TurmaId = null,
                DataMatricula = request.DataIngresso,
                Status = Matricula.Estados.EmEspera,
                DataCriacao = agoraUtc,
                DataAtualizacao = agoraUtc
            };

            _matriculas.Adicionar(matricula);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            matriculaId = matricula.Id;
        }, cancellationToken);

        return new CriarAlunoResultado(true, alunoId, matriculaId, null, CriarAlunoFalha.Nenhuma);
    }

    private Task<int?> ObterIdEscolaAtivaPorCodigoAsync(string? codigoEscola, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(codigoEscola))
            return Task.FromResult<int?>(null);

        return _escolas.ObterIdAtivaPorCodigoEscolaAsync(codigoEscola, cancellationToken);
    }

    private static string NormalizarSexo(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "Outro";

        var s = value.Trim();
        if (s.Equals("Masculino", StringComparison.OrdinalIgnoreCase)) return "Masculino";
        if (s.Equals("Feminino", StringComparison.OrdinalIgnoreCase)) return "Feminino";
        return "Outro";
    }

    private static bool EnderecoAlunoValido(CriarAlunoRequest request)
    {
        return !string.IsNullOrWhiteSpace(request.Cep)
               && !string.IsNullOrWhiteSpace(request.TipoLogradouro)
               && !string.IsNullOrWhiteSpace(request.Logradouro)
               && !string.IsNullOrWhiteSpace(request.Numero)
               && !string.IsNullOrWhiteSpace(request.Bairro)
               && !string.IsNullOrWhiteSpace(request.Municipio);
    }
}
