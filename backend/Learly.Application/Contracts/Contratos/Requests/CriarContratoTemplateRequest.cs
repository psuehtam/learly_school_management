namespace Learly.Application.Contracts.Contratos.Requests;

public sealed class CriarContratoTemplateRequest
{
    /// <summary>Nome identificador do template (ex.: "Contrato Padrão 2026").</summary>
    public string Nome { get; init; } = string.Empty;

    /// <summary>Conteúdo HTML completo do template com variáveis no formato {{Variavel_Campo}}.</summary>
    public string ConteudoHtml { get; init; } = string.Empty;

    /// <summary>Se true, desativa todos os outros templates ativos desta escola e ativa este.</summary>
    public bool AtivarImediatamente { get; init; } = false;
}
