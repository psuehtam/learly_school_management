using Learly.Application.Contracts.Alunos;
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

    public async Task<AlunosListagemResultado> ListarAsync(
        ListarAlunosQuery query,
        AppUserContext uc,
        CancellationToken cancellationToken = default)
    {
        var escolaId = await ObterIdEscolaAtivaPorCodigoAsync(uc.CodigoEscola, cancellationToken);
        if (!escolaId.HasValue)
        {
            return new AlunosListagemResultado(false, [], "Acesso negado.", AlunosListagemFalha.AcessoNegado);
        }

        string? statusFiltro = null;
        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            try
            {
                statusFiltro = Aluno.Estados.Normalize(query.Status);
            }
            catch
            {
                return new AlunosListagemResultado(false, [], "Status de filtro invalido.", AlunosListagemFalha.Validacao);
            }
        }

        var rows = await _alunos.ListarPorEscolaAsync(
            escolaId.Value,
            statusFiltro,
            query.Busca,
            query.Limite,
            cancellationToken);

        var itens = rows.Select(r => new AlunoListItemResponse
        {
            Id = r.Id,
            EscolaId = r.EscolaId,
            Nome = r.Nome,
            Sobrenome = r.Sobrenome,
            Cpf = r.Cpf,
            Status = r.Status
        }).ToList();

        return new AlunosListagemResultado(true, itens, null, AlunosListagemFalha.Nenhuma);
    }

    public async Task<AlunoDetalheResultado> ObterPorIdAsync(
        int id,
        AppUserContext uc,
        CancellationToken cancellationToken = default)
    {
        if (id <= 0)
        {
            return new AlunoDetalheResultado(false, null, "Id de aluno invalido.", AlunoDetalheFalha.NaoEncontrado);
        }

        var escolaId = await ObterIdEscolaAtivaPorCodigoAsync(uc.CodigoEscola, cancellationToken);
        if (!escolaId.HasValue)
        {
            return new AlunoDetalheResultado(false, null, "Acesso negado.", AlunoDetalheFalha.AcessoNegado);
        }

        var aluno = await _alunos.ObterPorIdEEscolaAsync(id, escolaId.Value, cancellationToken);
        if (aluno is null)
        {
            return new AlunoDetalheResultado(false, null, "Aluno nao encontrado.", AlunoDetalheFalha.NaoEncontrado);
        }

        var escola = await _escolas.ObterPorIdAsync(escolaId.Value, cancellationToken);
        var responsavel = await _alunos.ObterResponsavelPorIdEEscolaAsync(
            aluno.ResponsavelId,
            escolaId.Value,
            cancellationToken);

        var telefoneAluno = await _alunos.ObterTelefonePrincipalAsync(
            escolaId.Value,
            "aluno",
            aluno.Id,
            cancellationToken);

        string? telefoneResponsavel = null;
        if (responsavel is not null)
        {
            telefoneResponsavel = await _alunos.ObterTelefonePrincipalAsync(
                escolaId.Value,
                "responsavel",
                responsavel.Id,
                cancellationToken);
        }

        var resp = new AlunoDetalheResponse
        {
            Id = aluno.Id,
            EscolaId = aluno.EscolaId,
            EscolaNome = escola?.NomeFantasia ?? "",
            Nome = aluno.Nome,
            Sobrenome = aluno.Sobrenome,
            Sexo = aluno.Sexo,
            DataNascimento = aluno.DataNascimento.ToString("yyyy-MM-dd"),
            DataIngresso = aluno.DataIngresso.ToString("yyyy-MM-dd"),
            Cpf = aluno.Cpf,
            Status = aluno.Status,
            Cep = aluno.Cep,
            TipoLogradouro = aluno.TipoLogradouro,
            Logradouro = aluno.Logradouro,
            Numero = aluno.Numero,
            Complemento = aluno.Complemento,
            Bairro = aluno.Bairro,
            Municipio = aluno.Municipio,
            NaturalidadeCidade = aluno.NaturalidadeCidade,
            NaturalidadeEstado = aluno.NaturalidadeEstado,
            RgNumero = aluno.RgNumero,
            RgExpedicao = aluno.RgExpedicao?.ToString("yyyy-MM-dd"),
            RgOrgao = aluno.RgOrgao,
            TelefoneAluno = telefoneAluno,
            EProprioResponsavel = aluno.EProprioResponsavel,
            ResponsavelNome = responsavel?.Nome,
            ResponsavelSobrenome = responsavel?.Sobrenome,
            ResponsavelCpf = responsavel?.CpfCnpj,
            TelefoneResponsavel = telefoneResponsavel,
        };

        return new AlunoDetalheResultado(true, resp, null, AlunoDetalheFalha.Nenhuma);
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

        var telAlunoDigits = SomenteDigitos(request.AlunoTelefone);
        var telRespDigits = SomenteDigitos(request.ResponsavelTelefone);
        if (request.EProprioResponsavel)
        {
            if (telAlunoDigits.Length < 10)
            {
                return new CriarAlunoResultado(
                    false,
                    null,
                    null,
                    "Telefone celular do aluno e obrigatorio quando ele e o proprio responsavel (minimo 10 digitos).",
                    CriarAlunoFalha.Validacao);
            }
        }
        else
        {
            if (telRespDigits.Length < 10)
            {
                return new CriarAlunoResultado(
                    false,
                    null,
                    null,
                    "Telefone celular do responsavel e obrigatorio (minimo 10 digitos).",
                    CriarAlunoFalha.Validacao);
            }

            if (telAlunoDigits is { Length: > 0 and < 10 })
            {
                return new CriarAlunoResultado(false, null, null, "Telefone do aluno invalido.", CriarAlunoFalha.Validacao);
            }
        }

        var erroCamposExtras = ValidarCamposExtrasAluno(request);
        if (erroCamposExtras is not null)
            return new CriarAlunoResultado(false, null, null, erroCamposExtras, CriarAlunoFalha.Validacao);

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
                    "Dados do responsavel (nome, sobrenome e CPF) sao obrigatorios quando o aluno nao e o proprio responsavel.",
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
                CorRaca = NormalizarCorRaca(request.CorRaca),
                EstadoCivil = NormalizarEstadoCivil(request.EstadoCivil),
                Profissao = string.IsNullOrWhiteSpace(request.Profissao) ? null : request.Profissao.Trim(),
                RegistroEscolar = string.IsNullOrWhiteSpace(request.RegistroEscolar) ? null : request.RegistroEscolar.Trim(),
                Nacionalidade = string.IsNullOrWhiteSpace(request.Nacionalidade) ? null : request.Nacionalidade.Trim(),
                DataEntradaPais = request.DataEntradaPais,
                NaturalidadeCidade = string.IsNullOrWhiteSpace(request.NaturalidadeCidade)
                    ? null
                    : request.NaturalidadeCidade.Trim(),
                NaturalidadeEstado = string.IsNullOrWhiteSpace(request.NaturalidadeEstado)
                    ? null
                    : request.NaturalidadeEstado.Trim().ToUpperInvariant(),
                RgNumero = string.IsNullOrWhiteSpace(request.RgNumero) ? null : request.RgNumero.Trim(),
                RgExpedicao = request.RgExpedicao,
                RgOrgao = string.IsNullOrWhiteSpace(request.RgOrgao) ? null : request.RgOrgao.Trim(),
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

            if (telAlunoDigits.Length >= 10)
            {
                await _alunos.InserirContatoTelefoneAsync(
                    escolaId.Value,
                    "aluno",
                    aluno.Id,
                    "Celular",
                    telAlunoDigits,
                    principal: true,
                    cancellationToken);
            }

            if (!request.EProprioResponsavel && telRespDigits.Length >= 10)
            {
                await _alunos.InserirContatoTelefoneAsync(
                    escolaId.Value,
                    "responsavel",
                    responsavelIdResolvido,
                    "Celular",
                    telRespDigits,
                    principal: true,
                    cancellationToken);
            }
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

    private static string SomenteDigitos(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        return new string(value.Where(char.IsAsciiDigit).ToArray());
    }

    private static string? NormalizarCorRaca(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var s = value.Trim();
        return s is "Branca" or "Preta" or "Parda" or "Amarela" or "Indigena" or "Nao Declarado" ? s : null;
    }

    private static string? NormalizarEstadoCivil(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var s = value.Trim();
        return s is "Solteiro" or "Casado" or "Divorciado" or "Viuvo" or "Uniao Estavel" ? s : null;
    }

    private static string? ValidarCamposExtrasAluno(CriarAlunoRequest request)
    {
        if (!string.IsNullOrWhiteSpace(request.CorRaca) && NormalizarCorRaca(request.CorRaca) is null)
            return "Valor invalido para cor/raca.";

        if (!string.IsNullOrWhiteSpace(request.EstadoCivil) && NormalizarEstadoCivil(request.EstadoCivil) is null)
            return "Valor invalido para estado civil.";

        if (!string.IsNullOrWhiteSpace(request.NaturalidadeEstado))
        {
            var u = request.NaturalidadeEstado.Trim().ToUpperInvariant();
            if (u.Length is < 2 or > 2 || !u.All(char.IsAsciiLetter))
                return "UF de naturalidade deve ter 2 letras.";
        }

        if (request.RgExpedicao.HasValue && request.RgExpedicao.Value > DateOnly.FromDateTime(DateTime.UtcNow))
            return "Data de expedicao do RG invalida.";

        return null;
    }
}
