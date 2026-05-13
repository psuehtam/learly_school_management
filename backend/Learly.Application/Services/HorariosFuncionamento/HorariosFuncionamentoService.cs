using Learly.Application.Contracts.HorariosFuncionamento;
using Learly.Application.Contracts.HorariosFuncionamento.Requests;
using Learly.Application.Services.Common;
using Learly.Domain.Entities;
using Learly.Domain.Exceptions;
using Learly.Domain.Interfaces.Persistence;
using Learly.Domain.Interfaces.Repositories;

namespace Learly.Application.Services.HorariosFuncionamento;

public sealed class HorariosFuncionamentoService : IHorariosFuncionamentoService
{
    private readonly IEscolaHorarioFuncionamentoRepository _horarios;
    private readonly IEscolaRepository _escolas;
    private readonly IUnitOfWork _unitOfWork;

    public HorariosFuncionamentoService(
        IEscolaHorarioFuncionamentoRepository horarios,
        IEscolaRepository escolas,
        IUnitOfWork unitOfWork)
    {
        _horarios = horarios;
        _escolas = escolas;
        _unitOfWork = unitOfWork;
    }

    public async Task<HorariosFuncionamentoListagemResultado> ListarAsync(
        AppUserContext uc,
        CancellationToken cancellationToken = default)
    {
        var escolaId = await ObterEscolaIdAsync(uc, cancellationToken);
        if (!escolaId.HasValue)
        {
            return Falha(HorariosFuncionamentoFalha.AcessoNegado, "Acesso negado.");
        }

        var entidades = await _horarios.ListarPorEscolaAsync(escolaId.Value, cancellationToken);
        var itens = MapearLista(entidades);
        return new HorariosFuncionamentoListagemResultado(true, itens, null, HorariosFuncionamentoFalha.Nenhuma);
    }

    public async Task<HorariosFuncionamentoAtualizacaoResultado> AtualizarAsync(
        AtualizarHorariosEscolaRequest request,
        AppUserContext uc,
        CancellationToken cancellationToken = default)
    {
        var escolaId = await ObterEscolaIdAsync(uc, cancellationToken);
        if (!escolaId.HasValue)
        {
            return FalhaAtualizacao(HorariosFuncionamentoFalha.AcessoNegado, "Acesso negado.");
        }

        if (request.Dias is null || request.Dias.Count == 0)
        {
            return FalhaAtualizacao(HorariosFuncionamentoFalha.Validacao, "Informe ao menos um dia para atualizar.");
        }

        var diasDistintos = request.Dias.Select(d => d.DiaSemana).Distinct().ToList();
        if (diasDistintos.Count != request.Dias.Count)
        {
            return FalhaAtualizacao(HorariosFuncionamentoFalha.Validacao, "Dias duplicados na requisicao.");
        }

        if (diasDistintos.Any(d => d < 0 || d > 6))
        {
            return FalhaAtualizacao(HorariosFuncionamentoFalha.Validacao, "Dia da semana invalido. Use 0 (domingo) a 6 (sabado).");
        }

        var existentes = await _horarios.ListarRastreadosPorEscolaAsync(escolaId.Value, cancellationToken);
        var mapaExistentes = existentes.ToDictionary(h => h.DiaSemana);

        try
        {
            foreach (var item in request.Dias)
            {
                TimeOnly? abertura = null;
                TimeOnly? fechamento = null;

                if (item.Aberto)
                {
                    if (string.IsNullOrWhiteSpace(item.HorarioAbertura) ||
                        string.IsNullOrWhiteSpace(item.HorarioFechamento))
                    {
                        return FalhaAtualizacao(
                            HorariosFuncionamentoFalha.Validacao,
                            $"Horarios de abertura e fechamento sao obrigatorios para o dia {item.DiaSemana} (aberto).");
                    }

                    if (!TimeOnly.TryParseExact(item.HorarioAbertura.Trim(), "HH:mm", out var ab) ||
                        !TimeOnly.TryParseExact(item.HorarioFechamento.Trim(), "HH:mm", out var fe))
                    {
                        return FalhaAtualizacao(
                            HorariosFuncionamentoFalha.Validacao,
                            $"Formato de horario invalido para o dia {item.DiaSemana}. Use HH:mm.");
                    }

                    abertura = ab;
                    fechamento = fe;
                }

                if (mapaExistentes.TryGetValue(item.DiaSemana, out var existente))
                {
                    existente.Atualizar(item.Aberto, abertura, fechamento);
                }
                else
                {
                    var novo = EscolaHorarioFuncionamento.Criar(escolaId.Value, item.DiaSemana, item.Aberto, abertura, fechamento);
                    await _horarios.AdicionarAsync(novo, cancellationToken);
                }
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (DomainException ex)
        {
            return FalhaAtualizacao(HorariosFuncionamentoFalha.Validacao, ex.Message);
        }

        var atualizados = await _horarios.ListarPorEscolaAsync(escolaId.Value, cancellationToken);
        return new HorariosFuncionamentoAtualizacaoResultado(
            true,
            MapearLista(atualizados),
            null,
            HorariosFuncionamentoFalha.Nenhuma);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private async Task<int?> ObterEscolaIdAsync(AppUserContext uc, CancellationToken ct)
    {
        if (uc.IsSuperAdmin || string.IsNullOrWhiteSpace(uc.CodigoEscola))
            return null;

        return await _escolas.ObterIdAtivaPorCodigoEscolaAsync(uc.CodigoEscola, ct);
    }

    private static IReadOnlyList<HorarioFuncionamentoResponse> MapearLista(
        IEnumerable<EscolaHorarioFuncionamento> entidades)
    {
        return entidades
            .OrderBy(h => h.DiaSemana)
            .Select(h => new HorarioFuncionamentoResponse(
                h.DiaSemana,
                h.Aberto,
                h.HorarioAbertura?.ToString("HH:mm"),
                h.HorarioFechamento?.ToString("HH:mm")))
            .ToList();
    }

    private static HorariosFuncionamentoListagemResultado Falha(HorariosFuncionamentoFalha tipo, string msg)
        => new(false, [], msg, tipo);

    private static HorariosFuncionamentoAtualizacaoResultado FalhaAtualizacao(HorariosFuncionamentoFalha tipo, string msg)
        => new(false, null, msg, tipo);
}
