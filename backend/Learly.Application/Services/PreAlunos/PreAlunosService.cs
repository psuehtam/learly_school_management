using Learly.Application.Contracts.PreAlunos;
using Learly.Application.Contracts.PreAlunos.Requests;
using Learly.Application.Contracts.PreAlunos.Responses;
using Learly.Application.Services.Common;
using Learly.Domain.Entities;
using Learly.Domain.Interfaces.Persistence;
using Learly.Domain.Interfaces.Repositories;

namespace Learly.Application.Services.PreAlunos;

public sealed class PreAlunosService : IPreAlunosService
{
    private readonly IPreAlunoRepository _preAlunos;
    private readonly IAlunoRepository _alunos;
    private readonly ILivroCatalogoRepository _livros;
    private readonly IEscolaRepository _escolas;
    private readonly IUnitOfWork _unitOfWork;

    public PreAlunosService(
        IPreAlunoRepository preAlunos,
        IAlunoRepository alunos,
        ILivroCatalogoRepository livros,
        IEscolaRepository escolas,
        IUnitOfWork unitOfWork)
    {
        _preAlunos = preAlunos;
        _alunos = alunos;
        _livros = livros;
        _escolas = escolas;
        _unitOfWork = unitOfWork;
    }

    public async Task<PreAlunosCatalogoLivrosResultado> ListarCatalogoLivrosInteresseAsync(
        AppUserContext uc,
        CancellationToken cancellationToken = default)
    {
        var escolaId = await ObterIdEscolaAtivaPorCodigoAsync(uc.CodigoEscola, cancellationToken);
        if (!escolaId.HasValue)
        {
            return new PreAlunosCatalogoLivrosResultado(false, [], "Acesso negado.", PreAlunosCatalogoLivrosFalha.AcessoNegado);
        }

        var itens = await _livros.ListarAtivosPorEscolaAsync(escolaId.Value, cancellationToken);
        var list = itens.Select(l => new LivroInteresseOpcaoResponse(l.Id, l.Nome, l.Status)).ToList();

        return new PreAlunosCatalogoLivrosResultado(true, list, null, PreAlunosCatalogoLivrosFalha.Nenhuma);
    }

    public async Task<PreAlunosListagemResultado> ListarAsync(
        ListarPreAlunosQuery query,
        AppUserContext uc,
        CancellationToken cancellationToken = default)
    {
        var escolaId = await ObterIdEscolaAtivaPorCodigoAsync(uc.CodigoEscola, cancellationToken);
        if (!escolaId.HasValue)
        {
            return new PreAlunosListagemResultado(false, [], "Acesso negado.", PreAlunosListagemFalha.AcessoNegado);
        }

        string? filtroStatus = null;
        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            try
            {
                filtroStatus = PreAluno.Estados.Normalize(query.Status);
            }
            catch
            {
                return new PreAlunosListagemResultado(false, [], "Status de filtro invalido.", PreAlunosListagemFalha.Validacao);
            }
        }

        var rows = await _preAlunos.ListarPorEscolaAsync(escolaId.Value, filtroStatus, cancellationToken);
        var itens = rows.Select(r => new PreAlunoListItemResponse(
            r.Id,
            r.NomeCompletoAluno,
            r.NomeCompletoResponsavel,
            r.DataCadastro,
            r.TipoContrato,
            r.Status,
            r.NomeLivroInteresse,
            r.TelefoneAluno,
            r.ValorMensalidade,
            r.FormaPagamento,
            r.OrigemCaptacao,
            r.ValorMaterial,
            r.ValorMatricula)).ToList();

        return new PreAlunosListagemResultado(true, itens, null, PreAlunosListagemFalha.Nenhuma);
    }

    public async Task<PreAlunoDetalheResultado> ObterPorIdAsync(
        int id,
        AppUserContext uc,
        CancellationToken cancellationToken = default)
    {
        var escolaId = await ObterIdEscolaAtivaPorCodigoAsync(uc.CodigoEscola, cancellationToken);
        if (!escolaId.HasValue)
        {
            return new PreAlunoDetalheResultado(false, null, "Acesso negado.", PreAlunoDetalheFalha.AcessoNegado);
        }

        var d = await _preAlunos.ObterDetalheAsync(id, escolaId.Value, cancellationToken);
        if (d is null)
        {
            return new PreAlunoDetalheResultado(false, null, "Pre-aluno nao encontrado.", PreAlunoDetalheFalha.NaoEncontrado);
        }

        var resp = new PreAlunoDetalheResponse(
            d.Id,
            d.EscolaId,
            d.ResponsavelId,
            d.ResponsavelTipoPessoa,
            d.ResponsavelCpfCnpj,
            d.ResponsavelNomeCompleto,
            d.NomeAluno,
            d.SobrenomeAluno,
            d.DataNascimentoAluno,
            d.TelefoneAluno,
            d.LivroInteresseId,
            d.NomeLivroInteresse,
            d.TipoContrato,
            d.ValorMensalidade,
            d.FormaPagamento,
            d.ValorMatricula,
            d.FormaPagamentoMatricula,
            d.ValorMaterial,
            d.OrigemCaptacao,
            d.UsaTransporteVan,
            d.TransporteCep,
            d.TransporteLogradouro,
            d.TransporteNumero,
            d.TransporteComplemento,
            d.TransporteBairro,
            d.TransporteCidade,
            d.TransporteUf,
            d.ObservacoesComerciais,
            d.Status,
            d.AlunoId,
            d.CriadoPorUsuarioId,
            d.DataCriacao,
            d.DataAtualizacao);

        return new PreAlunoDetalheResultado(true, resp, null, PreAlunoDetalheFalha.Nenhuma);
    }

    public async Task<PreAlunoCriacaoResultado> CriarAsync(
        CriarPreAlunoRequest request,
        AppUserContext uc,
        CancellationToken cancellationToken = default)
    {
        var escolaId = await ObterIdEscolaAtivaPorCodigoAsync(uc.CodigoEscola, cancellationToken);
        if (!escolaId.HasValue)
        {
            return new PreAlunoCriacaoResultado(false, null, "Acesso negado.", PreAlunoCriacaoFalha.AcessoNegado);
        }

        if (uc.UserId <= 0)
        {
            return new PreAlunoCriacaoResultado(false, null, "Usuario invalido.", PreAlunoCriacaoFalha.Validacao);
        }

        if (string.IsNullOrWhiteSpace(request.Nome) || string.IsNullOrWhiteSpace(request.Sobrenome))
        {
            return new PreAlunoCriacaoResultado(false, null, "Nome e sobrenome do pre-aluno sao obrigatorios.", PreAlunoCriacaoFalha.Validacao);
        }

        if (request.DataNascimento == default)
        {
            return new PreAlunoCriacaoResultado(false, null, "Data de nascimento do pre-aluno e obrigatoria.", PreAlunoCriacaoFalha.Validacao);
        }

        var hojeReferencia = DateOnly.FromDateTime(DateTime.Today);
        if (request.DataNascimento > hojeReferencia)
        {
            return new PreAlunoCriacaoResultado(false, null, "Data de nascimento do pre-aluno invalida.", PreAlunoCriacaoFalha.Validacao);
        }

        var idadeAnos = CalcularIdadeAnos(request.DataNascimento, hojeReferencia);

        if (request.LivroInteresseId <= 0)
        {
            return new PreAlunoCriacaoResultado(false, null, "Livro de interesse invalido.", PreAlunoCriacaoFalha.Validacao);
        }

        var livroOk = await _preAlunos.ExisteLivroAtivoNaEscolaAsync(escolaId.Value, request.LivroInteresseId, cancellationToken);
        if (!livroOk)
        {
            return new PreAlunoCriacaoResultado(false, null, "Livro de interesse nao encontrado ou inativo nesta escola.", PreAlunoCriacaoFalha.Validacao);
        }

        if (request.ValorMensalidade <= 0)
        {
            return new PreAlunoCriacaoResultado(false, null, "Valor da mensalidade deve ser maior que zero.", PreAlunoCriacaoFalha.Validacao);
        }

        decimal? valorMaterialResolvido = request.ValorMaterial;
        if (valorMaterialResolvido is < 0)
        {
            return new PreAlunoCriacaoResultado(false, null, "Valor do material/livro nao pode ser negativo.", PreAlunoCriacaoFalha.Validacao);
        }

        var tipoContrato = request.TipoContrato.Trim();
        if (string.IsNullOrWhiteSpace(tipoContrato) || tipoContrato.Length > 120)
        {
            return new PreAlunoCriacaoResultado(false, null, "Tipo de contrato e obrigatorio (ate 120 caracteres).", PreAlunoCriacaoFalha.Validacao);
        }

        if (request.ValorMatricula < 0)
        {
            return new PreAlunoCriacaoResultado(false, null, "Valor da matricula nao pode ser negativo.", PreAlunoCriacaoFalha.Validacao);
        }

        var formaPgtoMatricula = string.IsNullOrWhiteSpace(request.FormaPagamentoMatricula)
            ? null
            : request.FormaPagamentoMatricula.Trim();
        if (request.ValorMatricula > 0 && string.IsNullOrWhiteSpace(formaPgtoMatricula))
        {
            return new PreAlunoCriacaoResultado(
                false,
                null,
                "Informe a forma de pagamento da matricula quando o valor for maior que zero.",
                PreAlunoCriacaoFalha.Validacao);
        }

        var origem = request.OrigemCaptacao.Trim();
        if (string.IsNullOrWhiteSpace(origem) || origem.Length > 80)
        {
            return new PreAlunoCriacaoResultado(false, null, "Origem de captacao e obrigatoria (ate 80 caracteres).", PreAlunoCriacaoFalha.Validacao);
        }

        if (request.UsaTransporteVan)
        {
            if (string.IsNullOrWhiteSpace(request.TransporteLogradouro) || request.TransporteLogradouro.Trim().Length > 200)
                return new PreAlunoCriacaoResultado(false, null, "Endereco para van: informe logradouro.", PreAlunoCriacaoFalha.Validacao);
            if (string.IsNullOrWhiteSpace(request.TransporteNumero) || request.TransporteNumero.Trim().Length > 20)
                return new PreAlunoCriacaoResultado(false, null, "Endereco para van: informe numero.", PreAlunoCriacaoFalha.Validacao);
            if (string.IsNullOrWhiteSpace(request.TransporteBairro) || request.TransporteBairro.Trim().Length > 100)
                return new PreAlunoCriacaoResultado(false, null, "Endereco para van: informe bairro.", PreAlunoCriacaoFalha.Validacao);
            if (string.IsNullOrWhiteSpace(request.TransporteCidade) || request.TransporteCidade.Trim().Length > 100)
                return new PreAlunoCriacaoResultado(false, null, "Endereco para van: informe cidade.", PreAlunoCriacaoFalha.Validacao);

            var uf = (request.TransporteUf ?? string.Empty).Trim().ToUpperInvariant();
            if (uf.Length != 2)
                return new PreAlunoCriacaoResultado(false, null, "Endereco para van: informe UF com 2 letras.", PreAlunoCriacaoFalha.Validacao);

            var cepVan = SomenteDigitos(request.TransporteCep);
            if (cepVan.Length != 8)
                return new PreAlunoCriacaoResultado(false, null, "Endereco para van: informe CEP com 8 digitos.", PreAlunoCriacaoFalha.Validacao);
        }

        var telAluno = SomenteDigitos(request.TelefoneAluno);
        if (telAluno is { Length: > 0 and < 10 })
        {
            return new PreAlunoCriacaoResultado(false, null, "Telefone do pre-aluno invalido.", PreAlunoCriacaoFalha.Validacao);
        }

        string docResp;
        string tipoResp;
        string nomeResp;
        string sobrenomeResp;
        string telResp;

        if (idadeAnos < 18)
        {
            if (request.EProprioResponsavel)
            {
                return new PreAlunoCriacaoResultado(
                    false,
                    null,
                    "Menores de 18 anos precisam de um responsavel financeiro cadastrado (nao pode ser o proprio aluno).",
                    PreAlunoCriacaoFalha.Validacao);
            }

            var tipoMenorResp = NormalizarTipoPessoa(request.ResponsavelTipoPessoa);
            if (tipoMenorResp is null)
            {
                return new PreAlunoCriacaoResultado(false, null, "Tipo de pessoa do responsavel invalido.", PreAlunoCriacaoFalha.Validacao);
            }

            docResp = SomenteDigitos(request.ResponsavelCpfCnpj);
            if (tipoMenorResp == "Fisica" && docResp.Length != 11)
            {
                return new PreAlunoCriacaoResultado(false, null, "CPF do responsavel deve ter 11 digitos.", PreAlunoCriacaoFalha.Validacao);
            }

            if (tipoMenorResp == "Juridica" && docResp.Length != 14)
            {
                return new PreAlunoCriacaoResultado(false, null, "CNPJ do responsavel deve ter 14 digitos.", PreAlunoCriacaoFalha.Validacao);
            }

            telResp = SomenteDigitos(request.ResponsavelTelefone);
            if (telResp.Length < 10)
            {
                return new PreAlunoCriacaoResultado(false, null, "Telefone do responsavel e obrigatorio (minimo 10 digitos).", PreAlunoCriacaoFalha.Validacao);
            }

            if (string.IsNullOrWhiteSpace(request.ResponsavelNome) || string.IsNullOrWhiteSpace(request.ResponsavelSobrenome))
            {
                return new PreAlunoCriacaoResultado(false, null, "Nome e sobrenome do responsavel sao obrigatorios.", PreAlunoCriacaoFalha.Validacao);
            }

            tipoResp = tipoMenorResp;
            nomeResp = request.ResponsavelNome.Trim();
            sobrenomeResp = request.ResponsavelSobrenome.Trim();
        }
        else
        {
            if (request.EProprioResponsavel)
            {
                docResp = SomenteDigitos(request.AlunoCpf);
                if (docResp.Length != 11)
                {
                    return new PreAlunoCriacaoResultado(
                        false,
                        null,
                        "Informe o CPF do pre-aluno (11 digitos) quando ele for o proprio responsavel financeiro.",
                        PreAlunoCriacaoFalha.Validacao);
                }

                telResp = telAluno;
                if (telResp.Length < 10)
                {
                    return new PreAlunoCriacaoResultado(
                        false,
                        null,
                        "Telefone celular do pre-aluno e obrigatorio quando ele e o proprio responsavel financeiro.",
                        PreAlunoCriacaoFalha.Validacao);
                }

                tipoResp = "Fisica";
                nomeResp = request.Nome.Trim();
                sobrenomeResp = request.Sobrenome.Trim();
            }
            else
            {
                var tipoMaiorResp = NormalizarTipoPessoa(request.ResponsavelTipoPessoa);
                if (tipoMaiorResp is null)
                {
                    return new PreAlunoCriacaoResultado(false, null, "Tipo de pessoa do responsavel invalido.", PreAlunoCriacaoFalha.Validacao);
                }

                docResp = SomenteDigitos(request.ResponsavelCpfCnpj);
                if (tipoMaiorResp == "Fisica" && docResp.Length != 11)
                {
                    return new PreAlunoCriacaoResultado(false, null, "CPF do responsavel deve ter 11 digitos.", PreAlunoCriacaoFalha.Validacao);
                }

                if (tipoMaiorResp == "Juridica" && docResp.Length != 14)
                {
                    return new PreAlunoCriacaoResultado(false, null, "CNPJ do responsavel deve ter 14 digitos.", PreAlunoCriacaoFalha.Validacao);
                }

                telResp = SomenteDigitos(request.ResponsavelTelefone);
                if (telResp.Length < 10)
                {
                    return new PreAlunoCriacaoResultado(false, null, "Telefone do responsavel e obrigatorio (minimo 10 digitos).", PreAlunoCriacaoFalha.Validacao);
                }

                if (string.IsNullOrWhiteSpace(request.ResponsavelNome) || string.IsNullOrWhiteSpace(request.ResponsavelSobrenome))
                {
                    return new PreAlunoCriacaoResultado(false, null, "Nome e sobrenome do responsavel sao obrigatorios.", PreAlunoCriacaoFalha.Validacao);
                }

                tipoResp = tipoMaiorResp;
                nomeResp = request.ResponsavelNome.Trim();
                sobrenomeResp = request.ResponsavelSobrenome.Trim();
            }
        }

        var formaPgtoMensal = string.IsNullOrWhiteSpace(request.FormaPagamento) ? null : request.FormaPagamento.Trim();

        string? transporteCep = null;
        string? transporteLog = null;
        string? transporteNumero = null;
        string? transporteComp = null;
        string? transporteBairro = null;
        string? transporteCidade = null;
        string? transporteUf = null;

        if (request.UsaTransporteVan)
        {
            transporteCep = SomenteDigitos(request.TransporteCep);
            transporteLog = request.TransporteLogradouro!.Trim();
            transporteNumero = request.TransporteNumero!.Trim();
            transporteComp = string.IsNullOrWhiteSpace(request.TransporteComplemento)
                ? null
                : request.TransporteComplemento.Trim();
            transporteBairro = request.TransporteBairro!.Trim();
            transporteCidade = request.TransporteCidade!.Trim();
            transporteUf = (request.TransporteUf ?? string.Empty).Trim().ToUpperInvariant();
        }

        var novoId = 0;

        try
        {
            await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                int responsavelIdResolvido;
                var existente = await _alunos.ObterResponsavelIdPorCpfAsync(escolaId.Value, docResp, cancellationToken);
                if (existente.HasValue)
                {
                    responsavelIdResolvido = existente.Value;
                }
                else
                {
                    responsavelIdResolvido = await _alunos.CriarResponsavelMinimoAsync(
                        escolaId.Value,
                        tipoResp,
                        docResp,
                        nomeResp,
                        sobrenomeResp,
                        cancellationToken);

                    await _alunos.InserirContatoTelefoneAsync(
                        escolaId.Value,
                        "responsavel",
                        responsavelIdResolvido,
                        "Celular",
                        telResp,
                        principal: true,
                        cancellationToken);
                }

                var agora = DateTime.UtcNow;
                var preAluno = new PreAluno
                {
                    EscolaId = escolaId.Value,
                    ResponsavelId = responsavelIdResolvido,
                    Nome = request.Nome.Trim(),
                    Sobrenome = request.Sobrenome.Trim(),
                    DataNascimento = request.DataNascimento,
                    Telefone = telAluno.Length >= 10 ? telAluno : null,
                    LivroInteresseId = request.LivroInteresseId,
                    TipoContrato = tipoContrato,
                    ValorMensalidade = request.ValorMensalidade,
                    FormaPagamento = formaPgtoMensal,
                    ValorMatricula = request.ValorMatricula,
                    FormaPagamentoMatricula = formaPgtoMatricula,
                    ValorMaterial = valorMaterialResolvido,
                    OrigemCaptacao = origem,
                    UsaTransporteVan = request.UsaTransporteVan,
                    TransporteCep = transporteCep,
                    TransporteLogradouro = transporteLog,
                    TransporteNumero = transporteNumero,
                    TransporteComplemento = transporteComp,
                    TransporteBairro = transporteBairro,
                    TransporteCidade = transporteCidade,
                    TransporteUf = transporteUf,
                    ObservacoesComerciais = string.IsNullOrWhiteSpace(request.ObservacoesComerciais)
                        ? null
                        : request.ObservacoesComerciais.Trim(),
                    Status = PreAluno.Estados.EmNegociacao,
                    CriadoPorUsuarioId = uc.UserId,
                    DataCriacao = agora,
                    DataAtualizacao = agora
                };

                _preAlunos.Adicionar(preAluno);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                novoId = preAluno.Id;
            }, cancellationToken);
        }
        catch (Exception ex) when (ContemDuplicateEntry(ex))
        {
            return new PreAlunoCriacaoResultado(false, null, "Ja existe cadastro para este documento nesta escola.", PreAlunoCriacaoFalha.Conflito);
        }

        return new PreAlunoCriacaoResultado(true, novoId, null, PreAlunoCriacaoFalha.Nenhuma);
    }

    public async Task<PreAlunoOperacaoResultado> SubmeterParaAprovacaoAsync(
        int id,
        AppUserContext uc,
        CancellationToken cancellationToken = default)
    {
        var escolaId = await ObterIdEscolaAtivaPorCodigoAsync(uc.CodigoEscola, cancellationToken);
        if (!escolaId.HasValue)
        {
            return new PreAlunoOperacaoResultado(false, "Acesso negado.", 403);
        }

        var pre = await _preAlunos.ObterPorIdEEscolaRastreadoAsync(id, escolaId.Value, cancellationToken);
        if (pre is null)
        {
            return new PreAlunoOperacaoResultado(false, "Pre-aluno nao encontrado.", 404);
        }

        if (!string.Equals(pre.Status, PreAluno.Estados.EmNegociacao, StringComparison.Ordinal))
        {
            return new PreAlunoOperacaoResultado(
                false,
                "Somente pre-alunos em negociacao podem ser enviados para aprovacao.",
                409);
        }

        pre.Status = PreAluno.Estados.AguardandoAprovacao;
        pre.DataAtualizacao = DateTime.UtcNow;
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new PreAlunoOperacaoResultado(true, null, 204);
    }

    public async Task<PreAlunoOperacaoResultado> AprovarAsync(
        int id,
        AppUserContext uc,
        CancellationToken cancellationToken = default)
    {
        var escolaId = await ObterIdEscolaAtivaPorCodigoAsync(uc.CodigoEscola, cancellationToken);
        if (!escolaId.HasValue)
        {
            return new PreAlunoOperacaoResultado(false, "Acesso negado.", 403);
        }

        var pre = await _preAlunos.ObterPorIdEEscolaRastreadoAsync(id, escolaId.Value, cancellationToken);
        if (pre is null)
        {
            return new PreAlunoOperacaoResultado(false, "Pre-aluno nao encontrado.", 404);
        }

        if (!string.Equals(pre.Status, PreAluno.Estados.AguardandoAprovacao, StringComparison.Ordinal))
        {
            return new PreAlunoOperacaoResultado(
                false,
                "Somente pre-alunos aguardando aprovacao podem ser aprovados.",
                409);
        }

        pre.Status = PreAluno.Estados.Aprovado;
        pre.DataAtualizacao = DateTime.UtcNow;
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new PreAlunoOperacaoResultado(true, null, 204);
    }

    public async Task<PreAlunoOperacaoResultado> CancelarAsync(
        int id,
        AppUserContext uc,
        CancellationToken cancellationToken = default)
    {
        var escolaId = await ObterIdEscolaAtivaPorCodigoAsync(uc.CodigoEscola, cancellationToken);
        if (!escolaId.HasValue)
        {
            return new PreAlunoOperacaoResultado(false, "Acesso negado.", 403);
        }

        var pre = await _preAlunos.ObterPorIdEEscolaRastreadoAsync(id, escolaId.Value, cancellationToken);
        if (pre is null)
        {
            return new PreAlunoOperacaoResultado(false, "Pre-aluno nao encontrado.", 404);
        }

        if (string.Equals(pre.Status, PreAluno.Estados.Matriculado, StringComparison.Ordinal))
        {
            return new PreAlunoOperacaoResultado(false, "Pre-aluno ja matriculado nao pode ser cancelado.", 409);
        }

        pre.Status = PreAluno.Estados.Cancelado;
        pre.DataAtualizacao = DateTime.UtcNow;
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new PreAlunoOperacaoResultado(true, null, 204);
    }

    private Task<int?> ObterIdEscolaAtivaPorCodigoAsync(string? codigoEscola, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(codigoEscola))
            return Task.FromResult<int?>(null);

        return _escolas.ObterIdAtivaPorCodigoEscolaAsync(codigoEscola.Trim(), cancellationToken);
    }

    private static string? NormalizarTipoPessoa(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "Fisica";

        var s = value.Trim();
        if (s.Equals("Fisica", StringComparison.OrdinalIgnoreCase)) return "Fisica";
        if (s.Equals("Juridica", StringComparison.OrdinalIgnoreCase)) return "Juridica";
        return null;
    }

    private static string SomenteDigitos(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        return new string(value.Where(char.IsAsciiDigit).ToArray());
    }

    private static int CalcularIdadeAnos(DateOnly dataNascimento, DateOnly hoje)
    {
        var idade = hoje.Year - dataNascimento.Year;
        if (hoje.Month < dataNascimento.Month ||
            (hoje.Month == dataNascimento.Month && hoje.Day < dataNascimento.Day))
        {
            idade--;
        }

        return idade;
    }

    private static bool ContemDuplicateEntry(Exception ex)
    {
        for (var e = ex; e is not null; e = e.InnerException)
        {
            if (e.Message.Contains("Duplicate", StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }
}
