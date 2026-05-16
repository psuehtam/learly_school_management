using Learly.Application.Contracts.Turmas;
using Learly.Application.Contracts.Turmas.Requests;
using Learly.Application.Contracts.Turmas.Responses;
using Learly.Application.Services.Common;
using Learly.Domain.Entities;
using Learly.Domain.Exceptions;
using Learly.Domain.Interfaces.Persistence;
using Learly.Domain.Interfaces.Repositories;
using Learly.Domain.ReadModels;
namespace Learly.Application.Services.Turmas;

public sealed partial class TurmasService : ITurmasService
{
    private readonly ITurmaRepository _turmas;
    private readonly ILivroCatalogoRepository _livros;
    private readonly IUsuarioRepository _usuarios;
    private readonly ICalendarioGeralRepository _calendario;
    private readonly IAulaRepository _aulas;
    private readonly IEscolaRepository _escolas;
    private readonly IEscolaHorarioFuncionamentoRepository _horariosFuncionamento;
    private readonly IAvaliacaoRepository _avaliacoes;
    private readonly IMatriculaRepository _matriculas;
    private readonly IUnitOfWork _unitOfWork;

    private static readonly string[] NomesDiaSemana =
    [
        "domingo", "segunda-feira", "terca-feira", "quarta-feira", "quinta-feira", "sexta-feira", "sabado"
    ];

    public TurmasService(
        ITurmaRepository turmas,
        ILivroCatalogoRepository livros,
        IUsuarioRepository usuarios,
        ICalendarioGeralRepository calendario,
        IAulaRepository aulas,
        IEscolaRepository escolas,
        IEscolaHorarioFuncionamentoRepository horariosFuncionamento,
        IAvaliacaoRepository avaliacoes,
        IMatriculaRepository matriculas,
        IUnitOfWork unitOfWork)
    {
        _turmas = turmas;
        _livros = livros;
        _usuarios = usuarios;
        _calendario = calendario;
        _aulas = aulas;
        _escolas = escolas;
        _horariosFuncionamento = horariosFuncionamento;
        _avaliacoes = avaliacoes;
        _matriculas = matriculas;
        _unitOfWork = unitOfWork;
    }

    public async Task<TurmasListagemResultado> ListarAsync(
        string? status,
        AppUserContext uc,
        CancellationToken cancellationToken = default)
    {
        var escolaId = await ObterEscolaIdAsync(uc, cancellationToken);
        if (!escolaId.HasValue)
        {
            return new TurmasListagemResultado(false, [], "Acesso negado.", TurmasFalha.AcessoNegado);
        }

        string? statusNorm = null;
        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!Turma.Estados.IsValid(status))
            {
                return new TurmasListagemResultado(false, [], "Status invalido.", TurmasFalha.Validacao);
            }

            statusNorm = Turma.Estados.Normalize(status);
        }

        var filtroProfessor = FiltroProfessorId(uc);
        var rows = await _turmas.ListarDetalhadoPorEscolaAsync(
            escolaId.Value,
            statusNorm,
            filtroProfessor,
            cancellationToken);
        var itens = new List<TurmaResponse>(rows.Count);
        foreach (var row in rows)
        {
            var (_, totalAulas) = await _livros.ObterTotaisCapitulosPorLivroAsync(row.LivroId, escolaId.Value, cancellationToken);
            itens.Add(MapResponse(row, escolaId.Value, totalAulas));
        }

        return new TurmasListagemResultado(true, itens, null, TurmasFalha.Nenhuma);
    }

    public async Task<TurmaDetalheResultado> ObterPorIdAsync(
        int id,
        AppUserContext uc,
        CancellationToken cancellationToken = default)
    {
        var escolaId = await ObterEscolaIdAsync(uc, cancellationToken);
        if (!escolaId.HasValue)
        {
            return new TurmaDetalheResultado(false, null, "Acesso negado.", TurmasFalha.AcessoNegado);
        }

        var filtroProfessor = FiltroProfessorId(uc);
        var rows = await _turmas.ListarDetalhadoPorEscolaAsync(
            escolaId.Value,
            null,
            filtroProfessor,
            cancellationToken);
        var row = rows.FirstOrDefault(r => r.Id == id);
        if (row is null)
        {
            return new TurmaDetalheResultado(false, null, "Turma nao encontrada.", TurmasFalha.NaoEncontrado);
        }

        var (_, totalAulas) = await _livros.ObterTotaisCapitulosPorLivroAsync(row.LivroId, escolaId.Value, cancellationToken);
        return new TurmaDetalheResultado(true, MapResponse(row, escolaId.Value, totalAulas), null, TurmasFalha.Nenhuma);
    }

    public async Task<TurmaOperacaoResultado> CriarAsync(
        CriarTurmaRequest request,
        AppUserContext uc,
        CancellationToken cancellationToken = default)
    {
        var escolaId = await ObterEscolaIdAsync(uc, cancellationToken);
        if (!escolaId.HasValue)
        {
            return Falha("Acesso negado.", TurmasFalha.AcessoNegado, 403);
        }

        if (request.ProfessorId <= 0 || request.LivroId <= 0)
        {
            return Falha("Professor e livro sao obrigatorios.", TurmasFalha.Validacao);
        }

        var livro = await _livros.ObterPorIdEscolaAsync(request.LivroId, escolaId.Value, cancellationToken);
        if (livro is null || !string.Equals(livro.Status, "Ativo", StringComparison.OrdinalIgnoreCase))
        {
            return Falha("Livro nao encontrado ou inativo.", TurmasFalha.Validacao);
        }

        var professorOk = await _usuarios.ProfessorAtivoNaEscolaAsync(request.ProfessorId, escolaId.Value, cancellationToken);
        if (!professorOk)
        {
            return Falha("Professor invalido para esta escola.", TurmasFalha.Validacao);
        }

        TimeOnly? hi = null;
        TimeOnly? hf = null;
        try
        {
            if (!string.IsNullOrWhiteSpace(request.HorarioInicio))
            {
                hi = TimeOnly.Parse(request.HorarioInicio.Trim());
            }

            if (!string.IsNullOrWhiteSpace(request.HorarioFim))
            {
                hf = TimeOnly.Parse(request.HorarioFim.Trim());
            }
        }
        catch (FormatException)
        {
            return Falha("Horario invalido. Use formato HH:mm.", TurmasFalha.Validacao);
        }

        if (hi is not null && hf is not null && hf <= hi)
        {
            return Falha("Horario fim deve ser maior que horario inicio.", TurmasFalha.Validacao);
        }

        if (request.DiasSemana is { Count: > 0 } && (hi is not null || hf is not null))
        {
            if (hi is null || hf is null)
            {
                return Falha("Informe horario de inicio e termino ao definir dias da semana.", TurmasFalha.Validacao);
            }

            var diasCriacao = NormalizarDiasSemana(request.DiasSemana);
            if (diasCriacao.Count == 0)
            {
                return Falha("Dias da semana invalidos.", TurmasFalha.Validacao);
            }

            var erroFuncionamento = await ValidarHorarioDentroFuncionamentoAsync(
                escolaId.Value,
                diasCriacao,
                hi.Value,
                hf.Value,
                cancellationToken);
            if (erroFuncionamento is not null)
            {
                return Falha(erroFuncionamento, TurmasFalha.Validacao);
            }
        }

        var nomeProvisorio = $"Turma {livro.Nome} - EM ESPERA";
        var turma = new Turma
        {
            EscolaId = escolaId.Value,
            ProfessorId = request.ProfessorId,
            LivroId = request.LivroId,
            Nome = nomeProvisorio,
            Sala = request.Sala,
            Observacoes = request.Observacoes,
            Status = Turma.Estados.EmEspera
        };
        turma.DefinirHorarios(hi, hf);

        _turmas.Adicionar(turma);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        if (request.DiasSemana is { Count: > 0 })
        {
            var dias = NormalizarDiasSemana(request.DiasSemana);
            if (dias.Count == 0)
            {
                return Falha("Dias da semana invalidos.", TurmasFalha.Validacao);
            }

            await _turmas.SubstituirDiasSemanaAsync(escolaId.Value, turma.Id, dias, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return await ObterOperacaoDetalheAsync(turma.Id, escolaId.Value, cancellationToken);
    }

    public async Task<TurmaOperacaoResultado> AtualizarAsync(
        int id,
        AtualizarTurmaRequest request,
        AppUserContext uc,
        CancellationToken cancellationToken = default)
    {
        var escolaId = await ObterEscolaIdAsync(uc, cancellationToken);
        if (!escolaId.HasValue)
        {
            return Falha("Acesso negado.", TurmasFalha.AcessoNegado, 403);
        }

        var turma = await _turmas.ObterRastreadaPorIdEEscolaAsync(id, escolaId.Value, cancellationToken);
        if (turma is null)
        {
            return Falha("Turma nao encontrada.", TurmasFalha.NaoEncontrado, 404);
        }

        if (string.Equals(turma.Status, Turma.Estados.Concluida, StringComparison.Ordinal)
            || string.Equals(turma.Status, Turma.Estados.Inativa, StringComparison.Ordinal)
            || string.Equals(turma.Status, Turma.Estados.Cancelada, StringComparison.Ordinal))
        {
            return Falha("Turmas concluidas, inativas ou canceladas nao podem ser editadas.", TurmasFalha.Validacao);
        }

        var emAndamento = string.Equals(turma.Status, Turma.Estados.EmAndamento, StringComparison.Ordinal);
        var emEspera = string.Equals(turma.Status, Turma.Estados.EmEspera, StringComparison.Ordinal);

        if (emAndamento && (request.LivroId.HasValue || request.DiasSemana is { Count: > 0 }
            || !string.IsNullOrWhiteSpace(request.HorarioInicio) || !string.IsNullOrWhiteSpace(request.HorarioFim)))
        {
            return Falha(
                "Livro, dias da semana e horarios nao podem ser alterados em turma em andamento. Use remanejamento.",
                TurmasFalha.Validacao);
        }

        if (request.LivroId.HasValue && request.LivroId.Value != turma.LivroId)
        {
            if (!emEspera)
            {
                return Falha("Livro so pode ser alterado em turmas em espera.", TurmasFalha.Validacao);
            }

            var livro = await _livros.ObterPorIdEscolaAsync(request.LivroId.Value, escolaId.Value, cancellationToken);
            if (livro is null || !string.Equals(livro.Status, "Ativo", StringComparison.OrdinalIgnoreCase))
            {
                return Falha("Livro nao encontrado ou inativo.", TurmasFalha.Validacao);
            }

            turma.LivroId = request.LivroId.Value;
        }

        var professorAlterado = false;
        if (request.ProfessorId.HasValue && request.ProfessorId.Value != turma.ProfessorId)
        {
            var professorOk = await _usuarios.ProfessorAtivoNaEscolaAsync(
                request.ProfessorId.Value,
                escolaId.Value,
                cancellationToken);
            if (!professorOk)
            {
                return Falha("Professor invalido para esta escola.", TurmasFalha.Validacao);
            }

            turma.ProfessorId = request.ProfessorId.Value;
            professorAlterado = true;
        }

        if (request.Observacoes is not null)
        {
            turma.Observacoes = request.Observacoes;
        }

        if (request.Sala is not null)
        {
            turma.Sala = request.Sala;
        }

        if (emEspera && request.DiasSemana is { Count: > 0 })
        {
            var dias = NormalizarDiasSemana(request.DiasSemana);
            if (dias.Count == 0)
            {
                return Falha("Dias da semana invalidos.", TurmasFalha.Validacao);
            }

            await _turmas.SubstituirDiasSemanaAsync(escolaId.Value, id, dias, cancellationToken);
        }

        try
        {
            if (emEspera && (!string.IsNullOrWhiteSpace(request.HorarioInicio) || !string.IsNullOrWhiteSpace(request.HorarioFim)))
            {
                var hi = string.IsNullOrWhiteSpace(request.HorarioInicio)
                    ? turma.Horario
                    : TimeOnly.Parse(request.HorarioInicio.Trim());
                var hf = string.IsNullOrWhiteSpace(request.HorarioFim)
                    ? turma.HorarioFim
                    : TimeOnly.Parse(request.HorarioFim.Trim());
                turma.DefinirHorarios(hi, hf);
            }
        }
        catch (FormatException)
        {
            return Falha("Horario invalido. Use formato HH:mm.", TurmasFalha.Validacao);
        }
        catch (DomainException ex)
        {
            return Falha(ex.Message, TurmasFalha.Validacao);
        }

        if (emEspera && turma.Horario is not null && turma.HorarioFim is not null)
        {
            var diasAtualizados = request.DiasSemana is { Count: > 0 }
                ? NormalizarDiasSemana(request.DiasSemana)
                : await _turmas.ListarDiasSemanaAsync(id, cancellationToken);

            if (diasAtualizados.Count > 0)
            {
                var erroFuncionamento = await ValidarHorarioDentroFuncionamentoAsync(
                    escolaId.Value,
                    diasAtualizados,
                    turma.Horario.Value,
                    turma.HorarioFim.Value,
                    cancellationToken);
                if (erroFuncionamento is not null)
                {
                    return Falha(erroFuncionamento, TurmasFalha.Validacao);
                }
            }
        }

        if (emAndamento && turma.Horario is not null && turma.HorarioFim is not null)
        {
            var diasSemana = await _turmas.ListarDiasSemanaAsync(id, cancellationToken);
            var professorId = turma.ProfessorId;
            foreach (var dia in diasSemana)
            {
                var conflito = await _turmas.ExisteConflitoHorarioProfessorAsync(
                    escolaId.Value,
                    professorId,
                    dia,
                    turma.Horario.Value,
                    turma.HorarioFim.Value,
                    id,
                    cancellationToken);
                if (conflito)
                {
                    return Falha(
                        "Professor ja possui turma em andamento no mesmo dia e horario.",
                        TurmasFalha.Conflito,
                        409);
                }
            }
        }

        if (emAndamento && professorAlterado)
        {
            await _turmas.AtualizarProfessorAulasAgendadasAsync(id, turma.ProfessorId, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return await ObterOperacaoDetalheAsync(id, escolaId.Value, cancellationToken);
    }

    public async Task<TurmaOperacaoResultado> AtivarAsync(
        int id,
        AtivarTurmaRequest request,
        AppUserContext uc,
        CancellationToken cancellationToken = default)
    {
        var escolaId = await ObterEscolaIdAsync(uc, cancellationToken);
        if (!escolaId.HasValue)
        {
            return Falha("Acesso negado.", TurmasFalha.AcessoNegado, 403);
        }

        var turma = await _turmas.ObterRastreadaPorIdEEscolaAsync(id, escolaId.Value, cancellationToken);
        if (turma is null)
        {
            return Falha("Turma nao encontrada.", TurmasFalha.NaoEncontrado, 404);
        }

        if (!string.Equals(turma.Status, Turma.Estados.EmEspera, StringComparison.Ordinal))
        {
            return Falha("Somente turmas em espera podem ser ativadas.", TurmasFalha.Validacao);
        }

        var ativos = await _turmas.ContarMatriculasAtivasAsync(id, cancellationToken);
        if (ativos < Turma.MinimoAlunosParaAtivar)
        {
            return Falha($"A turma precisa de no minimo {Turma.MinimoAlunosParaAtivar} alunos matriculados (ativos). Atual: {ativos}.", TurmasFalha.Validacao);
        }

        if (request.DiasSemana is { Count: > 0 })
        {
            var dias = NormalizarDiasSemana(request.DiasSemana);
            if (dias.Count == 0)
            {
                return Falha("Dias da semana invalidos.", TurmasFalha.Validacao);
            }

            await _turmas.SubstituirDiasSemanaAsync(escolaId.Value, id, dias, cancellationToken);
        }

        if (!string.IsNullOrWhiteSpace(request.Sala))
        {
            turma.Sala = request.Sala;
        }

        try
        {
            if (!string.IsNullOrWhiteSpace(request.HorarioInicio) || !string.IsNullOrWhiteSpace(request.HorarioFim))
            {
                var hi = string.IsNullOrWhiteSpace(request.HorarioInicio)
                    ? turma.Horario
                    : TimeOnly.Parse(request.HorarioInicio.Trim());
                var hf = string.IsNullOrWhiteSpace(request.HorarioFim)
                    ? turma.HorarioFim
                    : TimeOnly.Parse(request.HorarioFim.Trim());
                turma.DefinirHorarios(hi, hf);
            }
        }
        catch (FormatException)
        {
            return Falha("Horario invalido. Use formato HH:mm.", TurmasFalha.Validacao);
        }
        catch (DomainException ex)
        {
            return Falha(ex.Message, TurmasFalha.Validacao);
        }

        if (turma.Horario is null || turma.HorarioFim is null)
        {
            return Falha("Informe horario de inicio e termino antes de ativar.", TurmasFalha.Validacao);
        }

        var diasSemana = await _turmas.ListarDiasSemanaAsync(id, cancellationToken);
        if (diasSemana.Count == 0)
        {
            return Falha("Informe ao menos um dia da semana antes de ativar.", TurmasFalha.Validacao);
        }

        var hoje = DateOnly.FromDateTime(DateTime.Now);
        if (request.DataInicio < hoje)
        {
            return Falha("Data de inicio nao pode ser anterior a hoje.", TurmasFalha.Validacao);
        }

        var dowInicio = (int)request.DataInicio.DayOfWeek;
        if (!diasSemana.Contains(dowInicio))
        {
            return Falha("Data de inicio deve coincidir com um dos dias da semana da turma.", TurmasFalha.Validacao);
        }

        var erroFuncionamentoAtivar = await ValidarHorarioDentroFuncionamentoAsync(
            escolaId.Value,
            diasSemana,
            turma.Horario.Value,
            turma.HorarioFim.Value,
            cancellationToken);
        if (erroFuncionamentoAtivar is not null)
        {
            return Falha(erroFuncionamentoAtivar, TurmasFalha.Validacao);
        }

        foreach (var dia in diasSemana)
        {
            var conflito = await _turmas.ExisteConflitoHorarioProfessorAsync(
                escolaId.Value,
                turma.ProfessorId,
                dia,
                turma.Horario.Value,
                turma.HorarioFim.Value,
                id,
                cancellationToken);
            if (conflito)
            {
                return Falha("Professor ja possui turma em andamento no mesmo dia e horario.", TurmasFalha.Conflito, 409);
            }
        }

        var livro = await _livros.ObterPorIdEscolaComCapitulosAsync(turma.LivroId, escolaId.Value, cancellationToken);
        if (livro is null)
        {
            return Falha("Livro da turma nao encontrado.", TurmasFalha.Validacao);
        }

        var capitulos = livro.Capitulos
            .Where(c => string.Equals(c.Status, "Ativo", StringComparison.OrdinalIgnoreCase))
            .OrderBy(c => c.Id)
            .ToList();

        if (capitulos.Count == 0)
        {
            return Falha("Livro sem capitulos ativos para gerar aulas.", TurmasFalha.Validacao);
        }

        var suspendeCache = new Dictionary<DateOnly, bool>();
        async Task<bool> DiaSuspendeAsync(DateOnly d)
        {
            if (!suspendeCache.TryGetValue(d, out var s))
            {
                s = await _calendario.DiaSuspendeAulaAsync(escolaId.Value, d, cancellationToken);
                suspendeCache[d] = s;
            }

            return s;
        }

        TurmaAulaGerador.ResultadoGeracao geracao;
        try
        {
            geracao = await GerarComCalendarioAsync(
                request.DataInicio,
                diasSemana,
                capitulos,
                DiaSuspendeAsync);
        }
        catch (InvalidOperationException ex)
        {
            return Falha(ex.Message, TurmasFalha.Validacao);
        }

        await _turmas.RemoverAulasAgendadasDaTurmaAsync(id, cancellationToken);
        await _turmas.RemoverProgressoCapitulosAsync(id, cancellationToken);

        foreach (var planejada in geracao.Aulas)
        {
            var aula = new Aula
            {
                EscolaId = escolaId.Value,
                TurmaId = id,
                CapituloId = planejada.CapituloId,
                ProfessorId = turma.ProfessorId,
                NumeroAula = planejada.NumeroAula,
                DataAula = planejada.DataAula,
                HorarioInicio = turma.Horario.Value,
                HorarioFim = turma.HorarioFim.Value,
                TipoAula = "Normal",
                Status = Aula.Estados.Agendada
            };
            _aulas.Adicionar(aula);
        }

        await _turmas.InicializarProgressoCapitulosAsync(
            escolaId.Value,
            id,
            capitulos.Select(c => c.Id).ToList(),
            cancellationToken);

        var seq = await _turmas.ContarTurmasPorLivroAsync(escolaId.Value, turma.LivroId, cancellationToken);
        turma.Nome = TurmaAulaGerador.MontarNomeTurma(livro.Nome, seq, diasSemana, turma.Horario.Value);
        turma.DefinirPeriodoLetivo(request.DataInicio, geracao.DataTerminoPrevista);
        turma.TransicionarStatus(Turma.Estados.EmAndamento);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return await ObterOperacaoDetalheAsync(id, escolaId.Value, cancellationToken);
    }

    public async Task<TurmaOperacaoResultado> ConcluirAsync(
        int id,
        AppUserContext uc,
        CancellationToken cancellationToken = default)
    {
        var escolaId = await ObterEscolaIdAsync(uc, cancellationToken);
        if (!escolaId.HasValue)
        {
            return Falha("Acesso negado.", TurmasFalha.AcessoNegado, 403);
        }

        var turma = await _turmas.ObterRastreadaPorIdEEscolaAsync(id, escolaId.Value, cancellationToken);
        if (turma is null)
        {
            return Falha("Turma nao encontrada.", TurmasFalha.NaoEncontrado, 404);
        }

        if (!string.Equals(turma.Status, Turma.Estados.EmAndamento, StringComparison.Ordinal))
        {
            return Falha("Somente turmas em andamento podem ser concluidas.", TurmasFalha.Validacao);
        }

        var hoje = DateOnly.FromDateTime(DateTime.Now);
        var prazoOk = turma.DataTerminoPrevista is not null && turma.DataTerminoPrevista.Value <= hoje;
        var capitulosOk = await _turmas.TodosCapitulosConcluidosAsync(id, cancellationToken);

        if (!prazoOk)
        {
            return Falha("A data prevista de termino da turma ainda nao foi atingida.", TurmasFalha.Validacao);
        }

        if (!capitulosOk)
        {
            return Falha("Todos os capitulos do livro devem estar marcados como concluidos nesta turma.", TurmasFalha.Validacao);
        }

        turma.TransicionarStatus(Turma.Estados.Concluida);
        await _matriculas.EncerrarMatriculasAtivasDaTurmaAsync(
            escolaId.Value,
            id,
            Matricula.Estados.Concluido,
            cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return await ObterOperacaoDetalheAsync(id, escolaId.Value, cancellationToken);
    }

    public async Task<TurmaOperacaoResultado> InativarAsync(
        int id,
        AppUserContext uc,
        CancellationToken cancellationToken = default)
    {
        var escolaId = await ObterEscolaIdAsync(uc, cancellationToken);
        if (!escolaId.HasValue)
        {
            return Falha("Acesso negado.", TurmasFalha.AcessoNegado, 403);
        }

        var turma = await _turmas.ObterRastreadaPorIdEEscolaAsync(id, escolaId.Value, cancellationToken);
        if (turma is null)
        {
            return Falha("Turma nao encontrada.", TurmasFalha.NaoEncontrado, 404);
        }

        if (string.Equals(turma.Status, Turma.Estados.Inativa, StringComparison.Ordinal))
        {
            return Falha("Turma ja esta inativa.", TurmasFalha.Validacao);
        }

        turma.TransicionarStatus(Turma.Estados.Inativa);
        await _matriculas.EncerrarMatriculasAtivasDaTurmaAsync(
            escolaId.Value,
            id,
            Matricula.Estados.Cancelado,
            cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return await ObterOperacaoDetalheAsync(id, escolaId.Value, cancellationToken);
    }

    public async Task RecalcularTurmasEmAndamentoDaEscolaAsync(int escolaId, CancellationToken cancellationToken = default)
    {
        var ids = await _turmas.ListarIdsTurmasEmAndamentoAsync(escolaId, cancellationToken);
        foreach (var turmaId in ids)
        {
            try
            {
                await RecalcularTurmaInternoAsync(escolaId, turmaId, cancellationToken);
            }
            catch (Exception)
            {
                // Uma turma com dados inconsistentes nao deve impedir as demais.
                continue;
            }
        }
    }

    private async Task RecalcularTurmaInternoAsync(int escolaId, int turmaId, CancellationToken cancellationToken)
    {
        var turma = await _turmas.ObterRastreadaPorIdEEscolaAsync(turmaId, escolaId, cancellationToken);
        if (turma is null
            || !string.Equals(turma.Status, Turma.Estados.EmAndamento, StringComparison.Ordinal)
            || turma.DataInicio is null
            || turma.Horario is null
            || turma.HorarioFim is null)
        {
            return;
        }

        var diasSemana = await _turmas.ListarDiasSemanaAsync(turmaId, cancellationToken);
        if (diasSemana.Count == 0)
        {
            return;
        }

        var livro = await _livros.ObterPorIdEscolaComCapitulosAsync(turma.LivroId, escolaId, cancellationToken);
        if (livro is null)
        {
            return;
        }

        var capitulos = livro.Capitulos
            .Where(c => string.Equals(c.Status, "Ativo", StringComparison.OrdinalIgnoreCase))
            .OrderBy(c => c.Id)
            .ToList();

        var suspendeCache = new Dictionary<DateOnly, bool>();
        async Task<bool> DiaSuspendeAsync(DateOnly d)
        {
            if (!suspendeCache.TryGetValue(d, out var s))
            {
                s = await _calendario.DiaSuspendeAulaAsync(escolaId, d, cancellationToken);
                suspendeCache[d] = s;
            }

            return s;
        }

        TurmaAulaGerador.ResultadoGeracao geracao;
        try
        {
            geracao = await GerarComCalendarioAsync(
                turma.DataInicio.Value,
                diasSemana,
                capitulos,
                DiaSuspendeAsync);
        }
        catch (InvalidOperationException)
        {
            return;
        }

        await _turmas.RemoverAulasAgendadasDaTurmaAsync(turmaId, cancellationToken);

        foreach (var planejada in geracao.Aulas)
        {
            var aula = new Aula
            {
                EscolaId = escolaId,
                TurmaId = turmaId,
                CapituloId = planejada.CapituloId,
                ProfessorId = turma.ProfessorId,
                NumeroAula = planejada.NumeroAula,
                DataAula = planejada.DataAula,
                HorarioInicio = turma.Horario.Value,
                HorarioFim = turma.HorarioFim.Value,
                TipoAula = "Normal",
                Status = Aula.Estados.Agendada
            };
            _aulas.Adicionar(aula);
        }

        turma.DefinirPeriodoLetivo(turma.DataInicio, geracao.DataTerminoPrevista);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private static async Task<TurmaAulaGerador.ResultadoGeracao> GerarComCalendarioAsync(
        DateOnly dataInicio,
        IReadOnlyList<int> diasSemana,
        IReadOnlyList<Capitulo> capitulos,
        Func<DateOnly, Task<bool>> diaSuspendeAsync)
    {
        var slotsNecessarios = capitulos.Sum(c => c.QtdAulasPrevistas);
        if (slotsNecessarios == 0)
        {
            return new TurmaAulaGerador.ResultadoGeracao([], null);
        }

        var diasSet = new HashSet<int>(diasSemana);
        var datasValidas = new List<DateOnly>();
        var cursor = dataInicio;
        const int maxDias = 365 * 3;

        for (var i = 0; i < maxDias && datasValidas.Count < slotsNecessarios; i++)
        {
            var dow = (int)cursor.DayOfWeek;
            if (diasSet.Contains(dow) && !await diaSuspendeAsync(cursor))
            {
                datasValidas.Add(cursor);
            }

            cursor = cursor.AddDays(1);
        }

        if (datasValidas.Count < slotsNecessarios)
        {
            throw new InvalidOperationException(
                $"Nao foi possivel alocar {slotsNecessarios} aulas (encontradas {datasValidas.Count} datas validas).");
        }

        var aulas = new List<TurmaAulaGerador.AulaPlanejada>(slotsNecessarios);
        var indiceData = 0;
        var numeroAula = 1;
        foreach (var cap in capitulos)
        {
            for (var n = 0; n < cap.QtdAulasPrevistas; n++)
            {
                aulas.Add(new TurmaAulaGerador.AulaPlanejada(cap.Id, numeroAula++, datasValidas[indiceData++]));
            }
        }

        var termino = aulas.Count > 0 ? aulas[^1].DataAula : (DateOnly?)null;
        return new TurmaAulaGerador.ResultadoGeracao(aulas, termino);
    }

    private async Task<TurmaOperacaoResultado> ObterOperacaoDetalheAsync(
        int turmaId,
        int escolaId,
        CancellationToken cancellationToken)
    {
        var rows = await _turmas.ListarDetalhadoPorEscolaAsync(escolaId, null, professorId: null, cancellationToken);
        var row = rows.FirstOrDefault(r => r.Id == turmaId);
        if (row is null)
        {
            return Falha("Turma nao encontrada apos operacao.", TurmasFalha.NaoEncontrado, 404);
        }

        var (_, totalAulas) = await _livros.ObterTotaisCapitulosPorLivroAsync(row.LivroId, escolaId, cancellationToken);
        return new TurmaOperacaoResultado(true, MapResponse(row, escolaId, totalAulas), null, TurmasFalha.Nenhuma, 200);
    }

    private static TurmaResponse MapResponse(TurmaListagemItem row, int escolaId, int totalAulasLivro) =>
        new()
        {
            Id = row.Id,
            EscolaId = escolaId,
            ProfessorId = row.ProfessorId,
            ProfessorNome = row.ProfessorNome,
            LivroId = row.LivroId,
            LivroNome = row.LivroNome,
            Nome = row.Nome,
            Sala = row.Sala,
            HorarioInicio = row.Horario?.ToString("HH:mm"),
            HorarioFim = row.HorarioFim?.ToString("HH:mm"),
            DataInicio = row.DataInicio?.ToString("yyyy-MM-dd"),
            DataTerminoPrevista = row.DataTerminoPrevista?.ToString("yyyy-MM-dd"),
            Status = row.Status,
            Observacoes = row.Observacoes,
            DiasSemana = row.DiasSemana,
            TotalAlunosAtivos = row.TotalAlunosAtivos,
            TotalAulasPrevistasLivro = totalAulasLivro
        };

    private static IReadOnlyList<int> NormalizarDiasSemana(IEnumerable<int> dias) =>
        dias.Where(d => d is >= 0 and <= 6).Distinct().OrderBy(d => d).ToList();

    private async Task<string?> ValidarHorarioDentroFuncionamentoAsync(
        int escolaId,
        IReadOnlyList<int> diasSemana,
        TimeOnly horarioInicio,
        TimeOnly horarioFim,
        CancellationToken cancellationToken)
    {
        if (!await _horariosFuncionamento.PossuiConfiguracaoAsync(escolaId, cancellationToken))
        {
            return "Horario de funcionamento da escola nao configurado. Cadastre os horarios da escola.";
        }

        foreach (var dia in diasSemana)
        {
            var horarioDia = await _horariosFuncionamento.ObterPorDiaSemanaAsync(escolaId, dia, cancellationToken);
            if (horarioDia is null || !horarioDia.Aberto)
            {
                return $"A escola nao funciona em {NomeDiaSemana(dia)}. Escolha outro dia da semana.";
            }

            if (!horarioDia.PermiteIntervalo(horarioInicio, horarioFim))
            {
                var abertura = horarioDia.HorarioAbertura!.Value.ToString("HH:mm");
                var fechamento = horarioDia.HorarioFechamento!.Value.ToString("HH:mm");
                return
                    $"Horario da turma fora do funcionamento da escola em {NomeDiaSemana(dia)} (permitido: {abertura} - {fechamento}).";
            }
        }

        return null;
    }

    private static string NomeDiaSemana(int dia) =>
        dia is >= 0 and <= 6 ? NomesDiaSemana[dia] : $"dia {dia}";

    private static TurmaOperacaoResultado Falha(string msg, TurmasFalha falha, int statusCode = 400) =>
        new(false, null, msg, falha, statusCode);

    private Task<int?> ObterEscolaIdAsync(AppUserContext uc, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(uc.CodigoEscola))
        {
            return Task.FromResult<int?>(null);
        }

        return _escolas.ObterIdAtivaPorCodigoEscolaAsync(uc.CodigoEscola, cancellationToken);
    }

    private static int? FiltroProfessorId(AppUserContext uc) =>
        EhProfessor(uc) ? uc.UserId : null;

    private static bool EhProfessor(AppUserContext uc) =>
        string.Equals(uc.Perfil, "Professor", StringComparison.OrdinalIgnoreCase);
}
