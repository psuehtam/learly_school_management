namespace Learly.Application.Contracts.Alunos.Requests;

public sealed class CriarAlunoRequest
{
    public bool EProprioResponsavel { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Sobrenome { get; set; } = string.Empty;
    public string Sexo { get; set; } = string.Empty;
    public DateOnly DataNascimento { get; set; }
    public DateOnly DataIngresso { get; set; }
    public string? Cpf { get; set; }
    public string Cep { get; set; } = string.Empty;
    public string TipoLogradouro { get; set; } = string.Empty;
    public string Logradouro { get; set; } = string.Empty;
    public string Numero { get; set; } = string.Empty;
    public string? Complemento { get; set; }
    public string Bairro { get; set; } = string.Empty;
    public string Municipio { get; set; } = string.Empty;
    public string? ResponsavelNome { get; set; }
    public string? ResponsavelSobrenome { get; set; }
    public string? ResponsavelCpf { get; set; }
    public string? ResponsavelSexo { get; set; }
    public string? ResponsavelCep { get; set; }
    public string? ResponsavelTipoLogradouro { get; set; }
    public string? ResponsavelLogradouro { get; set; }
    public string? ResponsavelNumero { get; set; }
    public string? ResponsavelComplemento { get; set; }
    public string? ResponsavelBairro { get; set; }
    public string? ResponsavelMunicipio { get; set; }
}
