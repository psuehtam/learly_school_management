"use client";

import { useCallback, useEffect, useMemo, useState } from "react";
import { getCurrentUser } from "@/lib/api/auth";
import type { User } from "@/lib/api/types";
import { ApiError } from "@/lib/api/client";
import { atualizarLivroEscola, criarLivroEscola, listarLivrosEscola, obterLivroEscola, type LivroEscolaDto } from "@/lib/api/livros";
import { hasPermission } from "@/lib/permissions";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Modal } from "@/components/ui/modal";

function erroApi(e: unknown): string {
  if (e instanceof ApiError) {
    const data = e.data;
    if (typeof data === "object" && data !== null && "detail" in data) {
      const d = (data as { detail?: unknown }).detail;
      if (typeof d === "string" && d.trim()) return d.trim();
    }
  }
  if (e instanceof Error && e.message) return e.message;
  return "Não foi possível concluir a operação.";
}

function linhasAulasPadrao(qtd: number, padrao: string): string[] {
  return Array.from({ length: qtd }, () => padrao);
}

function ajustarLinhasAulas(prev: string[], qtd: number, padrao: string): string[] {
  const next = prev.slice(0, qtd);
  while (next.length < qtd) next.push(padrao);
  return next;
}

function parseAulasPorCapitulo(
  linhas: string[],
  qtd: number,
  maxPorCap: number,
): number[] | null {
  if (linhas.length !== qtd) return null;
  const out: number[] = [];
  for (let i = 0; i < qtd; i++) {
    const n = Number.parseInt(linhas[i] ?? "", 10);
    if (!Number.isFinite(n) || n < 1 || n > maxPorCap) return null;
    out.push(n);
  }
  return out;
}

export default function BooksPage() {
  const maxCapitulos = 200;
  const maxAulasPrevistasPorCapitulo = 500;
  const aulasPadraoNovaLinha = "4";

  const [user, setUser] = useState<User | null>(null);
  const [livros, setLivros] = useState<LivroEscolaDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [filtroStatus, setFiltroStatus] = useState<"todos" | "ativo" | "inativo">("todos");

  const [modalOpen, setModalOpen] = useState(false);
  const [livroEditando, setLivroEditando] = useState<LivroEscolaDto | null>(null);
  const [livroDetalhe, setLivroDetalhe] = useState<LivroEscolaDto | null>(null);
  const [carregandoDetalheLivro, setCarregandoDetalheLivro] = useState(false);
  const [aulasEdicaoLinhas, setAulasEdicaoLinhas] = useState<string[]>([]);
  const [capitulosNovosDraft, setCapitulosNovosDraft] = useState<{ nome: string; aulas: string }[]>([]);
  const [nomeCampo, setNomeCampo] = useState("");
  const [capitulosCampo, setCapitulosCampo] = useState("12");
  const [aulasPorCapituloLinhas, setAulasPorCapituloLinhas] = useState<string[]>(() => linhasAulasPadrao(12, "4"));
  const [salvandoModal, setSalvandoModal] = useState(false);
  const [livrosModalBaselineKey, setLivrosModalBaselineKey] = useState("");

  const podeCriar = user ? hasPermission(user, "CRIAR_LIVRO") : false;
  const podeEditar = user ? hasPermission(user, "EDITAR_LIVRO") : false;

  const carregar = useCallback(async () => {
    setLoading(true);
    try {
      const dados = await listarLivrosEscola();
      setLivros(dados);
    } catch {
      setLivros([]);
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    void getCurrentUser()
      .then(setUser)
      .catch(() => setUser(null));
  }, []);

  useEffect(() => {
    void carregar();
  }, [carregar]);

  useEffect(() => {
    if (!modalOpen) setLivrosModalBaselineKey("");
  }, [modalOpen]);

  useEffect(() => {
    if (!modalOpen || !livroEditando || !livroDetalhe || carregandoDetalheLivro) return;
    const caps = [...(livroDetalhe.capitulos ?? [])].sort((a, b) => a.id - b.id);
    if (aulasEdicaoLinhas.length !== caps.length) return;
    setLivrosModalBaselineKey((prev) => {
      if (prev !== "") return prev;
      return JSON.stringify({
        k: "e",
        nome: nomeCampo.trim(),
        caps: caps.map((c, i) => `${c.id}:${(aulasEdicaoLinhas[i] ?? "").trim()}`).join("|"),
        novos: capitulosNovosDraft.map((r) => `${r.nome.trim()}\t${r.aulas.trim()}`).join(";"),
      });
    });
  }, [
    modalOpen,
    livroEditando,
    livroDetalhe,
    carregandoDetalheLivro,
    aulasEdicaoLinhas,
    nomeCampo,
    capitulosNovosDraft,
  ]);

  const livrosFiltrados = livros.filter((l) =>
    filtroStatus === "todos" ? true : filtroStatus === "ativo" ? l.status === "Ativo" : l.status === "Inativo",
  );

  function abrirNovo() {
    setLivroEditando(null);
    setLivroDetalhe(null);
    setAulasEdicaoLinhas([]);
    setCapitulosNovosDraft([]);
    setCarregandoDetalheLivro(false);
    setNomeCampo("");
    setCapitulosCampo("12");
    setAulasPorCapituloLinhas(linhasAulasPadrao(12, "4"));
    setLivrosModalBaselineKey(
      JSON.stringify({
        k: "n",
        nome: "",
        cap: "12",
        linhas: linhasAulasPadrao(12, "4"),
        novos: "",
      }),
    );
    setModalOpen(true);
  }

  function abrirEditar(l: LivroEscolaDto) {
    setLivrosModalBaselineKey("");
    setLivroEditando(l);
    setLivroDetalhe(null);
    setAulasEdicaoLinhas([]);
    setCapitulosNovosDraft([]);
    setNomeCampo(l.nome);
    setModalOpen(true);
    setCarregandoDetalheLivro(true);
    void obterLivroEscola(l.id)
      .then((d) => {
        const caps = [...(d.capitulos ?? [])].sort((a, b) => a.id - b.id);
        setLivroDetalhe({ ...d, capitulos: caps });
        setAulasEdicaoLinhas(caps.map((c) => String(c.qtdAulasPrevistas)));
      })
      .catch(() => {
        setLivroDetalhe(null);
        setAulasEdicaoLinhas([]);
      })
      .finally(() => setCarregandoDetalheLivro(false));
  }

  function fecharModal() {
    if (salvandoModal) return;
    setModalOpen(false);
    setLivroEditando(null);
    setLivroDetalhe(null);
    setAulasEdicaoLinhas([]);
    setCapitulosNovosDraft([]);
    setCarregandoDetalheLivro(false);
  }

  function onCapitulosBlur() {
    const qtd = Number.parseInt(capitulosCampo, 10);
    if (!Number.isFinite(qtd) || qtd < 1 || qtd > maxCapitulos) return;
    setAulasPorCapituloLinhas((prev) => ajustarLinhasAulas(prev, qtd, aulasPadraoNovaLinha));
  }

  async function salvarModal() {
    const nome = nomeCampo.trim();
    if (!nome) return;

    setSalvandoModal(true);
    try {
      if (livroEditando) {
        if (!livroDetalhe) {
          alert("Aguarde o carregamento do livro ou feche e abra o modal novamente.");
          return;
        }
        const caps = [...(livroDetalhe.capitulos ?? [])].sort((a, b) => a.id - b.id);
        const capitulosNovosPayload =
          capitulosNovosDraft.length > 0
            ? capitulosNovosDraft.map((r) => {
                const qtd = Number.parseInt(r.aulas, 10);
                const item: { nome?: string; qtdAulasPrevistas: number } = { qtdAulasPrevistas: qtd };
                if (r.nome.trim()) item.nome = r.nome.trim();
                return item;
              })
            : undefined;

        if (capitulosNovosPayload) {
          const novosInvalidos = capitulosNovosPayload.some(
            (x) =>
              !Number.isFinite(x.qtdAulasPrevistas) ||
              x.qtdAulasPrevistas < 1 ||
              x.qtdAulasPrevistas > maxAulasPrevistasPorCapitulo,
          );
          if (novosInvalidos) {
            alert(`Cada capítulo novo precisa de entre 1 e ${maxAulasPrevistasPorCapitulo} aulas previstas.`);
            return;
          }
          if (caps.length + capitulosNovosPayload.length > maxCapitulos) {
            alert(`O livro não pode ultrapassar ${maxCapitulos} capítulos no total.`);
            return;
          }
        }

        if (caps.length > 0) {
          if (aulasEdicaoLinhas.length !== caps.length) {
            alert("Quantidade de valores de aulas não confere com os capítulos.");
            return;
          }
          const capitulosAulas = caps.map((c, i) => ({
            capituloId: c.id,
            qtdAulasPrevistas: Number.parseInt(aulasEdicaoLinhas[i] ?? "", 10),
          }));
          if (
            capitulosAulas.some(
              (x) =>
                !Number.isFinite(x.qtdAulasPrevistas) ||
                x.qtdAulasPrevistas < 1 ||
                x.qtdAulasPrevistas > maxAulasPrevistasPorCapitulo,
            )
          ) {
            alert(`Cada capítulo precisa de aulas previstas entre 1 e ${maxAulasPrevistasPorCapitulo}.`);
            return;
          }
          await atualizarLivroEscola(livroEditando.id, {
            nome,
            capitulosAulas,
            ...(capitulosNovosPayload ? { capitulosNovos: capitulosNovosPayload } : {}),
          });
        } else if (capitulosNovosPayload?.length) {
          await atualizarLivroEscola(livroEditando.id, { nome, capitulosNovos: capitulosNovosPayload });
        } else {
          await atualizarLivroEscola(livroEditando.id, { nome });
        }
      } else {
        const qtd = Number.parseInt(capitulosCampo, 10);
        if (!Number.isFinite(qtd) || qtd < 1 || qtd > maxCapitulos) {
          alert(`Informe uma quantidade de capítulos entre 1 e ${maxCapitulos}.`);
          return;
        }
        const linhas = ajustarLinhasAulas(aulasPorCapituloLinhas, qtd, aulasPadraoNovaLinha);
        const aulasArr = parseAulasPorCapitulo(linhas, qtd, maxAulasPrevistasPorCapitulo);
        if (!aulasArr) {
          alert(`Cada capítulo precisa de aulas previstas entre 1 e ${maxAulasPrevistasPorCapitulo}.`);
          return;
        }
        await criarLivroEscola({
          nome,
          quantidadeCapitulos: qtd,
          aulasPrevistasPorCapitulo: aulasArr,
        });
      }
      setModalOpen(false);
      setLivroEditando(null);
      setLivroDetalhe(null);
      setAulasEdicaoLinhas([]);
      setCapitulosNovosDraft([]);
      await carregar();
    } catch (e) {
      alert(erroApi(e));
    } finally {
      setSalvandoModal(false);
    }
  }

  async function toggleStatus(l: LivroEscolaDto) {
    const proximo = l.status === "Ativo" ? "Inativo" : "Ativo";
    try {
      await atualizarLivroEscola(l.id, { status: proximo });
      await carregar();
    } catch (e) {
      alert(erroApi(e));
    }
  }

  const qtdCapParsed = Number.parseInt(capitulosCampo, 10);
  const qtdCapValida =
    Number.isFinite(qtdCapParsed) && qtdCapParsed >= 1 && qtdCapParsed <= maxCapitulos;
  const linhasAjustadasPreview = qtdCapValida
    ? ajustarLinhasAulas(aulasPorCapituloLinhas, qtdCapParsed, aulasPadraoNovaLinha)
    : [];
  const aulasArrPreview =
    qtdCapValida && !livroEditando
      ? parseAulasPorCapitulo(linhasAjustadasPreview, qtdCapParsed, maxAulasPrevistasPorCapitulo)
      : null;
  const linhasForm =
    !livroEditando && qtdCapValida
      ? ajustarLinhasAulas(aulasPorCapituloLinhas, qtdCapParsed, aulasPadraoNovaLinha)
      : !livroEditando
        ? aulasPorCapituloLinhas
        : [];

  const livrosModalCurrentKey = useMemo(() => {
    if (!modalOpen) return "";
    const novosKey = capitulosNovosDraft.map((r) => `${r.nome.trim()}\t${r.aulas.trim()}`).join(";");
    if (livroEditando) {
      if (!livroDetalhe || carregandoDetalheLivro) return "";
      const caps = [...(livroDetalhe.capitulos ?? [])].sort((a, b) => a.id - b.id);
      return JSON.stringify({
        k: "e",
        nome: nomeCampo.trim(),
        caps: caps.map((c, i) => `${c.id}:${(aulasEdicaoLinhas[i] ?? "").trim()}`).join("|"),
        novos: novosKey,
      });
    }
    const linhasSnap = qtdCapValida
      ? ajustarLinhasAulas(aulasPorCapituloLinhas, qtdCapParsed, aulasPadraoNovaLinha)
      : aulasPorCapituloLinhas;
    return JSON.stringify({
      k: "n",
      nome: nomeCampo.trim(),
      cap: capitulosCampo.trim(),
      linhas: linhasSnap,
      novos: novosKey,
    });
  }, [
    modalOpen,
    livroEditando,
    livroDetalhe,
    carregandoDetalheLivro,
    nomeCampo,
    capitulosNovosDraft,
    aulasEdicaoLinhas,
    capitulosCampo,
    aulasPorCapituloLinhas,
    qtdCapValida,
    qtdCapParsed,
  ]);

  const livrosModalTemAlteracao =
    modalOpen &&
    !salvandoModal &&
    livrosModalBaselineKey !== "" &&
    livrosModalCurrentKey !== "" &&
    livrosModalCurrentKey !== livrosModalBaselineKey;

  const podeCadastrarNovo = Boolean(nomeCampo.trim()) && qtdCapValida && aulasArrPreview !== null;

  const capsEdicaoOrdenados = livroEditando
    ? [...(livroDetalhe?.capitulos ?? [])].sort((a, b) => a.id - b.id)
    : [];

  const novosDraftOk =
    capitulosNovosDraft.length === 0 ||
    capitulosNovosDraft.every((r) => {
      const n = Number.parseInt(r.aulas, 10);
      return Number.isFinite(n) && n >= 1 && n <= maxAulasPrevistasPorCapitulo;
    });

  const aulasExistentesOk =
    capsEdicaoOrdenados.length === 0 ||
    (aulasEdicaoLinhas.length === capsEdicaoOrdenados.length &&
      capsEdicaoOrdenados.every((_, i) => {
        const n = Number.parseInt(aulasEdicaoLinhas[i] ?? "", 10);
        return Number.isFinite(n) && n >= 1 && n <= maxAulasPrevistasPorCapitulo;
      }));

  const totalCapitulosPrevisto = capsEdicaoOrdenados.length + capitulosNovosDraft.length;

  const edicaoLivroPodeSalvar =
    Boolean(nomeCampo.trim()) &&
    livroDetalhe !== null &&
    !carregandoDetalheLivro &&
    novosDraftOk &&
    aulasExistentesOk &&
    totalCapitulosPrevisto <= maxCapitulos;

  return (
    <div className="flex h-full flex-col gap-6 overflow-auto">
      <div className="flex flex-shrink-0 flex-wrap items-start justify-between gap-4">
        <div>
          <h1 className="text-xl font-semibold text-zinc-900">Livros</h1>
          <p className="mt-0.5 max-w-xl text-sm text-zinc-500">
            Cadastro de níveis/livros da escola. Ao criar um livro, são gerados capítulos na tabela{" "}
            <code className="rounded bg-zinc-100 px-1 py-0.5 text-xs">capitulos</code> com{" "}
            <code className="rounded bg-zinc-100 px-1 py-0.5 text-xs">qtd_aulas_previstas</code> definida por capítulo.
            As aulas de turma continuam em <code className="rounded bg-zinc-100 px-1 py-0.5 text-xs">aulas</code>{" "}
            (agenda).
          </p>
        </div>
        {podeCriar ? (
          <Button type="button" onClick={abrirNovo} className="flex-shrink-0">
            Novo livro
          </Button>
        ) : null}
      </div>

      <div className="flex flex-shrink-0 flex-wrap gap-2">
        {(["todos", "ativo", "inativo"] as const).map((s) => (
          <button
            key={s}
            type="button"
            onClick={() => setFiltroStatus(s)}
            className={`h-8 rounded-lg border px-4 text-sm font-medium transition-colors ${
              filtroStatus === s
                ? "border-[#1F2A35] bg-[#1F2A35] text-white"
                : "border-zinc-300 text-zinc-600 hover:bg-zinc-50"
            }`}
          >
            {s === "todos" ? "Todos" : s === "ativo" ? "Ativos" : "Inativos"}
          </button>
        ))}
      </div>

      {loading ? (
        <p className="text-sm text-zinc-500">Carregando…</p>
      ) : livrosFiltrados.length === 0 ? (
        <div className="rounded-xl border border-dashed border-zinc-200 bg-zinc-50/80 py-14 text-center text-sm text-zinc-500">
          Nenhum livro neste filtro. {podeCriar ? "Clique em Novo livro para cadastrar." : ""}
        </div>
      ) : (
        <div className="flex flex-col gap-2 pb-8">
          {livrosFiltrados.map((l) => (
            <div
              key={l.id}
              className="flex flex-wrap items-center justify-between gap-3 rounded-xl border border-zinc-200 bg-white px-4 py-4"
            >
              <div>
                <div className="flex flex-wrap items-center gap-2">
                  <span className="font-semibold text-zinc-900">{l.nome}</span>
                  <span
                    className={`inline-flex items-center gap-1 rounded-full px-2 py-0.5 text-xs font-medium ${
                      l.status === "Ativo" ? "bg-green-50 text-green-700" : "bg-zinc-100 text-zinc-600"
                    }`}
                  >
                    {l.status}
                  </span>
                </div>
                <p className="mt-1 text-xs text-zinc-500">
                  {l.quantidadeCapitulos} capítulo{l.quantidadeCapitulos === 1 ? "" : "s"} · {l.totalAulasPrevistas}{" "}
                  aula{l.totalAulasPrevistas === 1 ? "" : "s"} prevista{l.totalAulasPrevistas === 1 ? "" : "s"} (soma por
                  capítulo)
                </p>
              </div>
              {podeEditar ? (
                <div className="flex flex-wrap gap-2">
                  <Button type="button" variant="secondary" size="sm" onClick={() => abrirEditar(l)}>
                    Editar livro
                  </Button>
                  <Button
                    type="button"
                    variant={l.status === "Ativo" ? "danger" : "secondary"}
                    size="sm"
                    onClick={() => void toggleStatus(l)}
                  >
                    {l.status === "Ativo" ? "Inativar" : "Reativar"}
                  </Button>
                </div>
              ) : null}
            </div>
          ))}
        </div>
      )}

      <Modal
        open={modalOpen}
        onClose={fecharModal}
        title={livroEditando ? "Editar livro" : "Novo livro"}
        hasUnsavedChanges={livrosModalTemAlteracao}
        closeDisabled={salvandoModal}
        footer={(requestClose) => (
          <>
            <Button type="button" variant="secondary" disabled={salvandoModal} onClick={requestClose}>
              Cancelar
            </Button>
            <Button
              type="button"
              isLoading={salvandoModal}
              disabled={livroEditando ? !edicaoLivroPodeSalvar : !podeCadastrarNovo}
              onClick={() => void salvarModal()}
            >
              {livroEditando ? "Salvar" : "Cadastrar"}
            </Button>
          </>
        )}
      >
        <Input
          label="Nome"
          placeholder="Ex.: Book 1, Teens A"
          value={nomeCampo}
          onChange={(e) => setNomeCampo(e.target.value)}
        />
        {livroEditando ? (
          <div className="mt-4 space-y-4">
            {carregandoDetalheLivro ? (
              <p className="text-sm text-zinc-500">Carregando capítulos…</p>
            ) : !livroDetalhe ? (
              <p className="text-sm text-amber-800">
                Não foi possível carregar o livro. Feche o modal e tente novamente.
              </p>
            ) : (
              <>
                {capsEdicaoOrdenados.length > 0 ? (
                  <div className="space-y-2">
                    <p className="text-xs font-medium text-zinc-700">Capítulos atuais — aulas previstas</p>
                    <p className="text-xs text-zinc-500">
                      Valores de 1 a {maxAulasPrevistasPorCapitulo}. Ao salvar, capítulos novos (abaixo) são criados no
                      final.
                    </p>
                    <div className="max-h-48 space-y-2 overflow-y-auto rounded-lg border border-zinc-200 bg-zinc-50/50 p-3">
                      {capsEdicaoOrdenados.map((c, i) => (
                        <div key={c.id} className="flex flex-wrap items-end gap-2 sm:flex-nowrap sm:items-center">
                          <span
                            className="min-w-0 flex-1 truncate text-xs font-medium text-zinc-700"
                            title={c.nome}
                          >
                            {c.nome}
                          </span>
                          <Input
                            id={`livro-edit-aulas-${c.id}`}
                            className="w-24 shrink-0 sm:w-28"
                            type="number"
                            min={1}
                            max={maxAulasPrevistasPorCapitulo}
                            inputMode="numeric"
                            value={aulasEdicaoLinhas[i] ?? ""}
                            onChange={(e) => {
                              const v = e.target.value;
                              setAulasEdicaoLinhas((prev) => {
                                const next = [...prev];
                                while (next.length <= i) next.push("");
                                next[i] = v;
                                return next;
                              });
                            }}
                            aria-label={`Aulas previstas — ${c.nome}`}
                          />
                        </div>
                      ))}
                    </div>
                  </div>
                ) : (
                  <p className="text-sm text-zinc-600">Este livro ainda não tem capítulos. Adicione abaixo.</p>
                )}

                <div className="space-y-2 border-t border-zinc-200 pt-4">
                  <div className="flex flex-wrap items-center justify-between gap-2">
                    <p className="text-xs font-medium text-zinc-700">Novos capítulos (no final do livro)</p>
                    <Button
                      type="button"
                      variant="secondary"
                      size="sm"
                      disabled={totalCapitulosPrevisto >= maxCapitulos}
                      onClick={() =>
                        setCapitulosNovosDraft((d) => [...d, { nome: "", aulas: aulasPadraoNovaLinha }])
                      }
                    >
                      + Capítulo
                    </Button>
                  </div>
                  <p className="text-xs text-zinc-500">
                    Nome opcional (vazio = &quot;Capítulo N&quot; no servidor). Limite total do livro: {maxCapitulos}{" "}
                    capítulos (atual + novos: {totalCapitulosPrevisto}).
                  </p>
                  {capitulosNovosDraft.length > 0 ? (
                    <div className="max-h-40 space-y-2 overflow-y-auto rounded-lg border border-zinc-200 bg-white p-3">
                      {capitulosNovosDraft.map((row, i) => (
                        <div key={i} className="flex flex-wrap items-end gap-2 border-b border-zinc-100 pb-2 last:border-0 last:pb-0">
                          <Input
                            label={i === 0 ? "Nome (opcional)" : undefined}
                            className="min-w-[8rem] flex-1"
                            placeholder="Ex.: Revisão"
                            value={row.nome}
                            onChange={(e) => {
                              const v = e.target.value;
                              setCapitulosNovosDraft((prev) => {
                                const next = [...prev];
                                next[i] = { ...next[i]!, nome: v };
                                return next;
                              });
                            }}
                          />
                          <Input
                            label={i === 0 ? "Aulas" : undefined}
                            className="w-24 shrink-0"
                            type="number"
                            min={1}
                            max={maxAulasPrevistasPorCapitulo}
                            inputMode="numeric"
                            value={row.aulas}
                            onChange={(e) => {
                              const v = e.target.value;
                              setCapitulosNovosDraft((prev) => {
                                const next = [...prev];
                                next[i] = { ...next[i]!, aulas: v };
                                return next;
                              });
                            }}
                          />
                          <Button
                            type="button"
                            variant="secondary"
                            size="sm"
                            className="shrink-0"
                            onClick={() => setCapitulosNovosDraft((prev) => prev.filter((_, j) => j !== i))}
                          >
                            Remover
                          </Button>
                        </div>
                      ))}
                    </div>
                  ) : null}
                </div>
              </>
            )}
          </div>
        ) : (
          <div className="mt-4 space-y-4">
            <Input
              label="Quantidade de capítulos"
              type="number"
              min={1}
              max={maxCapitulos}
              inputMode="numeric"
              value={capitulosCampo}
              onChange={(e) => setCapitulosCampo(e.target.value)}
              onBlur={onCapitulosBlur}
            />
            <p className="text-xs text-zinc-500">
              Ao sair do campo da quantidade, a lista abaixo é alinhada ao número de capítulos. Na hora de cadastrar,
              valores extras são ignorados e faltantes entram como {aulasPadraoNovaLinha}. Cada capítulo: 1 a{" "}
              {maxAulasPrevistasPorCapitulo} aulas previstas.
            </p>
            <div className="max-h-64 space-y-2 overflow-y-auto rounded-lg border border-zinc-200 bg-zinc-50/50 p-3">
              {linhasForm.map((valor, idx) => (
                <div key={idx} className="flex flex-wrap items-center gap-2 sm:flex-nowrap">
                  <span className="w-28 shrink-0 text-xs font-medium text-zinc-600">Capítulo {idx + 1}</span>
                  <Input
                    id={`livro-aulas-cap-${idx}`}
                    className="min-w-[6rem] flex-1"
                    type="number"
                    min={1}
                    max={maxAulasPrevistasPorCapitulo}
                    inputMode="numeric"
                    value={valor}
                    onChange={(e) => {
                      const v = e.target.value;
                      setAulasPorCapituloLinhas((prev) => {
                        const next = [...prev];
                        while (next.length <= idx) next.push(aulasPadraoNovaLinha);
                        next[idx] = v;
                        return next;
                      });
                    }}
                    aria-label={`Aulas previstas capítulo ${idx + 1}`}
                  />
                </div>
              ))}
            </div>
          </div>
        )}
      </Modal>
    </div>
  );
}
