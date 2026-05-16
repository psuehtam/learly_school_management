namespace Learly.Application.Contracts.Turmas.Requests;

public sealed class AtualizarTurmaRequest
{
    public int? ProfessorId { get; set; }
    public int? LivroId { get; set; }
    public string? Sala { get; set; }
    public string? Observacoes { get; set; }
    /// <summary>0=Dom … 6=Sáb. Apenas turmas em espera.</summary>
    public List<int>? DiasSemana { get; set; }
    public string? HorarioInicio { get; set; }
    public string? HorarioFim { get; set; }
}
