using Learly.Domain.Exceptions;

namespace Learly.Domain.Entities;

public sealed class Avaliacao
{
    public static readonly string[] TiposValidos =
    [
        "Speaking",
        "Listening",
        "Writing",
        "Class Participation",
        "Avaliacao Final",
    ];

    public int Id { get; internal set; }
    public int EscolaId { get; internal set; }
    public int TurmaId { get; internal set; }
    public int AlunoId { get; internal set; }
    public string TipoAvaliacao { get; internal set; } = TiposValidos[0];
    public decimal Nota { get; internal set; }

    public static Avaliacao Criar(int escolaId, int turmaId, int alunoId, string tipo, decimal nota) =>
        new()
        {
            EscolaId = escolaId,
            TurmaId = turmaId,
            AlunoId = alunoId,
            TipoAvaliacao = NormalizarTipo(tipo),
            Nota = ValidarNota(nota),
        };

    public void AtualizarNota(decimal nota) => Nota = ValidarNota(nota);

    public static string NormalizarTipo(string tipo)
    {
        if (string.IsNullOrWhiteSpace(tipo))
        {
            throw new DomainException("Tipo de avaliacao invalido.");
        }

        var t = tipo.Trim();
        if (t.Equals("Avaliação Final", StringComparison.OrdinalIgnoreCase)
            || t.Equals("Avaliacao Final", StringComparison.OrdinalIgnoreCase))
        {
            return "Avaliacao Final";
        }

        var match = TiposValidos.FirstOrDefault(
            x => string.Equals(x, t, StringComparison.OrdinalIgnoreCase));
        if (match is null)
        {
            throw new DomainException("Tipo de avaliacao invalido.");
        }

        return match;
    }

    private static decimal ValidarNota(decimal nota)
    {
        if (nota < 0 || nota > 100)
        {
            throw new DomainException("Nota deve estar entre 0 e 100.");
        }

        return nota;
    }
}
