using Learly.Application.Contracts.Calendario.Requests;
using Learly.Application.Contracts.Calendario.Responses;
using Learly.Application.Services.Common;
using Learly.Application.Services.Turmas;
using Learly.Domain.Entities;
using Learly.Domain.Exceptions;
using Learly.Domain.Interfaces.Persistence;
using Learly.Domain.Interfaces.Repositories;
using MapsterMapper;
using Microsoft.Extensions.Logging;

namespace Learly.Application.Services.Calendario;

public sealed class CalendarioService : ICalendarioService
{
    private readonly ICalendarioGeralRepository _calendario;
    private readonly ICompromissoRepository _compromissos;
    private readonly IEscolaRepository _escolas;
    private readonly ITurmasService _turmas;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<CalendarioService> _logger;

    public CalendarioService(
        ICalendarioGeralRepository calendario,
        ICompromissoRepository compromissos,
        IEscolaRepository escolas,
        ITurmasService turmas,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<CalendarioService> logger)
    {
        _calendario = calendario;
        _compromissos = compromissos;
        _escolas = escolas;
        _turmas = turmas;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<IReadOnlyList<EventoCalendarioResponse>> ListarPorMesAsync(
        int mes,
        int ano,
        AppUserContext uc,
        CancellationToken cancellationToken = default)
    {
        if (mes is < 1 or > 12 || ano < 2000)
            return [];

        var escolaId = await ObterIdEscolaAtivaPorCodigoAsync(uc.CodigoEscola, cancellationToken);
        if (!escolaId.HasValue)
            return [];

        var inicio = new DateOnly(ano, mes, 1);
        var fim = inicio.AddMonths(1).AddDays(-1);

        var eventos = await _calendario.ListarPorPeriodoAsync(escolaId.Value, inicio, fim, cancellationToken);
        return eventos.Select(e => _mapper.Map<EventoCalendarioResponse>(e)).ToList();
    }

    public async Task<(bool Success, EventoCalendarioResponse? Evento, string? Error, int StatusCode)> CriarAsync(
        CriarEventoCalendarioRequest request,
        AppUserContext uc,
        CancellationToken cancellationToken = default)
    {
        var escolaId = await ObterIdEscolaAtivaPorCodigoAsync(uc.CodigoEscola, cancellationToken);
        if (!escolaId.HasValue)
            return (false, null, "Acesso negado.", 403);

        var existente = await _calendario.ObterPorDataAsync(escolaId.Value, request.DataEvento, cancellationToken);
        if (existente is not null)
            return (false, null, "Ja existe evento para essa data.", 409);

        string tipoNormalizado;
        try
        {
            tipoNormalizado = CalendarioGeral.NormalizarTipoEvento(request.TipoEvento);
        }
        catch (DomainException ex)
        {
            return (false, null, ex.Message, 400);
        }

        if (CalendarioGeral.TipoSuspendeAula(tipoNormalizado))
        {
            var validar = await ValidarDataEventoSuspensivoAsync(
                escolaId.Value,
                request.DataEvento,
                tipoNormalizado,
                cancellationToken);
            if (validar.Error is not null)
                return (false, null, validar.Error, validar.StatusCode);
        }

        var entidade = new CalendarioGeral
        {
            EscolaId = escolaId.Value,
            DataEvento = request.DataEvento,
            TipoEvento = tipoNormalizado,
            Descricao = request.Descricao,
            SuspendeAula = CalendarioGeral.TipoSuspendeAula(tipoNormalizado),
            UsuarioId = uc.UserId
        };

        _calendario.Adicionar(entidade);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await RecalcularTurmasSeNecessarioAsync(escolaId.Value, entidade.SuspendeAula, cancellationToken);

        return (true, MapEvento(entidade), null, 201);
    }

    public async Task<(bool Success, EventoCalendarioResponse? Evento, string? Error, int StatusCode)> EditarAsync(
        int id,
        EditarEventoCalendarioRequest request,
        AppUserContext uc,
        CancellationToken cancellationToken = default)
    {
        var escolaId = await ObterIdEscolaAtivaPorCodigoAsync(uc.CodigoEscola, cancellationToken);
        if (!escolaId.HasValue)
            return (false, null, "Acesso negado.", 403);

        var entidade = await _calendario.ObterPorIdEEscolaAsync(id, escolaId.Value, cancellationToken);
        if (entidade is null)
            return (false, null, "Evento nao encontrado.", 404);

        var novaData = request.DataEvento ?? entidade.DataEvento;
        if (novaData != entidade.DataEvento)
        {
            var conflitoData = await _calendario.ObterPorDataAsync(escolaId.Value, novaData, cancellationToken);
            if (conflitoData is not null && conflitoData.Id != entidade.Id)
                return (false, null, "Ja existe evento para essa data.", 409);
        }

        string tipoEvento;
        try
        {
            tipoEvento = request.TipoEvento is null
                ? entidade.TipoEvento
                : CalendarioGeral.NormalizarTipoEvento(request.TipoEvento);
        }
        catch (DomainException ex)
        {
            return (false, null, ex.Message, 400);
        }

        var mudouTipoOuData = request.TipoEvento is not null || request.DataEvento.HasValue;
        if (CalendarioGeral.TipoSuspendeAula(tipoEvento) && mudouTipoOuData)
        {
            var validar = await ValidarDataEventoSuspensivoAsync(escolaId.Value, novaData, tipoEvento, cancellationToken);
            if (validar.Error is not null)
                return (false, null, validar.Error, validar.StatusCode);
        }

        entidade.DataEvento = novaData;
        entidade.TipoEvento = tipoEvento;
        if (request.Descricao is not null)
            entidade.Descricao = request.Descricao;
        entidade.SuspendeAula = CalendarioGeral.TipoSuspendeAula(tipoEvento);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await RecalcularTurmasSeNecessarioAsync(escolaId.Value, entidade.SuspendeAula, cancellationToken);

        return (true, MapEvento(entidade), null, 200);
    }

    public async Task<(bool Success, string? Error, int StatusCode)> ExcluirAsync(
        int id,
        AppUserContext uc,
        CancellationToken cancellationToken = default)
    {
        var escolaId = await ObterIdEscolaAtivaPorCodigoAsync(uc.CodigoEscola, cancellationToken);
        if (!escolaId.HasValue)
            return (false, "Acesso negado.", 403);

        var entidade = await _calendario.ObterPorIdEEscolaAsync(id, escolaId.Value, cancellationToken);
        if (entidade is null)
            return (false, "Evento nao encontrado.", 404);

        var suspendia = entidade.SuspendeAula;
        _calendario.Remover(entidade);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await RecalcularTurmasSeNecessarioAsync(escolaId.Value, suspendia, cancellationToken);

        return (true, null, 204);
    }

    private async Task RecalcularTurmasSeNecessarioAsync(
        int escolaId,
        bool deveRecalcular,
        CancellationToken cancellationToken)
    {
        if (!deveRecalcular)
        {
            return;
        }

        try
        {
            await _turmas.RecalcularTurmasEmAndamentoDaEscolaAsync(escolaId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Calendario salvo, mas falhou o recalculo de turmas em andamento da escola {EscolaId}",
                escolaId);
        }
    }

    private static EventoCalendarioResponse MapEvento(CalendarioGeral entidade) =>
        new()
        {
            Id = entidade.Id,
            DataEvento = entidade.DataEvento,
            TipoEvento = entidade.TipoEvento,
            Descricao = entidade.Descricao,
            SuspendeAula = entidade.SuspendeAula,
        };

    /// <summary>Sem aula, feriado e recesso: só a partir de amanhã; não pode haver compromisso ativo no dia.</summary>
    private async Task<(string? Error, int StatusCode)> ValidarDataEventoSuspensivoAsync(
        int escolaId,
        DateOnly dataEvento,
        string tipoNormalizado,
        CancellationToken cancellationToken)
    {
        var rotulo = RotuloTipoAmigavel(tipoNormalizado);
        var hoje = DateOnly.FromDateTime(DateTime.Now);
        if (dataEvento < hoje)
            return ($"Nao e permitido marcar {rotulo} em data anterior a hoje.", 400);

        if (dataEvento == hoje)
            return ($"Nao e permitido marcar {rotulo} no dia de hoje. Escolha a partir de amanha.", 400);

        if (await _compromissos.ExisteCompromissoAtivoNoDiaAsync(escolaId, dataEvento, cancellationToken))
        {
            return (
                $"Nao e possivel marcar {rotulo} nesta data: existem compromissos ativos nesse dia. Cancele ou reagende os compromissos antes de bloquear o dia no calendario.",
                409);
        }

        return (null, 200);
    }

    private static string RotuloTipoAmigavel(string tipoNormalizado)
    {
        if (string.Equals(tipoNormalizado, CalendarioGeral.TiposEvento.SemAula, StringComparison.OrdinalIgnoreCase))
            return "Sem aula";
        if (string.Equals(tipoNormalizado, CalendarioGeral.TiposEvento.Feriado, StringComparison.OrdinalIgnoreCase))
            return "Feriado";
        if (string.Equals(tipoNormalizado, CalendarioGeral.TiposEvento.Recesso, StringComparison.OrdinalIgnoreCase))
            return "Recesso";
        return tipoNormalizado;
    }

    private Task<int?> ObterIdEscolaAtivaPorCodigoAsync(string? codigoEscola, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(codigoEscola))
            return Task.FromResult<int?>(null);

        return _escolas.ObterIdAtivaPorCodigoEscolaAsync(codigoEscola, cancellationToken);
    }
}
