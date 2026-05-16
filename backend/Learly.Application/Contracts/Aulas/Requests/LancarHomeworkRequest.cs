namespace Learly.Application.Contracts.Aulas.Requests;

public sealed class LancarHomeworkRequest
{
    public List<HomeworkNotaItemRequest> Notas { get; init; } = [];
}

public sealed class HomeworkNotaItemRequest
{
    public int AlunoId { get; init; }
    public decimal? Nota { get; init; }
}
