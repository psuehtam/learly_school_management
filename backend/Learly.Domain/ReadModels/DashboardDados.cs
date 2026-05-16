namespace Learly.Domain.ReadModels;

public sealed record DashboardResumoDados(
    int AlunosAtivos,
    int TurmasAtivas,
    int Professores,
    int ParcelasEmAberto);

public sealed record DashboardAulaHojeDados(
    int AulaId,
    string TurmaNome,
    string ProfessorNome,
    TimeOnly HorarioInicio,
    int TotalAlunos);

public sealed record DashboardParcelaVencidaDados(
    int ParcelaId,
    string AlunoNome,
    string TurmaNome,
    DateOnly DataVencimento,
    decimal Valor);

public sealed record DashboardAtividadeDados(
    string Acao,
    string Detalhe,
    DateTime OcorridoEm,
    string Tipo);

public sealed record DashboardDadosEscola(
    DashboardResumoDados Resumo,
    IReadOnlyList<DashboardAulaHojeDados> AulasHoje,
    IReadOnlyList<DashboardParcelaVencidaDados> ParcelasVencidas,
    IReadOnlyList<DashboardAtividadeDados> AtividadeRecente);
