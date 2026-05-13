namespace Learly.Application.Contracts.HorariosFuncionamento;

/// <param name="DiaSemana">0=Domingo … 6=Sábado (padrão .NET DayOfWeek).</param>
/// <param name="HorarioAbertura">Formato "HH:mm". Nulo quando fechado.</param>
/// <param name="HorarioFechamento">Formato "HH:mm". Nulo quando fechado.</param>
public sealed record HorarioFuncionamentoResponse(
    int DiaSemana,
    bool Aberto,
    string? HorarioAbertura,
    string? HorarioFechamento);
