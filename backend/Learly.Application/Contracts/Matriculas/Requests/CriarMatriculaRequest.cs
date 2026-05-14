namespace Learly.Application.Contracts.Matriculas.Requests;

public sealed class CriarMatriculaRequest
{
    public int AlunoId { get; set; }
    public int? TurmaId { get; set; }
    public DateOnly DataMatricula { get; set; }
}
