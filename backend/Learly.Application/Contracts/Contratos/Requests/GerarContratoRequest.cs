namespace Learly.Application.Contracts.Contratos.Requests;

public sealed class GerarContratoRequest
{
    /// <summary>ID do pré-aluno para o qual o contrato será gerado.</summary>
    public int PreAlunoId { get; init; }

    /// <summary>
    /// ID do template a ser usado. Se null, usa o template ativo da escola.
    /// </summary>
    public int? TemplateId { get; init; }
}
