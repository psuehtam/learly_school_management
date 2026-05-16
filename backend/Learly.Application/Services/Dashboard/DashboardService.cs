using Learly.Application.Contracts.Dashboard;
using Learly.Application.Services.Common;
using Learly.Domain.Interfaces.Repositories;
using Learly.Domain.ReadModels;

namespace Learly.Application.Services.Dashboard;

public sealed class DashboardService : IDashboardService
{
    private readonly IDashboardRepository _dashboard;
    private readonly IEscolaRepository _escolas;

    public DashboardService(IDashboardRepository dashboard, IEscolaRepository escolas)
    {
        _dashboard = dashboard;
        _escolas = escolas;
    }

    public async Task<DashboardGeralResponse?> ObterGeralAsync(
        AppUserContext uc,
        DateOnly? dataReferencia = null,
        CancellationToken cancellationToken = default)
    {
        var escolaId = await ObterEscolaIdAsync(uc, cancellationToken);
        if (!escolaId.HasValue)
        {
            return null;
        }

        var data = dataReferencia ?? DateOnly.FromDateTime(DateTime.Now);
        var dados = await _dashboard.ObterDadosEscolaAsync(escolaId.Value, data, cancellationToken);
        return Map(dados);
    }

    private static DashboardGeralResponse Map(DashboardDadosEscola dados) =>
        new()
        {
            Resumo = new DashboardResumoResponse
            {
                AlunosAtivos = dados.Resumo.AlunosAtivos,
                TurmasAtivas = dados.Resumo.TurmasAtivas,
                Professores = dados.Resumo.Professores,
                ParcelasEmAberto = dados.Resumo.ParcelasEmAberto,
            },
            AulasHoje = dados.AulasHoje.Select(a => new DashboardAulaHojeResponse
            {
                AulaId = a.AulaId,
                TurmaNome = a.TurmaNome,
                ProfessorNome = a.ProfessorNome,
                Horario = a.HorarioInicio.ToString("HH:mm"),
                Turno = ClassificarTurno(a.HorarioInicio),
                TotalAlunos = a.TotalAlunos,
            }).ToList(),
            ParcelasVencidas = dados.ParcelasVencidas.Select(p => new DashboardParcelaVencidaResponse
            {
                ParcelaId = p.ParcelaId,
                AlunoNome = p.AlunoNome,
                TurmaNome = p.TurmaNome,
                DataVencimento = p.DataVencimento.ToString("dd/MM/yyyy"),
                Valor = p.Valor,
            }).ToList(),
            AtividadeRecente = dados.AtividadeRecente.Select(a => new DashboardAtividadeResponse
            {
                Acao = a.Acao,
                Detalhe = a.Detalhe,
                OcorridoEm = a.OcorridoEm,
                Tipo = a.Tipo,
            }).ToList(),
        };

    private static string ClassificarTurno(TimeOnly horario)
    {
        if (horario.Hour < 12)
        {
            return "morning";
        }

        if (horario.Hour < 18)
        {
            return "afternoon";
        }

        return "evening";
    }

    private Task<int?> ObterEscolaIdAsync(AppUserContext uc, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(uc.CodigoEscola))
        {
            return Task.FromResult<int?>(null);
        }

        return _escolas.ObterIdAtivaPorCodigoEscolaAsync(uc.CodigoEscola, cancellationToken);
    }
}
