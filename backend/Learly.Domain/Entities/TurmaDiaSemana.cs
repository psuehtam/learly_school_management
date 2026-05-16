namespace Learly.Domain.Entities;

/// <summary>Dia da semana em que a turma tem aula (0=Domingo … 6=Sábado).</summary>
public sealed class TurmaDiaSemana
{
    public int Id { get; internal set; }
    public int EscolaId { get; internal set; }
    public int TurmaId { get; internal set; }
    public int DiaSemana { get; internal set; }
}
