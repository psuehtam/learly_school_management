using Learly.Application.Contracts.Calendario.Requests;
using Learly.Application.Contracts.Calendario.Responses;
using Learly.Application.Services.Common;
using Learly.Domain.Entities;
using Learly.Domain.Exceptions;
using Learly.Domain.Interfaces.Persistence;
using Learly.Domain.Interfaces.Repositories;
using MapsterMapper;

namespace Learly.Application.Services.Calendario;

public sealed class CalendarioService : ICalendarioService
{
    private readonly ICalendarioGeralRepository _calendario;
    private readonly IEscolaRepository _escolas;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public CalendarioService(
        ICalendarioGeralRepository calendario,
        IEscolaRepository escolas,
        IUnitOfWork unitOfWork,
        IMapper mapper)
    {
        _calendario = calendario;
        _escolas = escolas;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
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

        return (true, _mapper.Map<EventoCalendarioResponse>(entidade), null, 201);
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

        entidade.DataEvento = novaData;
        entidade.TipoEvento = tipoEvento;
        if (request.Descricao is not null)
            entidade.Descricao = request.Descricao;
        entidade.SuspendeAula = CalendarioGeral.TipoSuspendeAula(tipoEvento);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return (true, _mapper.Map<EventoCalendarioResponse>(entidade), null, 200);
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

        _calendario.Remover(entidade);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return (true, null, 204);
    }

    private Task<int?> ObterIdEscolaAtivaPorCodigoAsync(string? codigoEscola, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(codigoEscola))
            return Task.FromResult<int?>(null);

        return _escolas.ObterIdAtivaPorCodigoEscolaAsync(codigoEscola, cancellationToken);
    }
}
