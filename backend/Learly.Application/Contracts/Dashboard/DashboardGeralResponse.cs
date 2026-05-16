namespace Learly.Application.Contracts.Dashboard;

public sealed class DashboardGeralResponse
{
    public DashboardResumoResponse Resumo { get; init; } = new();
    public IReadOnlyList<DashboardAulaHojeResponse> AulasHoje { get; init; } = [];
    public IReadOnlyList<DashboardParcelaVencidaResponse> ParcelasVencidas { get; init; } = [];
    public IReadOnlyList<DashboardAtividadeResponse> AtividadeRecente { get; init; } = [];
}

public sealed class DashboardResumoResponse
{
    public int AlunosAtivos { get; init; }
    public int TurmasAtivas { get; init; }
    public int Professores { get; init; }
    public int ParcelasEmAberto { get; init; }
}

public sealed class DashboardAulaHojeResponse
{
    public int AulaId { get; init; }
    public string TurmaNome { get; init; } = string.Empty;
    public string ProfessorNome { get; init; } = string.Empty;
    public string Horario { get; init; } = string.Empty;
    public string Turno { get; init; } = string.Empty;
    public int TotalAlunos { get; init; }
}

public sealed class DashboardParcelaVencidaResponse
{
    public int ParcelaId { get; init; }
    public string AlunoNome { get; init; } = string.Empty;
    public string TurmaNome { get; init; } = string.Empty;
    public string DataVencimento { get; init; } = string.Empty;
    public decimal Valor { get; init; }
}

public sealed class DashboardAtividadeResponse
{
    public string Acao { get; init; } = string.Empty;
    public string Detalhe { get; init; } = string.Empty;
    public DateTime OcorridoEm { get; init; }
    public string Tipo { get; init; } = string.Empty;
}
