using Learly.Domain.Exceptions;

namespace Learly.Domain.Entities;

public sealed class Presenca
{
    public static class Statuses
    {
        public const string Presente = "P";
        public const string Falta = "F";
        public const string FaltaJustificada = "FJ";
        public const string Reposicao = "R";
    }

    public int Id { get; internal set; }
    public int EscolaId { get; internal set; }
    public int AulaId { get; internal set; }
    public int AlunoId { get; internal set; }
    public string StatusPresenca { get; internal set; } = Statuses.Presente;
    public int? ReposicaoDePresencaId { get; internal set; }

    public static Presenca Criar(int escolaId, int aulaId, int alunoId, string status)
    {
        return new Presenca
        {
            EscolaId = escolaId,
            AulaId = aulaId,
            AlunoId = alunoId,
            StatusPresenca = NormalizarStatusProfessor(status),
        };
    }

    public bool PodeAlterarPeloProfessor() =>
        !string.Equals(StatusPresenca, Statuses.Reposicao, StringComparison.Ordinal)
        && !string.Equals(StatusPresenca, Statuses.FaltaJustificada, StringComparison.Ordinal);

    public void AtualizarStatusProfessor(string status)
    {
        if (!PodeAlterarPeloProfessor())
        {
            throw new DomainException("Presenca bloqueada para edicao pelo professor.");
        }

        StatusPresenca = NormalizarStatusProfessor(status);
    }

    private static string NormalizarStatusProfessor(string status)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            throw new DomainException("Status de presenca invalido.");
        }

        var s = status.Trim().ToUpperInvariant();
        if (s is not (Statuses.Presente or Statuses.Falta))
        {
            throw new DomainException("Status de presenca invalido para lancamento.");
        }

        return s;
    }
}
