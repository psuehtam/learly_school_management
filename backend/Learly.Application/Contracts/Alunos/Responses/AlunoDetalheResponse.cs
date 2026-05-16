namespace Learly.Application.Contracts.Alunos.Responses;

public sealed class AlunoDetalheResponse
{
    public int Id { get; init; }
    public int EscolaId { get; init; }
    public string EscolaNome { get; init; } = "";
    public string Nome { get; init; } = "";
    public string Sobrenome { get; init; } = "";
    public string Sexo { get; init; } = "";
    public string DataNascimento { get; init; } = "";
    public string DataIngresso { get; init; } = "";
    public string? Cpf { get; init; }
    public string Status { get; init; } = "";
    public string Cep { get; init; } = "";
    public string TipoLogradouro { get; init; } = "";
    public string Logradouro { get; init; } = "";
    public string Numero { get; init; } = "";
    public string? Complemento { get; init; }
    public string Bairro { get; init; } = "";
    public string Municipio { get; init; } = "";
    public string? NaturalidadeCidade { get; init; }
    public string? NaturalidadeEstado { get; init; }
    public string? RgNumero { get; init; }
    public string? RgExpedicao { get; init; }
    public string? RgOrgao { get; init; }
    public string? TelefoneAluno { get; init; }
    public bool EProprioResponsavel { get; init; }
    public string? ResponsavelNome { get; init; }
    public string? ResponsavelSobrenome { get; init; }
    public string? ResponsavelCpf { get; init; }
    public string? TelefoneResponsavel { get; init; }
}
