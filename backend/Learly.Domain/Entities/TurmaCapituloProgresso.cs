namespace Learly.Domain.Entities;

public sealed class TurmaCapituloProgresso
{
    public int Id { get; internal set; }
    public int EscolaId { get; internal set; }
    public int TurmaId { get; internal set; }
    public int CapituloId { get; internal set; }
    public bool Concluido { get; internal set; }
    public DateOnly? DataConclusao { get; internal set; }
}
