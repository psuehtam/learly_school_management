namespace Learly.Domain.Entities;

public sealed class CompromissoParticipante
{
    public int Id { get; internal set; }
    public int CompromissoId { get; internal set; }
    public int UsuarioId { get; internal set; }
    public string Confirmacao { get; internal set; } = "Pendente";
}
