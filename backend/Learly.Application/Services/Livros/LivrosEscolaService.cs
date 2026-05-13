using Learly.Application.Contracts.Livros;
using Learly.Application.Contracts.Livros.Requests;
using Learly.Application.Contracts.Livros.Responses;
using Learly.Application.Services.Common;
using Learly.Domain.Entities;
using Learly.Domain.Exceptions;
using Learly.Domain.Interfaces.Persistence;
using Learly.Domain.Interfaces.Repositories;

namespace Learly.Application.Services.Livros;

public sealed class LivrosEscolaService : ILivrosEscolaService
{
    private readonly ILivroCatalogoRepository _livros;
    private readonly IEscolaRepository _escolas;
    private readonly IUnitOfWork _unitOfWork;

    public LivrosEscolaService(ILivroCatalogoRepository livros, IEscolaRepository escolas, IUnitOfWork unitOfWork)
    {
        _livros = livros;
        _escolas = escolas;
        _unitOfWork = unitOfWork;
    }

    public async Task<LivrosEscolaListagemResultado> ListarAsync(
        AppUserContext uc,
        CancellationToken cancellationToken = default)
    {
        var escolaId = await ObterEscolaIdAsync(uc, cancellationToken);
        if (!escolaId.HasValue)
        {
            return new LivrosEscolaListagemResultado(false, [], "Acesso negado.", LivrosEscolaFalha.AcessoNegado);
        }

        var rows = await _livros.ListarTodosPorEscolaAsync(escolaId.Value, cancellationToken);
        var itens = rows.Select(r => new LivroEscolaResponse(
            r.Id,
            r.Nome,
            r.Status,
            r.QuantidadeCapitulos,
            r.TotalAulasPrevistas)).ToList();
        return new LivrosEscolaListagemResultado(true, itens, null, LivrosEscolaFalha.Nenhuma);
    }

    public async Task<LivrosEscolaDetalheResultado> ObterPorIdAsync(
        int livroId,
        AppUserContext uc,
        CancellationToken cancellationToken = default)
    {
        var escolaId = await ObterEscolaIdAsync(uc, cancellationToken);
        if (!escolaId.HasValue)
        {
            return new LivrosEscolaDetalheResultado(false, null, "Acesso negado.", LivrosEscolaFalha.AcessoNegado);
        }

        var entidade = await _livros.ObterPorIdEscolaComCapitulosAsync(livroId, escolaId.Value, cancellationToken);
        if (entidade is null)
        {
            return new LivrosEscolaDetalheResultado(false, null, "Livro nao encontrado.", LivrosEscolaFalha.NaoEncontrado);
        }

        var capsOrd = entidade.Capitulos.OrderBy(c => c.Id).ToList();
        var totalAulas = capsOrd.Sum(c => c.QtdAulasPrevistas);
        var dto = new LivroEscolaResponse(
            entidade.Id,
            entidade.Nome,
            entidade.Status,
            capsOrd.Count,
            totalAulas,
            MapearCapitulosDto(capsOrd));
        return new LivrosEscolaDetalheResultado(true, dto, null, LivrosEscolaFalha.Nenhuma);
    }

    public async Task<LivrosEscolaCriacaoResultado> CriarAsync(
        CriarLivroEscolaRequest body,
        AppUserContext uc,
        CancellationToken cancellationToken = default)
    {
        var escolaId = await ObterEscolaIdAsync(uc, cancellationToken);
        if (!escolaId.HasValue)
        {
            return new LivrosEscolaCriacaoResultado(false, null, "Acesso negado.", LivrosEscolaFalha.AcessoNegado);
        }

        var nome = body.Nome?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(nome) || nome.Length > 150)
        {
            return new LivrosEscolaCriacaoResultado(
                false, null,
                "Nome e obrigatorio e deve ter ate 150 caracteres.",
                LivrosEscolaFalha.Validacao);
        }

        if (await _livros.ExisteNomeEmEscolaAsync(escolaId.Value, nome, null, cancellationToken))
        {
            return new LivrosEscolaCriacaoResultado(false, null, "Ja existe um livro com este nome nesta escola.", LivrosEscolaFalha.Conflito);
        }

        const int maxCapitulos = 200;
        const int maxAulasPrevistasPorCapitulo = 500;
        var qtdCapitulos = body.QuantidadeCapitulos;
        var aulasLista = body.AulasPrevistasPorCapitulo;
        if (qtdCapitulos < 1 || qtdCapitulos > maxCapitulos)
        {
            return new LivrosEscolaCriacaoResultado(
                false, null,
                $"Quantidade de capitulos deve ser entre 1 e {maxCapitulos}.",
                LivrosEscolaFalha.Validacao);
        }

        if (aulasLista is null || aulasLista.Count != qtdCapitulos)
        {
            return new LivrosEscolaCriacaoResultado(
                false, null,
                "Informe a quantidade de aulas previstas para cada capitulo (lista com o mesmo tamanho da quantidade de capitulos).",
                LivrosEscolaFalha.Validacao);
        }

        for (var idx = 0; idx < aulasLista.Count; idx++)
        {
            var v = aulasLista[idx];
            if (v < 1 || v > maxAulasPrevistasPorCapitulo)
            {
                return new LivrosEscolaCriacaoResultado(
                    false, null,
                    $"Capitulo {idx + 1}: aulas previstas devem ser entre 1 e {maxAulasPrevistasPorCapitulo}.",
                    LivrosEscolaFalha.Validacao);
            }
        }

        Livro entidade;
        try
        {
            var agora = DateTime.UtcNow;
            entidade = new Livro
            {
                EscolaId = escolaId.Value,
                Nome = nome,
                Status = "Ativo",
                DataCriacao = agora,
                DataAtualizacao = agora
            };

            for (var i = 1; i <= qtdCapitulos; i++)
            {
                entidade.Capitulos.Add(new Capitulo
                {
                    EscolaId = escolaId.Value,
                    Nome = $"Capítulo {i}",
                    QtdAulasPrevistas = aulasLista[i - 1],
                    Status = "Ativo",
                    DataCriacao = agora,
                    DataAtualizacao = agora
                });
            }
        }
        catch (DomainException ex)
        {
            return new LivrosEscolaCriacaoResultado(false, null, ex.Message, LivrosEscolaFalha.Validacao);
        }

        _livros.Adicionar(entidade);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var totalAulas = 0;
        for (var i = 0; i < aulasLista.Count; i++)
            totalAulas += aulasLista[i];

        var dto = new LivroEscolaResponse(
            entidade.Id,
            entidade.Nome,
            entidade.Status,
            qtdCapitulos,
            totalAulas);
        return new LivrosEscolaCriacaoResultado(true, dto, null, LivrosEscolaFalha.Nenhuma);
    }

    public async Task<LivrosEscolaAtualizacaoResultado> AtualizarAsync(
        int livroId,
        AtualizarLivroEscolaRequest body,
        AppUserContext uc,
        CancellationToken cancellationToken = default)
    {
        var escolaId = await ObterEscolaIdAsync(uc, cancellationToken);
        if (!escolaId.HasValue)
        {
            return new LivrosEscolaAtualizacaoResultado(false, null, "Acesso negado.", LivrosEscolaFalha.AcessoNegado);
        }

        var temNome = body.Nome is not null;
        var temStatus = body.Status is not null;
        var temCapitulosAulas = body.CapitulosAulas is { Count: > 0 };
        var temCapitulosNovos = body.CapitulosNovos is { Count: > 0 };

        if (!temNome && !temStatus && !temCapitulosAulas && !temCapitulosNovos)
        {
            return new LivrosEscolaAtualizacaoResultado(
                false, null,
                "Informe ao menos nome, status, capitulosAulas ou capitulosNovos para atualizar.",
                LivrosEscolaFalha.Validacao);
        }

        string? novoStatus = null;
        if (body.Status is not null)
        {
            novoStatus = body.Status.Trim();
            if (!string.Equals(novoStatus, "Ativo", StringComparison.Ordinal) &&
                !string.Equals(novoStatus, "Inativo", StringComparison.Ordinal))
            {
                return new LivrosEscolaAtualizacaoResultado(
                    false, null,
                    "Status deve ser Ativo ou Inativo.",
                    LivrosEscolaFalha.Validacao);
            }
        }

        var precisaCapitulosNoContexto = temCapitulosAulas || temCapitulosNovos;
        var entidade = precisaCapitulosNoContexto
            ? await _livros.ObterRastreadoPorIdEscolaComCapitulosAsync(livroId, escolaId.Value, cancellationToken)
            : await _livros.ObterRastreadoPorIdEscolaAsync(livroId, escolaId.Value, cancellationToken);
        if (entidade is null)
        {
            return new LivrosEscolaAtualizacaoResultado(false, null, "Livro nao encontrado.", LivrosEscolaFalha.NaoEncontrado);
        }

        string? novoNome = null;
        if (body.Nome is not null)
        {
            novoNome = body.Nome.Trim();
            if (string.IsNullOrWhiteSpace(novoNome) || novoNome.Length > 150)
            {
                return new LivrosEscolaAtualizacaoResultado(
                    false, null,
                    "Nome deve ter entre 1 e 150 caracteres.",
                    LivrosEscolaFalha.Validacao);
            }

            if (!string.Equals(novoNome, entidade.Nome, StringComparison.Ordinal) &&
                await _livros.ExisteNomeEmEscolaAsync(escolaId.Value, novoNome, livroId, cancellationToken))
            {
                return new LivrosEscolaAtualizacaoResultado(
                    false, null,
                    "Ja existe um livro com este nome nesta escola.",
                    LivrosEscolaFalha.Conflito);
            }

            try
            {
                entidade.Nome = novoNome;
            }
            catch (DomainException ex)
            {
                return new LivrosEscolaAtualizacaoResultado(false, null, ex.Message, LivrosEscolaFalha.Validacao);
            }
        }

        if (novoStatus is not null)
        {
            try
            {
                entidade.Status = novoStatus;
            }
            catch (DomainException ex)
            {
                return new LivrosEscolaAtualizacaoResultado(false, null, ex.Message, LivrosEscolaFalha.Validacao);
            }
        }

        if (temCapitulosAulas)
        {
            const int maxAulasPrevistasPorCapitulo = 500;
            var capsOrd = entidade.Capitulos.OrderBy(c => c.Id).ToList();
            if (body.CapitulosAulas!.Count != capsOrd.Count)
            {
                return new LivrosEscolaAtualizacaoResultado(
                    false, null,
                    capsOrd.Count == 0
                        ? "Este livro nao possui capitulos para atualizar."
                        : $"Envie exatamente {capsOrd.Count} item(ns) em capitulosAulas (um por capitulo).",
                    LivrosEscolaFalha.Validacao);
            }

            var mapPorId = new Dictionary<int, int>();
            foreach (var item in body.CapitulosAulas)
            {
                if (item.QtdAulasPrevistas < 1 || item.QtdAulasPrevistas > maxAulasPrevistasPorCapitulo)
                {
                    return new LivrosEscolaAtualizacaoResultado(
                        false, null,
                        $"Aulas previstas por capitulo devem ser entre 1 e {maxAulasPrevistasPorCapitulo}.",
                        LivrosEscolaFalha.Validacao);
                }

                if (!mapPorId.TryAdd(item.CapituloId, item.QtdAulasPrevistas))
                {
                    return new LivrosEscolaAtualizacaoResultado(
                        false, null,
                        "Ids de capitulo repetidos em capitulosAulas.",
                        LivrosEscolaFalha.Validacao);
                }
            }

            foreach (var c in capsOrd)
            {
                if (!mapPorId.ContainsKey(c.Id))
                {
                    return new LivrosEscolaAtualizacaoResultado(
                        false, null,
                        "Cada capitulo do livro deve constar em capitulosAulas exatamente uma vez.",
                        LivrosEscolaFalha.Validacao);
                }
            }

            var agoraCap = DateTime.UtcNow;
            foreach (var c in capsOrd)
            {
                try
                {
                    c.QtdAulasPrevistas = mapPorId[c.Id];
                    c.DataAtualizacao = agoraCap;
                }
                catch (DomainException ex)
                {
                    return new LivrosEscolaAtualizacaoResultado(false, null, ex.Message, LivrosEscolaFalha.Validacao);
                }
            }
        }

        if (temCapitulosNovos)
        {
            const int maxCapitulosLivro = 200;
            const int maxAulasPrevistasPorCapitulo = 500;
            var atualCount = entidade.Capitulos.Count;
            if (atualCount + body.CapitulosNovos!.Count > maxCapitulosLivro)
            {
                return new LivrosEscolaAtualizacaoResultado(
                    false, null,
                    $"O livro pode ter no maximo {maxCapitulosLivro} capitulos no total (atual {atualCount}, novos {body.CapitulosNovos.Count}).",
                    LivrosEscolaFalha.Validacao);
            }

            var agoraNovo = DateTime.UtcNow;
            foreach (var item in body.CapitulosNovos)
            {
                if (item.QtdAulasPrevistas < 1 || item.QtdAulasPrevistas > maxAulasPrevistasPorCapitulo)
                {
                    return new LivrosEscolaAtualizacaoResultado(
                        false, null,
                        $"Cada capitulo novo deve ter entre 1 e {maxAulasPrevistasPorCapitulo} aulas previstas.",
                        LivrosEscolaFalha.Validacao);
                }

                var ordemNome = entidade.Capitulos.Count + 1;
                var nomeCap = string.IsNullOrWhiteSpace(item.Nome?.Trim())
                    ? $"Capítulo {ordemNome}"
                    : item.Nome.Trim();

                try
                {
                    entidade.Capitulos.Add(new Capitulo
                    {
                        EscolaId = escolaId.Value,
                        LivroId = entidade.Id,
                        Nome = nomeCap,
                        QtdAulasPrevistas = item.QtdAulasPrevistas,
                        Status = "Ativo",
                        DataCriacao = agoraNovo,
                        DataAtualizacao = agoraNovo
                    });
                }
                catch (DomainException ex)
                {
                    return new LivrosEscolaAtualizacaoResultado(false, null, ex.Message, LivrosEscolaFalha.Validacao);
                }
            }
        }

        entidade.DataAtualizacao = DateTime.UtcNow;
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var totais = await _livros.ObterTotaisCapitulosPorLivroAsync(livroId, escolaId.Value, cancellationToken);
        IReadOnlyList<LivroCapituloItemResponse>? capDtos = null;
        if (temCapitulosAulas || temCapitulosNovos)
            capDtos = MapearCapitulosDto(entidade.Capitulos);

        var dto = new LivroEscolaResponse(
            entidade.Id,
            entidade.Nome,
            entidade.Status,
            totais.QuantidadeCapitulos,
            totais.TotalAulasPrevistas,
            capDtos);
        return new LivrosEscolaAtualizacaoResultado(true, dto, null, LivrosEscolaFalha.Nenhuma);
    }

    private static IReadOnlyList<LivroCapituloItemResponse> MapearCapitulosDto(IEnumerable<Capitulo> capitulos) =>
        capitulos
            .OrderBy(c => c.Id)
            .Select(c => new LivroCapituloItemResponse(c.Id, c.Nome, c.QtdAulasPrevistas, c.Status))
            .ToList();

    private async Task<int?> ObterEscolaIdAsync(AppUserContext uc, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(uc.CodigoEscola))
            return null;

        return await _escolas.ObterIdAtivaPorCodigoEscolaAsync(uc.CodigoEscola.Trim(), cancellationToken);
    }
}
