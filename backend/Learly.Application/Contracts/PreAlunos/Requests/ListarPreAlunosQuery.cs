namespace Learly.Application.Contracts.PreAlunos.Requests;

public sealed class ListarPreAlunosQuery
{
    /// <summary>Filtro opcional: status exato como no banco (ex.: Em negociacao, Aguardando aprovacao).</summary>
    public string? Status { get; init; }
}
