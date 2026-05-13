using Learly.Domain.Entities;
using Learly.Domain.Interfaces.Repositories;
using Learly.Domain.ReadModels;
using Learly.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Learly.Infrastructure.Repositories;

internal sealed class PreAlunoRepository(LearlyDbContext db) : RepositoryBase<PreAluno, int>(db), IPreAlunoRepository
{
    public Task<PreAluno?> ObterPorIdEEscolaRastreadoAsync(int id, int escolaId, CancellationToken cancellationToken = default)
    {
        return Set.FirstOrDefaultAsync(p => p.Id == id && p.EscolaId == escolaId, cancellationToken);
    }

    public async Task<IReadOnlyList<PreAlunoListagemItem>> ListarPorEscolaAsync(
        int escolaId,
        string? filtroStatus,
        CancellationToken cancellationToken = default)
    {
        var query = Db.PreAlunos.AsNoTracking().Where(p => p.EscolaId == escolaId);
        if (!string.IsNullOrWhiteSpace(filtroStatus))
        {
            var st = filtroStatus.Trim();
            query = query.Where(p => p.Status == st);
        }

        var lista = await query
            .OrderByDescending(p => p.DataCriacao)
            .ToListAsync(cancellationToken);

        if (lista.Count == 0)
            return Array.Empty<PreAlunoListagemItem>();

        var respIds = lista.Select(p => p.ResponsavelId).Distinct().ToList();
        var livIds = lista.Select(p => p.LivroInteresseId).Distinct().ToList();

        var respostasResp = await Db.Responsaveis.AsNoTracking()
            .Where(r => r.EscolaId == escolaId && respIds.Contains(r.Id))
            .ToDictionaryAsync(r => r.Id, cancellationToken);

        var respostasLivros = await Db.Livros.AsNoTracking()
            .Where(l => l.EscolaId == escolaId && livIds.Contains(l.Id))
            .ToDictionaryAsync(l => l.Id, cancellationToken);

        var itens = new List<PreAlunoListagemItem>(lista.Count);
        foreach (var p in lista)
        {
            respostasResp.TryGetValue(p.ResponsavelId, out var resp);
            respostasLivros.TryGetValue(p.LivroInteresseId, out var liv);

            var nomeResp = resp is null
                ? $"Responsavel #{p.ResponsavelId}"
                : $"{resp.Nome.Trim()} {resp.Sobrenome.Trim()}".Trim();

            itens.Add(new PreAlunoListagemItem(
                p.Id,
                $"{p.Nome.Trim()} {p.Sobrenome.Trim()}".Trim(),
                nomeResp,
                p.DataCriacao,
                p.TipoContrato,
                p.Status,
                liv?.Nome?.Trim() ?? $"Livro #{p.LivroInteresseId}",
                p.Telefone,
                p.ValorMensalidade,
                p.FormaPagamento,
                p.OrigemCaptacao,
                p.ValorMaterial,
                p.ValorMatricula));
        }

        return itens;
    }

    public async Task<PreAlunoDetalheItem?> ObterDetalheAsync(int id, int escolaId, CancellationToken cancellationToken = default)
    {
        var p = await Set.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id && x.EscolaId == escolaId, cancellationToken);

        if (p is null)
            return null;

        var resp = await Db.Responsaveis.AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == p.ResponsavelId && r.EscolaId == escolaId, cancellationToken);

        var liv = await Db.Livros.AsNoTracking()
            .FirstOrDefaultAsync(l => l.Id == p.LivroInteresseId && l.EscolaId == escolaId, cancellationToken);

        if (resp is null || liv is null)
            return null;

        return new PreAlunoDetalheItem(
            p.Id,
            p.EscolaId,
            p.ResponsavelId,
            resp.TipoPessoa,
            resp.CpfCnpj,
            $"{resp.Nome.Trim()} {resp.Sobrenome.Trim()}".Trim(),
            p.Nome.Trim(),
            p.Sobrenome.Trim(),
            p.DataNascimento,
            p.Telefone,
            p.LivroInteresseId,
            liv.Nome.Trim(),
            p.TipoContrato,
            p.ValorMensalidade,
            p.FormaPagamento,
            p.ValorMatricula,
            p.FormaPagamentoMatricula,
            p.ValorMaterial,
            p.OrigemCaptacao,
            p.UsaTransporteVan,
            p.TransporteCep,
            p.TransporteLogradouro,
            p.TransporteNumero,
            p.TransporteComplemento,
            p.TransporteBairro,
            p.TransporteCidade,
            p.TransporteUf,
            p.ObservacoesComerciais,
            p.Status,
            p.AlunoId,
            p.CriadoPorUsuarioId,
            p.DataCriacao,
            p.DataAtualizacao);
    }

    public Task<bool> ExisteLivroAtivoNaEscolaAsync(int escolaId, int livroId, CancellationToken cancellationToken = default)
    {
        return Db.Livros.AsNoTracking()
            .AnyAsync(l => l.EscolaId == escolaId && l.Id == livroId && l.Status == "Ativo", cancellationToken);
    }
}
