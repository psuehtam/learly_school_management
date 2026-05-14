using Learly.Application.Contracts.Livros;
using Learly.Application.Contracts.Livros.Requests;
using Learly.Application.Services.Common;

namespace Learly.Application.Services.Livros;

public interface ILivrosEscolaService
{
    Task<LivrosEscolaListagemResultado> ListarAsync(AppUserContext uc, CancellationToken cancellationToken = default);

    Task<LivrosEscolaDetalheResultado> ObterPorIdAsync(int livroId, AppUserContext uc, CancellationToken cancellationToken = default);

    Task<LivrosEscolaCriacaoResultado> CriarAsync(CriarLivroEscolaRequest body, AppUserContext uc, CancellationToken cancellationToken = default);

    Task<LivrosEscolaAtualizacaoResultado> AtualizarAsync(
        int livroId,
        AtualizarLivroEscolaRequest body,
        AppUserContext uc,
        CancellationToken cancellationToken = default);
}
