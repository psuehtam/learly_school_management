namespace Learly.Application.Contracts.Matriculas.Requests;

public sealed class ListarMatriculasQuery
{
    public string? Status { get; set; }
    public int? AlunoId { get; set; }
    public int? TurmaId { get; set; }
}
