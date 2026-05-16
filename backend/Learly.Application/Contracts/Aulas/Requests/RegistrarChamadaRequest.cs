namespace Learly.Application.Contracts.Aulas.Requests;

public sealed class RegistrarChamadaRequest
{
    public List<ChamadaPresencaItemRequest> Presencas { get; init; } = [];
}

public sealed class ChamadaPresencaItemRequest
{
    public int AlunoId { get; init; }
    public string Status { get; init; } = string.Empty;
}
