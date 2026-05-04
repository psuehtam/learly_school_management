using Learly.Application.Contracts.Compromissos.Requests;
using Learly.Application.Contracts.Compromissos.Responses;
using Learly.Application.Services.Common;
using Learly.Domain.Entities;
using Learly.Domain.Exceptions;
using Learly.Domain.Interfaces.Persistence;
using Learly.Domain.Interfaces.Repositories;

namespace Learly.Application.Services.Compromissos;

public sealed class CompromissosService : ICompromissosService
{
    private readonly ICompromissoRepository _compromissos;
    private readonly ICalendarioGeralRepository _calendario;
    private readonly IEscolaRepository _escolas;
    private readonly IUsuarioRepository _usuarios;
    private readonly IUnitOfWork _unitOfWork;

    public CompromissosService(
        ICompromissoRepository compromissos,
        ICalendarioGeralRepository calendario,
        IEscolaRepository escolas,
        IUsuarioRepository usuarios,
        IUnitOfWork unitOfWork)
    {
        _compromissos = compromissos;
        _calendario = calendario;
        _escolas = escolas;
        _usuarios = usuarios;
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<CompromissoResponse>> ListarMeusAsync(AppUserContext uc, CancellationToken cancellationToken = default)
    {
        var escolaId = await ObterEscolaIdAsync(uc, cancellationToken);
        if (!escolaId.HasValue)
            return [];

        var items = await _compromissos.ListarPorEscolaEUsuarioAsync(escolaId.Value, uc.UserId, cancellationToken);
        var result = new List<CompromissoResponse>(items.Count);
        foreach (var item in items)
        {
            var participantes = await _compromissos.ListarParticipantesIdsAsync(item.Id, cancellationToken);
            result.Add(ToResponse(item, participantes));
        }

        return result;
    }

    public async Task<IReadOnlyList<CompromissoResponse>> ListarAgendaGlobalAsync(
        DateOnly data,
        int? usuarioId,
        AppUserContext uc,
        CancellationToken cancellationToken = default)
    {
        var escolaId = await ObterEscolaIdAsync(uc, cancellationToken);
        if (!escolaId.HasValue)
            return [];

        var items = await _compromissos.ListarAgendaGlobalAsync(escolaId.Value, data, usuarioId, cancellationToken);
        var result = new List<CompromissoResponse>(items.Count);
        foreach (var item in items)
        {
            var participantes = await _compromissos.ListarParticipantesIdsAsync(item.Id, cancellationToken);
            result.Add(ToResponse(item, participantes));
        }

        return result;
    }

    public async Task<(bool Success, CompromissoResponse? Item, string? Error, int StatusCode)> CriarAsync(
        CriarCompromissoRequest request,
        AppUserContext uc,
        CancellationToken cancellationToken = default)
    {
        var escolaId = await ObterEscolaIdAsync(uc, cancellationToken);
        if (!escolaId.HasValue)
            return (false, null, "Acesso negado.", 403);

        var participantes = request.ParticipantesUsuarioIds.Where(id => id > 0).Distinct().ToList();
        if (participantes.Count == 0)
            return (false, null, "Selecione ao menos um participante para o compromisso.", 400);
        var validar = await ValidarRegrasAsync(
            escolaId.Value,
            request.DataInicio,
            request.DataFim,
            participantes,
            null,
            cancellationToken);
        if (validar is not null)
            return (false, null, validar, 409);

        Compromisso entidade;
        try
        {
            entidade = new Compromisso
            {
                EscolaId = escolaId.Value,
                UsuarioId = uc.UserId,
                Titulo = request.Titulo,
                Descricao = request.Descricao,
                DataInicio = request.DataInicio,
                DataFim = request.DataFim,
                Local = request.Local,
                Tipo = string.IsNullOrWhiteSpace(request.Tipo) ? "Outro" : request.Tipo.Trim(),
                Prioridade = string.IsNullOrWhiteSpace(request.Prioridade) ? "Media" : request.Prioridade.Trim(),
                Status = Compromisso.Statuses.Pendente,
                LembreteMinutos = request.LembreteMinutos,
                Cor = request.Cor
            };
            entidade.ValidarIntervalo();
        }
        catch (DomainException ex)
        {
            return (false, null, ex.Message, 400);
        }

        _compromissos.Adicionar(entidade);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _compromissos.DefinirParticipantesAsync(entidade.Id, participantes, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return (true, ToResponse(entidade, participantes), null, 201);
    }

    public async Task<(bool Success, CompromissoResponse? Item, string? Error, int StatusCode)> EditarAsync(
        int id,
        EditarCompromissoRequest request,
        AppUserContext uc,
        CancellationToken cancellationToken = default)
    {
        var escolaId = await ObterEscolaIdAsync(uc, cancellationToken);
        if (!escolaId.HasValue)
            return (false, null, "Acesso negado.", 403);

        var entidade = await _compromissos.ObterPorIdEEscolaAsync(id, escolaId.Value, cancellationToken);
        if (entidade is null)
            return (false, null, "Compromisso nao encontrado.", 404);

        if (string.Equals(entidade.Status, Compromisso.Statuses.Concluido, StringComparison.OrdinalIgnoreCase))
            return (false, null, "Compromisso concluido nao pode ser editado.", 409);

        var participantesAtuais = await _compromissos.ListarParticipantesIdsAsync(id, cancellationToken);
        var participantes = request.ParticipantesUsuarioIds?.Where(idp => idp > 0).Distinct().ToList()
                          ?? participantesAtuais.Where(idp => idp > 0).Distinct().ToList();
        if (participantes.Count == 0)
            return (false, null, "Selecione ao menos um participante para o compromisso.", 400);

        var novoInicio = request.DataInicio ?? entidade.DataInicio;
        var novoFim = request.DataFim ?? entidade.DataFim;

        var validar = await ValidarRegrasAsync(
            escolaId.Value,
            novoInicio,
            novoFim,
            participantes,
            entidade.Id,
            cancellationToken);
        if (validar is not null)
            return (false, null, validar, 409);

        entidade.Titulo = request.Titulo ?? entidade.Titulo;
        if (request.Descricao is not null) entidade.Descricao = request.Descricao;
        entidade.DataInicio = novoInicio;
        entidade.DataFim = novoFim;
        if (request.Local is not null) entidade.Local = request.Local;
        if (request.Tipo is not null) entidade.Tipo = request.Tipo;
        if (request.Prioridade is not null) entidade.Prioridade = request.Prioridade;
        if (request.Status is not null) entidade.Status = request.Status;
        if (request.LembreteMinutos.HasValue) entidade.LembreteMinutos = request.LembreteMinutos;
        if (request.Cor is not null) entidade.Cor = request.Cor;

        try
        {
            entidade.ValidarIntervalo();
        }
        catch (DomainException ex)
        {
            return (false, null, ex.Message, 400);
        }

        if (request.ParticipantesUsuarioIds is not null)
            await _compromissos.DefinirParticipantesAsync(entidade.Id, participantes, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return (true, ToResponse(entidade, participantes), null, 200);
    }

    public async Task<(bool Success, string? Error, int StatusCode)> CancelarAsync(
        int id,
        string motivo,
        AppUserContext uc,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(motivo))
            return (false, "Informe o motivo do cancelamento.", 400);

        var escolaId = await ObterEscolaIdAsync(uc, cancellationToken);
        if (!escolaId.HasValue)
            return (false, "Acesso negado.", 403);

        var entidade = await _compromissos.ObterPorIdEEscolaAsync(id, escolaId.Value, cancellationToken);
        if (entidade is null)
            return (false, "Compromisso nao encontrado.", 404);

        var motivoLimpo = motivo.Trim();
        var carimbo = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
        var anotacao = $"[CANCELADO EM {carimbo} UTC] Motivo: {motivoLimpo}";
        entidade.Descricao = string.IsNullOrWhiteSpace(entidade.Descricao)
            ? anotacao
            : $"{entidade.Descricao}\n{anotacao}";
        entidade.Status = Compromisso.Statuses.Cancelado;
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return (true, null, 204);
    }

    private async Task<string?> ValidarRegrasAsync(
        int escolaId,
        DateTime dataInicio,
        DateTime dataFim,
        IReadOnlyCollection<int> participantes,
        int? compromissoIgnoradoId,
        CancellationToken cancellationToken)
    {
        if (dataFim <= dataInicio)
            return "Data fim deve ser maior que data inicio.";

        if (DateOnly.FromDateTime(dataInicio) != DateOnly.FromDateTime(dataFim))
            return "Compromisso deve iniciar e terminar no mesmo dia.";

        var data = DateOnly.FromDateTime(dataInicio);
        if (await _calendario.DiaSuspendeAulaAsync(escolaId, data, cancellationToken))
            return "Nao e permitido criar compromisso em dia sem aula, feriado ou recesso.";

        var usuariosDaEscola = await _usuarios.ListarPorEscolaAsync(escolaId, cancellationToken);
        var idsValidos = usuariosDaEscola.Where(u => u.Status == Usuario.Estados.Ativo).Select(u => u.Id).ToHashSet();
        if (participantes.Any(p => !idsValidos.Contains(p)))
            return "Um ou mais participantes sao invalidos para esta escola.";

        if (await _compromissos.ExisteConflitoCompromissoAsync(
                escolaId,
                participantes,
                dataInicio,
                dataFim,
                compromissoIgnoradoId,
                cancellationToken))
        {
            return "Conflito de horario: um dos participantes ja possui compromisso neste periodo.";
        }

        if (await _compromissos.ExisteConflitoAulaProfessorAsync(escolaId, participantes, dataInicio, dataFim, cancellationToken))
            return "Conflito de horario: professor possui aula ou reposicao neste periodo.";

        return null;
    }

    private Task<int?> ObterEscolaIdAsync(AppUserContext uc, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(uc.CodigoEscola))
            return Task.FromResult<int?>(null);
        return _escolas.ObterIdAtivaPorCodigoEscolaAsync(uc.CodigoEscola, cancellationToken);
    }

    private static CompromissoResponse ToResponse(Compromisso c, IReadOnlyCollection<int> participantes) => new()
    {
        Id = c.Id,
        Titulo = c.Titulo,
        Descricao = c.Descricao,
        DataInicio = c.DataInicio,
        DataFim = c.DataFim,
        Local = c.Local,
        Tipo = c.Tipo,
        Prioridade = c.Prioridade,
        Status = c.Status,
        LembreteMinutos = c.LembreteMinutos,
        Cor = c.Cor,
        ParticipantesUsuarioIds = participantes.ToArray()
    };
}
