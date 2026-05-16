namespace Learly.Application.Contracts.Turmas.Requests;

public sealed class SalvarAvaliacoesTurmaRequest
{
    public List<AvaliacaoItemRequest> Avaliacoes { get; init; } = [];
}

public sealed class AvaliacaoItemRequest
{
    public int AlunoId { get; init; }
    public string Tipo { get; init; } = string.Empty;
    public decimal Nota { get; init; }
}
