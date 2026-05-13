"use client";

import { useCallback, useEffect, useMemo, useState } from "react";
import { getApiErrorMessage } from "@/lib/api/client";
import {
  getPerfis,
  getPermissoesAgrupadas,
  getPermissoesDoPerfil,
  salvarPermissoes,
  type PermissaoCatalogoItem,
  type PerfilTemplate,
  type PermissaoModuloGrupo,
} from "@/lib/api/templates";

function getNomePermissaoPai(nomePermissao: string, permissaoIdByNome: Map<string, number>): string | null {
  if (nomePermissao.startsWith("VISUALIZAR_")) {
    return null;
  }

  const [, ...sufixo] = nomePermissao.split("_");
  if (sufixo.length === 0) return null;

  const possivelPai = `VISUALIZAR_${sufixo.join("_")}`;
  return permissaoIdByNome.has(possivelPai) ? possivelPai : null;
}

function ordenarPermissoes(a: PermissaoCatalogoItem, b: PermissaoCatalogoItem) {
  const aEhVisualizacao = a.nome.startsWith("VISUALIZAR_");
  const bEhVisualizacao = b.nome.startsWith("VISUALIZAR_");

  if (aEhVisualizacao && !bEhVisualizacao) return -1;
  if (!aEhVisualizacao && bEhVisualizacao) return 1;
  return a.nome.localeCompare(b.nome);
}

const CATEGORIAS_ACAO = [
  { id: "visualizacao", rotulo: "Visualizacao", prefixos: ["VISUALIZAR_"] },
  {
    id: "criacao",
    rotulo: "Criacao e cadastro",
    prefixos: ["CRIAR_", "GERAR_", "UPLOAD_", "ANEXAR_", "VINCULAR_", "ADICIONAR_", "IMPORTAR_"],
  },
  {
    id: "edicao",
    rotulo: "Operacao e edicao",
    prefixos: [
      "EDITAR_",
      "MARCAR_",
      "REALIZAR_",
      "APROVAR_",
      "REPROVAR_",
      "CONFIRMAR_",
      "RECUSAR_",
      "BAIXA_",
      "ESTORNAR_",
      "FINALIZAR_",
      "CONCLUIR_",
      "TRANCAR_",
      "JUSTIFICAR_",
      "DESVINCULAR_",
      "REMANEJAR_",
      "REGISTRAR_",
      "LANCAR_",
      "DEVOLVER_",
    ],
  },
  {
    id: "gestao",
    rotulo: "Gestao e controle",
    prefixos: ["GERENCIAR_", "CANCELAR_", "INATIVAR_", "EXCLUIR_", "EXPORTAR_", "REMOVER_"],
  },
  { id: "outras", rotulo: "Outras permissoes", prefixos: [] },
] as const;

function getCategoriaPermissao(nomePermissao: string) {
  for (const categoria of CATEGORIAS_ACAO) {
    if (categoria.id === "outras") continue;
    if (categoria.prefixos.some((prefixo) => nomePermissao.startsWith(prefixo))) {
      return categoria.id;
    }
  }
  return "outras";
}

function agruparPermissoesPorCategoria(permissoes: PermissaoCatalogoItem[]) {
  const buckets = new Map<string, PermissaoCatalogoItem[]>();
  for (const categoria of CATEGORIAS_ACAO) {
    buckets.set(categoria.id, []);
  }

  for (const permissao of permissoes) {
    const categoriaId = getCategoriaPermissao(permissao.nome);
    buckets.get(categoriaId)?.push(permissao);
  }

  return CATEGORIAS_ACAO.map((categoria) => ({
    id: categoria.id,
    rotulo: categoria.rotulo,
    permissoes: (buckets.get(categoria.id) ?? []).sort(ordenarPermissoes),
  })).filter((categoria) => categoria.permissoes.length > 0);
}

function formatarNomePermissao(nomePermissao: string) {
  return nomePermissao.toLowerCase().replaceAll("_", " ");
}

function coletarIdsComDependencias(
  permissaoIds: number[],
  permissaoById: Map<number, PermissaoCatalogoItem>,
  permissaoIdByNome: Map<string, number>,
) {
  const resultado = new Set<number>(permissaoIds);
  for (const id of permissaoIds) {
    const permissao = permissaoById.get(id);
    if (!permissao) continue;
    const nomePai = getNomePermissaoPai(permissao.nome, permissaoIdByNome);
    if (!nomePai) continue;
    const paiId = permissaoIdByNome.get(nomePai);
    if (paiId) resultado.add(paiId);
  }

  return Array.from(resultado).sort((a, b) => a - b);
}

export function GerenciarTemplates() {
  const [perfis, setPerfis] = useState<PerfilTemplate[]>([]);
  const [permissoesAgrupadas, setPermissoesAgrupadas] = useState<PermissaoModuloGrupo[]>([]);
  const [perfilSelecionado, setPerfilSelecionado] = useState<number | "">("");
  const [permissoesMarcadas, setPermissoesMarcadas] = useState<number[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [isSaving, setIsSaving] = useState(false);
  const [erro, setErro] = useState("");
  const [sucesso, setSucesso] = useState("");
  const [filtroPermissao, setFiltroPermissao] = useState("");
  const [mostrarSomenteSelecionadas, setMostrarSomenteSelecionadas] = useState(false);

  useEffect(() => {
    let cancelado = false;
    (async () => {
      setIsLoading(true);
      setErro("");
      try {
        const [listaPerfis, grupos] = await Promise.all([getPerfis(), getPermissoesAgrupadas()]);
        if (cancelado) return;
        setPerfis(listaPerfis);
        setPermissoesAgrupadas(grupos);
      } catch (e) {
        if (!cancelado) {
          setErro(getApiErrorMessage(e, "Nao foi possivel carregar os dados de templates."));
        }
      } finally {
        if (!cancelado) setIsLoading(false);
      }
    })();
    return () => {
      cancelado = true;
    };
  }, []);

  const carregarMarcacoesDoPerfil = useCallback(async (id: number) => {
    setErro("");
    setSucesso("");
    try {
      const res = await getPermissoesDoPerfil(id);
      setPermissoesMarcadas([...res.permissaoIds]);
    } catch (e) {
      setPermissoesMarcadas([]);
      setErro(getApiErrorMessage(e, "Nao foi possivel carregar as permissoes deste perfil."));
    }
  }, []);

  useEffect(() => {
    if (perfilSelecionado === "") {
      setPermissoesMarcadas([]);
      return;
    }
    void carregarMarcacoesDoPerfil(perfilSelecionado);
  }, [perfilSelecionado, carregarMarcacoesDoPerfil]);

  const permissoesCatalogo = useMemo(
    () => permissoesAgrupadas.flatMap((grupo) => grupo.permissoes),
    [permissoesAgrupadas],
  );

  const permissaoById = useMemo(
    () => new Map(permissoesCatalogo.map((permissao) => [permissao.id, permissao])),
    [permissoesCatalogo],
  );

  const permissaoIdByNome = useMemo(
    () => new Map(permissoesCatalogo.map((permissao) => [permissao.nome, permissao.id])),
    [permissoesCatalogo],
  );

  const dependenciasByFilhaId = useMemo(() => {
    const mapa = new Map<number, number>();
    for (const permissao of permissoesCatalogo) {
      const nomePai = getNomePermissaoPai(permissao.nome, permissaoIdByNome);
      if (!nomePai) continue;
      const idPai = permissaoIdByNome.get(nomePai);
      if (!idPai) continue;
      mapa.set(permissao.id, idPai);
    }
    return mapa;
  }, [permissoesCatalogo, permissaoIdByNome]);

  const marcadasSet = useMemo(() => new Set(permissoesMarcadas), [permissoesMarcadas]);
  const termoFiltro = filtroPermissao.trim().toLowerCase();

  const gruposFiltrados = useMemo(() => {
    return permissoesAgrupadas
      .map((grupo) => {
        const permissoes = grupo.permissoes.filter((perm) => {
            if (mostrarSomenteSelecionadas && !marcadasSet.has(perm.id)) {
              return false;
            }
            if (!termoFiltro) return true;
            const texto = `${perm.nome} ${perm.descricao ?? ""}`.toLowerCase();
            return texto.includes(termoFiltro);
          });

        return {
          ...grupo,
          permissoes,
          categorias: agruparPermissoesPorCategoria(permissoes),
        };
      })
      .filter((grupo) => grupo.permissoes.length > 0 && grupo.categorias.length > 0);
  }, [marcadasSet, mostrarSomenteSelecionadas, permissoesAgrupadas, termoFiltro]);

  function handleSelecionarPerfil(idStr: string) {
    setSucesso("");
    if (idStr === "") {
      setPerfilSelecionado("");
      setFiltroPermissao("");
      setMostrarSomenteSelecionadas(false);
      return;
    }

    const id = Number(idStr);
    if (!Number.isFinite(id)) {
      setPerfilSelecionado("");
      return;
    }

    setPerfilSelecionado(id);
  }

  function handleTogglePermissao(permissao: PermissaoCatalogoItem, marcado: boolean) {
    setPermissoesMarcadas((prev) => {
      const next = new Set(prev);

      if (marcado) {
        next.add(permissao.id);
        const nomePai = getNomePermissaoPai(permissao.nome, permissaoIdByNome);
        if (nomePai) {
          const paiId = permissaoIdByNome.get(nomePai);
          if (paiId) next.add(paiId);
        }
      } else {
        next.delete(permissao.id);

        for (const [filhaId, paiId] of dependenciasByFilhaId.entries()) {
          if (paiId === permissao.id) {
            next.delete(filhaId);
          }
        }
      }

      return Array.from(next).sort((a, b) => a - b);
    });
  }

  function handleMarcarModulo(permissoesDoModulo: PermissaoCatalogoItem[]) {
    setPermissoesMarcadas((prev) => {
      const idsModulo = permissoesDoModulo.map((perm) => perm.id);
      return coletarIdsComDependencias([...prev, ...idsModulo], permissaoById, permissaoIdByNome);
    });
  }

  function handleLimparModulo(permissoesDoModulo: PermissaoCatalogoItem[]) {
    const idsModulo = new Set(permissoesDoModulo.map((perm) => perm.id));
    setPermissoesMarcadas((prev) => {
      const next = new Set(prev.filter((id) => !idsModulo.has(id)));
      for (const [filhaId, paiId] of dependenciasByFilhaId.entries()) {
        if (!next.has(paiId)) {
          next.delete(filhaId);
        }
      }
      return Array.from(next).sort((a, b) => a - b);
    });
  }

  async function handleSalvar() {
    if (perfilSelecionado === "") {
      setErro("Selecione um perfil de template antes de salvar.");
      return;
    }
    setIsSaving(true);
    setErro("");
    setSucesso("");
    try {
      await salvarPermissoes(perfilSelecionado, permissoesMarcadas);
      setSucesso("Permissoes salvas com sucesso.");
    } catch (e) {
      setErro(getApiErrorMessage(e, "Nao foi possivel salvar as permissoes."));
    } finally {
      setIsSaving(false);
    }
  }

  if (isLoading) {
    return (
      <div className="rounded-xl border border-zinc-200 bg-white p-10 text-center text-sm text-zinc-500">
        Carregando templates...
      </div>
    );
  }

  return (
    <div className="max-w-5xl mx-auto flex flex-col gap-6">
      <div className="rounded-2xl border border-indigo-100 bg-gradient-to-r from-indigo-50 via-slate-50 to-white p-6 shadow-sm">
        <h1 className="text-xl font-semibold text-zinc-900 tracking-tight">Gestao de templates</h1>
        <p className="text-sm text-zinc-600 mt-1 max-w-3xl">
          Defina quais permissoes entram em cada perfil padrao. Ao marcar uma acao, a permissao de visualizacao
          correspondente e adicionada automaticamente.
        </p>
      </div>

      {(erro || sucesso) && (
        <div
          className={`rounded-lg border px-4 py-3 text-sm ${
            erro
              ? "border-red-200 bg-red-50 text-red-800"
              : "border-emerald-200 bg-emerald-50 text-emerald-800"
          }`}
          role="status"
        >
          {erro || sucesso}
        </div>
      )}

      <div className="rounded-xl border border-zinc-200 bg-white p-5 shadow-sm">
        <label htmlFor="perfil-template" className="block text-sm font-medium text-zinc-700 mb-2">
          Perfil de template
        </label>
        <select
          id="perfil-template"
          value={perfilSelecionado === "" ? "" : String(perfilSelecionado)}
          onChange={(e) => handleSelecionarPerfil(e.target.value)}
          className="w-full max-w-md h-11 border border-zinc-300 rounded-lg px-3 text-sm bg-white text-zinc-900 focus:outline-none focus:ring-2 focus:ring-indigo-500/30 focus:border-indigo-500"
        >
          <option value="">Selecione um perfil...</option>
          {perfis.map((p) => (
            <option key={p.id} value={p.id}>
              {p.nome}
            </option>
          ))}
        </select>
      </div>

      {perfilSelecionado !== "" && (
        <div className="flex flex-col gap-6">
          <div className="rounded-xl border border-zinc-200 bg-white p-4 shadow-sm flex flex-col gap-3 md:flex-row md:items-center md:justify-between">
            <div className="relative w-full md:max-w-md">
              <input
                type="search"
                value={filtroPermissao}
                onChange={(e) => setFiltroPermissao(e.target.value)}
                placeholder="Buscar permissao por nome ou descricao..."
                className="h-10 w-full border border-zinc-300 rounded-lg pl-9 pr-3 text-sm outline-none focus:border-indigo-500 focus:ring-2 focus:ring-indigo-500/20"
              />
              <svg
                aria-hidden="true"
                className="absolute left-3 top-1/2 -translate-y-1/2 text-zinc-400"
                width="16"
                height="16"
                viewBox="0 0 24 24"
                fill="none"
                stroke="currentColor"
                strokeWidth="2"
              >
                <circle cx="11" cy="11" r="7" />
                <line x1="16.65" y1="16.65" x2="21" y2="21" />
              </svg>
            </div>
            <label className="inline-flex items-center gap-2 text-sm text-zinc-700">
              <input
                type="checkbox"
                checked={mostrarSomenteSelecionadas}
                onChange={(e) => setMostrarSomenteSelecionadas(e.target.checked)}
                className="h-4 w-4 rounded border-zinc-300 text-indigo-600 focus:ring-indigo-500"
              />
              Mostrar somente selecionadas
            </label>
          </div>

          {gruposFiltrados.length === 0 && (
            <div className="rounded-xl border border-zinc-200 bg-white px-5 py-10 text-center shadow-sm">
              <p className="text-sm font-medium text-zinc-700">Nenhuma permissao encontrada.</p>
              <p className="text-xs text-zinc-500 mt-1">Ajuste os filtros para visualizar mais itens.</p>
            </div>
          )}

          {gruposFiltrados.map((grupo) => {
            const totalGrupo = grupo.permissoes.length;
            const selecionadasGrupo = grupo.permissoes.filter((perm) => marcadasSet.has(perm.id)).length;

            return (
              <section
                key={grupo.modulo}
                className="rounded-xl border border-zinc-200 bg-white shadow-sm overflow-hidden"
              >
                <header className="px-5 py-3 border-b border-zinc-100 bg-zinc-50/80 flex flex-col gap-3 md:flex-row md:items-center md:justify-between">
                  <div>
                    <h2 className="text-sm font-semibold text-zinc-800 tracking-wide uppercase">
                      {grupo.moduloRotulo}
                    </h2>
                    <p className="text-[11px] text-zinc-500 mt-0.5">
                      {selecionadasGrupo} de {totalGrupo} selecionadas
                    </p>
                  </div>
                  <div className="flex items-center gap-2">
                    <button
                      type="button"
                      onClick={() => handleMarcarModulo(grupo.permissoes)}
                      className="h-8 px-3 rounded-md border border-zinc-300 text-xs font-medium text-zinc-700 hover:bg-zinc-100 transition-colors"
                    >
                      Marcar modulo
                    </button>
                    <button
                      type="button"
                      onClick={() => handleLimparModulo(grupo.permissoes)}
                      className="h-8 px-3 rounded-md border border-zinc-300 text-xs font-medium text-zinc-700 hover:bg-zinc-100 transition-colors"
                    >
                      Limpar modulo
                    </button>
                  </div>
                </header>
                <div className="p-4 flex flex-col gap-3 bg-zinc-50/40">
                  {grupo.categorias.map((categoria) => (
                    <div key={`${grupo.modulo}-${categoria.id}`} className="rounded-lg border border-zinc-200 bg-white overflow-hidden">
                      <div className="px-4 py-2.5 border-b border-zinc-100 bg-zinc-50">
                        <p className="text-xs font-semibold uppercase tracking-wide text-zinc-600">
                          {categoria.rotulo}
                        </p>
                      </div>
                      <ul className="divide-y divide-zinc-100">
                        {categoria.permissoes.map((perm) => {
                          const marcado = marcadasSet.has(perm.id);
                          const paiId = dependenciasByFilhaId.get(perm.id);
                          const permissaoPai = paiId ? permissaoById.get(paiId) : null;
                          const bloqueadoPorDependencia = Boolean(
                            paiId && permissaoPai && !marcadasSet.has(paiId),
                          );

                          return (
                            <li
                              key={perm.id}
                              className={`px-4 py-3 flex gap-3 items-start transition-colors ${
                                bloqueadoPorDependencia ? "bg-zinc-50/80" : "hover:bg-zinc-50/50"
                              }`}
                            >
                              <input
                                type="checkbox"
                                id={`perm-${perm.id}`}
                                checked={marcado}
                                disabled={bloqueadoPorDependencia}
                                onChange={(e) => handleTogglePermissao(perm, e.target.checked)}
                                className="mt-1 h-4 w-4 rounded border-zinc-300 text-indigo-600 focus:ring-indigo-500 disabled:opacity-60 disabled:cursor-not-allowed"
                              />
                              <label
                                htmlFor={`perm-${perm.id}`}
                                className={`flex-1 min-w-0 ${bloqueadoPorDependencia ? "cursor-not-allowed" : "cursor-pointer"}`}
                              >
                                <span className="font-semibold text-zinc-900 text-sm block">{perm.nome}</span>
                                <span className="text-[11px] uppercase tracking-wide text-zinc-500 block mt-0.5">
                                  {formatarNomePermissao(perm.nome)}
                                </span>
                                {perm.descricao ? (
                                  <span className="text-xs text-zinc-500 block mt-0.5 leading-relaxed">{perm.descricao}</span>
                                ) : null}
                                {permissaoPai ? (
                                  <span className={`text-[11px] mt-1 inline-block ${
                                    bloqueadoPorDependencia ? "text-amber-700" : "text-indigo-700"
                                  }`}>
                                    Permissao pai: <strong>{permissaoPai.nome}</strong>
                                    {bloqueadoPorDependencia ? " (marque a pai para liberar)" : ""}
                                  </span>
                                ) : null}
                              </label>
                            </li>
                          );
                        })}
                      </ul>
                    </div>
                  ))}
                </div>
              </section>
            );
          })}
        </div>
      )}

      {perfilSelecionado !== "" && (
        <div className="sticky bottom-4 z-10">
          <div className="flex flex-col gap-3 rounded-xl border border-zinc-200 bg-white/95 backdrop-blur px-4 py-3 shadow-lg md:flex-row md:items-center md:justify-between">
            <span className="text-xs text-zinc-600">
              {permissoesMarcadas.length} permissao(oes) selecionada(s)
            </span>
            <button
              type="button"
              onClick={() => void handleSalvar()}
              disabled={isSaving}
              className="inline-flex items-center justify-center h-10 px-5 rounded-lg text-sm font-semibold text-white bg-indigo-600 hover:bg-indigo-700 disabled:opacity-50 disabled:pointer-events-none shadow-sm transition-colors"
            >
              {isSaving ? "Salvando..." : "Salvar permissoes"}
            </button>
          </div>
        </div>
      )}
    </div>
  );
}
