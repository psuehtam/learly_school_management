namespace Learly.Domain.Entities;

public sealed class Homework
{
    public int Id { get; internal set; }
    public int EscolaId { get; internal set; }
    public int AulaId { get; internal set; }
    public int AlunoId { get; internal set; }
    public decimal? Nota { get; internal set; }

    public static Homework Criar(int escolaId, int aulaId, int alunoId, decimal? nota) =>
        new()
        {
            EscolaId = escolaId,
            AulaId = aulaId,
            AlunoId = alunoId,
            Nota = nota,
        };

    public void AtualizarNota(decimal? nota) => Nota = nota;
}
