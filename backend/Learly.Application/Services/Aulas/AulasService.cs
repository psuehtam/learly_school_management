using Learly.Application.Contracts.Aulas;
using Learly.Application.Contracts.Aulas.Requests;
using Learly.Application.Contracts.Aulas.Responses;
using Learly.Application.Services.Common;
using Learly.Domain.Entities;
using Learly.Domain.Interfaces.Persistence;
using Learly.Domain.Interfaces.Repositories;
using MapsterMapper;

namespace Learly.Application.Services.Aulas;

public sealed partial class AulasService : IAulasService
{
    private static readonly HashSet<string> StatusValidos = new(StringComparer.OrdinalIgnoreCase)
    {
        Aula.Estados.Agendada,
        Aula.Estados.Realizada,
        Aula.Estados.Cancelada
    };

    private readonly IAulaRepository _aulas;
    private readonly IEscolaRepository _escolas;
    private readonly ICalendarioGeralRepository _calendario;
    private readonly ITurmaRepository _turmas;
    private readonly IUsuarioRepository _usuarios;
    private readonly IPresencaRepository _presencas;
    private readonly IHomeworkRepository _homeworks;
    private readonly IMatriculaRepository _matriculas;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public AulasService(
        IAulaRepository aulas,
        IEscolaRepository escolas,
        ICalendarioGeralRepository calendario,
        ITurmaRepository turmas,
        IUsuarioRepository usuarios,
        IPresencaRepository presencas,
        IHomeworkRepository homeworks,
        IMatriculaRepository matriculas,
        IUnitOfWork unitOfWork,
        IMapper mapper)
    {
        _aulas = aulas;
        _escolas = escolas;
        _calendario = calendario;
        _turmas = turmas;
        _usuarios = usuarios;
        _presencas = presencas;
        _homeworks = homeworks;
        _matriculas = matriculas;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<IReadOnlyList<AulaListItemResponse>> ListarAsync(
        AppUserContext uc,
        CancellationToken cancellationToken = default)
    {
        var escolaId = await ObterIdEscolaAtivaPorCodigoAsync(uc.CodigoEscola, cancellationToken);
        if (!escolaId.HasValue)
        {
            return [];
        }

        var filtroProfessor = EhProfessor(uc) ? uc.UserId : (int?)null;
        var entidades = await _aulas.ListarPorEscolaEFiltroProfessorAsync(
            escolaId.Value,
            filtroProfessor,
            cancellationToken);

        var list = entidades.Select(a => _mapper.Map<AulaListItemResponse>(a)).ToList();
        await EnriquecerAulasParaAgendaAsync(list, escolaId.Value, cancellationToken);

        return list;
    }

    public async Task<AulaListItemResponse?> ObterPorIdAsync(
        int id,
        AppUserContext uc,
        CancellationToken cancellationToken = default)
    {
        var escolaId = await ObterIdEscolaAtivaPorCodigoAsync(uc.CodigoEscola, cancellationToken);
        if (!escolaId.HasValue)
        {
            return null;
        }

        var filtroProfessor = EhProfessor(uc) ? uc.UserId : (int?)null;
        var entidade = await _aulas.ObterSemRastreioPorIdEEscolaAsync(id, escolaId.Value, filtroProfessor, cancellationToken);
        if (entidade is null)
        {
            return null;
        }

        var dto = _mapper.Map<AulaListItemResponse>(entidade);
        await EnriquecerAulasParaAgendaAsync([dto], escolaId.Value, cancellationToken);
        return dto;
    }

    public async Task<AulaCriacaoResultado> CriarAsync(
        CriarAulaRequest request,
        AppUserContext uc,
        CancellationToken cancellationToken = default)
    {
        var escolaId = await ObterIdEscolaAtivaPorCodigoAsync(uc.CodigoEscola, cancellationToken);
        if (!escolaId.HasValue)
        {
            return new AulaCriacaoResultado(false, null, "Acesso negado.", AulaCriacaoFalha.AcessoNegado);
        }

        if (request.NumeroAula <= 0)
        {
            return new AulaCriacaoResultado(false, null, "Numero da aula deve ser maior que zero.", AulaCriacaoFalha.Validacao);
        }

        if (request.HorarioFim <= request.HorarioInicio)
        {
            return new AulaCriacaoResultado(false, null, "Horario fim deve ser maior que horario inicio.", AulaCriacaoFalha.Validacao);
        }

        var tipoAulaSolicitada = string.IsNullOrWhiteSpace(request.TipoAula) ? "Normal" : request.TipoAula.Trim();
        var bloqueiaPorCalendario = await DeveBloquearPorCalendarioAsync(
            escolaId.Value,
            request.DataAula,
            tipoAulaSolicitada,
            cancellationToken);
        if (bloqueiaPorCalendario)
        {
            return new AulaCriacaoResultado(
                false,
                null,
                "Data marcada no calendario geral como sem aula para aulas e reposicoes.",
                AulaCriacaoFalha.Validacao);
        }

        var turma = await _turmas.ObterPorIdEEscolaAsync(request.TurmaId, escolaId.Value, cancellationToken);
        if (turma is null)
        {
            return new AulaCriacaoResultado(false, null, "Turma nao encontrada nesta escola.", AulaCriacaoFalha.Validacao);
        }

        var professorId = request.ProfessorId ?? uc.UserId;
        var professorValido = await _usuarios.ProfessorAtivoNaEscolaAsync(professorId, escolaId.Value, cancellationToken);
        if (!professorValido)
        {
            return new AulaCriacaoResultado(false, null, "Professor invalido para esta escola.", AulaCriacaoFalha.Validacao);
        }

        var entidade = _mapper.Map<Aula>(request);
        entidade.EscolaId = escolaId.Value;
        entidade.ProfessorId = professorId;
        entidade.Status = Aula.Estados.Agendada;

        _aulas.Adicionar(entidade);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return new AulaCriacaoResultado(true, entidade.Id, null, AulaCriacaoFalha.Nenhuma);
    }

    public async Task<AulaOperacaoResultado> EditarAsync(
        int id,
        EditarAulaRequest request,
        AppUserContext uc,
        CancellationToken cancellationToken = default)
    {
        if (EhProfessor(uc))
        {
            return new AulaOperacaoResultado(false, "Professor nao pode editar aulas.", 403);
        }

        var escolaId = await ObterIdEscolaAtivaPorCodigoAsync(uc.CodigoEscola, cancellationToken);
        if (!escolaId.HasValue)
        {
            return new AulaOperacaoResultado(false, "Acesso negado.", 403);
        }

        var aula = await _aulas.ObterRastreadaPorIdEEscolaAsync(id, escolaId.Value, cancellationToken);
        if (aula is null)
        {
            return new AulaOperacaoResultado(false, "Aula nao encontrada.", 404);
        }

        var horarioInicio = request.HorarioInicio ?? aula.HorarioInicio;
        var horarioFim = request.HorarioFim ?? aula.HorarioFim;
        if (horarioFim <= horarioInicio)
        {
            return new AulaOperacaoResultado(false, "Horario fim deve ser maior que horario inicio.", 400);
        }

        if (request.DataAula.HasValue)
        {
            var bloqueiaPorCalendario = await DeveBloquearPorCalendarioAsync(
                escolaId.Value,
                request.DataAula.Value,
                aula.TipoAula,
                cancellationToken);
            if (bloqueiaPorCalendario)
            {
                return new AulaOperacaoResultado(false, "Data marcada no calendario geral como sem aula para aulas e reposicoes.", 409);
            }
        }

        if (request.Status is not null)
        {
            var normalizedStatus = request.Status.Trim();
            if (!StatusValidos.Contains(normalizedStatus))
            {
                return new AulaOperacaoResultado(false, "Status da aula invalido.", 400);
            }

            if (!PodeTransicionar(aula.Status, normalizedStatus))
            {
                return new AulaOperacaoResultado(false, "Transicao de status nao permitida.", 409);
            }

            aula.Status = normalizedStatus;
        }

        if (request.CapituloId.HasValue) aula.CapituloId = request.CapituloId.Value;
        if (request.DataAula.HasValue) aula.DataAula = request.DataAula.Value;
        if (request.HorarioInicio.HasValue) aula.HorarioInicio = request.HorarioInicio.Value;
        if (request.HorarioFim.HasValue) aula.HorarioFim = request.HorarioFim.Value;
        if (request.ConteudoDado is not null) aula.ConteudoDado = request.ConteudoDado;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return new AulaOperacaoResultado(true, null, 204);
    }

    public async Task<AulaOperacaoResultado> CancelarAsync(int id, AppUserContext uc, CancellationToken cancellationToken = default)
    {
        if (EhProfessor(uc))
        {
            return new AulaOperacaoResultado(false, "Professor nao pode cancelar aulas.", 403);
        }

        var escolaId = await ObterIdEscolaAtivaPorCodigoAsync(uc.CodigoEscola, cancellationToken);
        if (!escolaId.HasValue)
        {
            return new AulaOperacaoResultado(false, "Acesso negado.", 403);
        }

        var aula = await _aulas.ObterRastreadaPorIdEEscolaAsync(id, escolaId.Value, cancellationToken);
        if (aula is null)
        {
            return new AulaOperacaoResultado(false, "Aula nao encontrada.", 404);
        }

        if (!PodeTransicionar(aula.Status, Aula.Estados.Cancelada))
        {
            return new AulaOperacaoResultado(false, "Transicao de status nao permitida.", 409);
        }

        aula.Status = Aula.Estados.Cancelada;
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return new AulaOperacaoResultado(true, null, 204);
    }

    private async Task EnriquecerAulasParaAgendaAsync(
        IReadOnlyList<AulaListItemResponse> itens,
        int escolaId,
        CancellationToken cancellationToken)
    {
        if (itens.Count == 0)
        {
            return;
        }

        var turmaIds = itens.Select(a => a.TurmaId).Distinct().ToList();
        var resumoTurmas = await _turmas.ObterResumoParaAgendaAsync(escolaId, turmaIds, cancellationToken);

        var idsReposicao = itens
            .Where(a => string.Equals(a.TipoAula, "Reposicao", StringComparison.OrdinalIgnoreCase))
            .Select(a => a.Id)
            .Distinct()
            .ToList();
        var ctxReposicao = await _aulas.ObterContextoReposicaoPorAulaIdsAsync(escolaId, idsReposicao, cancellationToken);

        foreach (var dto in itens)
        {
            if (resumoTurmas.TryGetValue(dto.TurmaId, out var tr))
            {
                dto.TurmaNome = tr.TurmaNome;
                dto.LivroNome = tr.LivroNome;
            }

            if (ctxReposicao.TryGetValue(dto.Id, out var rx))
            {
                dto.ReposicaoAlunoNome = rx.AlunoNomeCompleto;
                dto.ReposicaoAulaOriginalNumero = rx.NumeroAulaOriginal;
                dto.ReposicaoAulaOriginalData = rx.DataAulaOriginal;
            }
        }
    }

    private static bool EhProfessor(AppUserContext uc) =>
        string.Equals(uc.Perfil, "Professor", StringComparison.OrdinalIgnoreCase);

    private Task<int?> ObterIdEscolaAtivaPorCodigoAsync(string? codigoEscola, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(codigoEscola))
        {
            return Task.FromResult<int?>(null);
        }

        return _escolas.ObterIdAtivaPorCodigoEscolaAsync(codigoEscola, cancellationToken);
    }

    private static bool PodeTransicionar(string currentStatus, string nextStatus)
    {
        if (string.Equals(currentStatus, nextStatus, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return currentStatus.ToLowerInvariant() switch
        {
            "agendada" => string.Equals(nextStatus, Aula.Estados.Realizada, StringComparison.OrdinalIgnoreCase)
                          || string.Equals(nextStatus, Aula.Estados.Cancelada, StringComparison.OrdinalIgnoreCase),
            "realizada" => false,
            "cancelada" => false,
            _ => false
        };
    }

    private async Task<bool> DeveBloquearPorCalendarioAsync(
        int escolaId,
        DateOnly dataAula,
        string tipoAula,
        CancellationToken cancellationToken)
    {
        if (!string.Equals(tipoAula, "Normal", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(tipoAula, "Reposicao", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return await _calendario.DiaSuspendeAulaAsync(escolaId, dataAula, cancellationToken);
    }
}
