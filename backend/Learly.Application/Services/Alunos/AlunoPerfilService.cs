using Learly.Application.Contracts.Alunos.Requests;
using Learly.Application.Contracts.Alunos.Responses;
using Learly.Application.Services.Common;
using Learly.Domain.Interfaces.Repositories;

namespace Learly.Application.Services.Alunos;

public sealed class AlunoPerfilService : IAlunoPerfilService
{
    private readonly IAlunoRepository _alunos;
    private readonly IAlunoPerfilRepository _perfil;
    private readonly IEscolaRepository _escolas;

    public AlunoPerfilService(
        IAlunoRepository alunos,
        IAlunoPerfilRepository perfil,
        IEscolaRepository escolas)
    {
        _alunos = alunos;
        _perfil = perfil;
        _escolas = escolas;
    }

    public async Task<IReadOnlyList<AlunoOcorrenciaResponse>> ListarOcorrenciasAsync(
        int alunoId,
        AppUserContext uc,
        CancellationToken cancellationToken = default)
    {
        var escolaId = await ObterEscolaIdAsync(uc, cancellationToken);
        if (!escolaId.HasValue || !await AlunoExisteAsync(alunoId, escolaId.Value, cancellationToken))
            return [];

        var rows = await _perfil.ListarOcorrenciasAsync(escolaId.Value, alunoId, cancellationToken);
        return rows.Select(MapOcorrencia).ToList();
    }

    public async Task<(bool Ok, AlunoOcorrenciaResponse? Item, string? Erro, int StatusCode)> SalvarOcorrenciaAsync(
        int alunoId,
        int? ocorrenciaId,
        SalvarAlunoOcorrenciaRequest request,
        AppUserContext uc,
        CancellationToken cancellationToken = default)
    {
        var escolaId = await ObterEscolaIdAsync(uc, cancellationToken);
        if (!escolaId.HasValue)
            return (false, null, "Acesso negado.", 403);

        if (!await AlunoExisteAsync(alunoId, escolaId.Value, cancellationToken))
            return (false, null, "Aluno nao encontrado.", 404);

        if (!TryParseOcorrencia(request, out var tipoDb, out var data, out var hora, out var erro))
            return (false, null, erro, 400);

        if (ocorrenciaId is null or <= 0)
        {
            var novoId = await _perfil.CriarOcorrenciaAsync(
                escolaId.Value,
                alunoId,
                uc.UserId,
                tipoDb,
                data,
                hora,
                request.Descricao.Trim(),
                string.IsNullOrWhiteSpace(request.Resolucao) ? null : request.Resolucao.Trim(),
                aulaId: null,
                cancellationToken);

            var criada = (await _perfil.ListarOcorrenciasAsync(escolaId.Value, alunoId, cancellationToken))
                .FirstOrDefault(o => o.Id == novoId);

            return criada is null
                ? (true, null, null, 201)
                : (true, MapOcorrencia(criada), null, 201);
        }

        var atualizado = await _perfil.AtualizarOcorrenciaAsync(
            escolaId.Value,
            alunoId,
            ocorrenciaId.Value,
            tipoDb,
            data,
            hora,
            request.Descricao.Trim(),
            string.IsNullOrWhiteSpace(request.Resolucao) ? null : request.Resolucao.Trim(),
            cancellationToken);

        if (!atualizado)
            return (false, null, "Ocorrencia nao encontrada.", 404);

        var item = (await _perfil.ListarOcorrenciasAsync(escolaId.Value, alunoId, cancellationToken))
            .FirstOrDefault(o => o.Id == ocorrenciaId.Value);

        return item is null
            ? (true, null, null, 200)
            : (true, MapOcorrencia(item), null, 200);
    }

    public Task<IReadOnlyList<AlunoDocumentoResponse>> ListarDocumentosAsync(
        int alunoId,
        AppUserContext uc,
        CancellationToken cancellationToken = default)
    {
        // Tabela de documentos do aluno ainda nao modelada no banco; endpoint pronto para integracao futura.
        return Task.FromResult<IReadOnlyList<AlunoDocumentoResponse>>([]);
    }

    public async Task<IReadOnlyList<AlunoFaltaResponse>> ListarFaltasAsync(
        int alunoId,
        AppUserContext uc,
        CancellationToken cancellationToken = default)
    {
        var escolaId = await ObterEscolaIdAsync(uc, cancellationToken);
        if (!escolaId.HasValue || !await AlunoExisteAsync(alunoId, escolaId.Value, cancellationToken))
            return [];

        var rows = await _perfil.ListarFaltasAsync(escolaId.Value, alunoId, cancellationToken);
        return rows.Select(MapFalta).ToList();
    }

    public async Task<(bool Ok, string? Erro, int StatusCode)> JustificarFaltaAsync(
        int alunoId,
        int presencaId,
        JustificarAlunoFaltaRequest request,
        AppUserContext uc,
        CancellationToken cancellationToken = default)
    {
        var escolaId = await ObterEscolaIdAsync(uc, cancellationToken);
        if (!escolaId.HasValue)
            return (false, "Acesso negado.", 403);

        if (!await AlunoExisteAsync(alunoId, escolaId.Value, cancellationToken))
            return (false, "Aluno nao encontrado.", 404);

        if (string.IsNullOrWhiteSpace(request.Motivo))
            return (false, "Motivo da justificativa e obrigatorio.", 400);

        var ok = await _perfil.JustificarFaltaAsync(escolaId.Value, alunoId, presencaId, cancellationToken);
        if (!ok)
            return (false, "Falta nao encontrada ou ja justificada.", 404);

        return (true, null, 204);
    }

    private async Task<bool> AlunoExisteAsync(int alunoId, int escolaId, CancellationToken cancellationToken)
    {
        var aluno = await _alunos.ObterPorIdEEscolaAsync(alunoId, escolaId, cancellationToken);
        return aluno is not null;
    }

    private Task<int?> ObterEscolaIdAsync(AppUserContext uc, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(uc.CodigoEscola))
            return Task.FromResult<int?>(null);

        return _escolas.ObterIdAtivaPorCodigoEscolaAsync(uc.CodigoEscola, cancellationToken);
    }

    private static bool TryParseOcorrencia(
        SalvarAlunoOcorrenciaRequest request,
        out string tipoDb,
        out DateOnly data,
        out TimeOnly hora,
        out string erro)
    {
        tipoDb = "";
        data = default;
        hora = default;
        erro = "";

        if (string.IsNullOrWhiteSpace(request.Descricao))
        {
            erro = "Descricao da ocorrencia e obrigatoria.";
            return false;
        }

        if (!DateOnly.TryParse(request.Data, out data))
        {
            erro = "Data da ocorrencia invalida.";
            return false;
        }

        if (!TimeOnly.TryParse(request.Hora, out hora))
        {
            erro = "Hora da ocorrencia invalida.";
            return false;
        }

        var tipo = request.Tipo.Trim();
        if (tipo.Equals("Acadêmica", StringComparison.OrdinalIgnoreCase)
            || tipo.Equals("Academica", StringComparison.OrdinalIgnoreCase))
        {
            tipoDb = "Academica";
            return true;
        }

        if (tipo.Equals("Administrativa", StringComparison.OrdinalIgnoreCase))
        {
            tipoDb = "Administrativa";
            return true;
        }

        erro = "Tipo de ocorrencia invalido.";
        return false;
    }

    private static AlunoOcorrenciaResponse MapOcorrencia(Domain.ReadModels.AlunoOcorrenciaItem item) =>
        new()
        {
            Id = item.Id,
            Data = item.DataOcorrencia.ToString("yyyy-MM-dd"),
            Hora = item.HoraOcorrencia.ToString("HH:mm"),
            Tipo = item.Tipo.Equals("Academica", StringComparison.OrdinalIgnoreCase) ? "Acadêmica" : "Administrativa",
            Descricao = item.Descricao,
            Resolucao = item.Resolucao,
            AulaVinculada = string.IsNullOrWhiteSpace(item.AulaVinculada) ? "" : item.AulaVinculada.Trim(),
            Autor = item.AutorNome.Trim(),
        };

    private static AlunoFaltaResponse MapFalta(Domain.ReadModels.AlunoFaltaItem item) =>
        new()
        {
            Id = item.PresencaId,
            Data = item.DataAula.ToString("yyyy-MM-dd"),
            Book = item.TurmaNome.Trim(),
            Aula = item.AulaTitulo.Trim(),
            Justificada = item.StatusPresenca.Equals("FJ", StringComparison.OrdinalIgnoreCase),
        };
}
