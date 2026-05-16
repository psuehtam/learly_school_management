namespace Learly.Application.Contracts.Turmas.Requests;

public sealed class AtivarTurmaRequest
{
    public DateOnly DataInicio { get; set; }
    /// <summary>Se informado na ativação, substitui dias já cadastrados.</summary>
    public List<int>? DiasSemana { get; set; }
    public string? HorarioInicio { get; set; }
    public string? HorarioFim { get; set; }
    public string? Sala { get; set; }
}
