using Learly.Domain.Entities;

namespace Learly.Domain.Interfaces.Repositories;

public interface IAlunoRepository : IRepository<Aluno, int>
{
    Task<Aluno?> ObterPorIdEEscolaAsync(int alunoId, int escolaId, CancellationToken cancellationToken = default);
    Task<bool> ExisteCpfNaEscolaAsync(int escolaId, string cpf, CancellationToken cancellationToken = default);
    Task<bool> ExisteResponsavelNaEscolaAsync(int escolaId, int responsavelId, CancellationToken cancellationToken = default);
    Task<int?> ObterResponsavelIdPorCpfAsync(int escolaId, string cpfCnpj, CancellationToken cancellationToken = default);
    Task<int> CriarResponsavelFisicoAsync(
        int escolaId,
        string cpf,
        string nome,
        string sobrenome,
        string sexo,
        string cep,
        string tipoLogradouro,
        string logradouro,
        string numero,
        string? complemento,
        string bairro,
        string municipio,
        CancellationToken cancellationToken = default);
}
