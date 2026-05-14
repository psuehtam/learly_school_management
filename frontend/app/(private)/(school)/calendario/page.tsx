"use client";

import { useCallback, useEffect, useState } from "react";
import {
  criarEvento,
  deletarEvento,
  editarEvento,
  listarEventos,
  type EventoCalendario,
} from "@/lib/api/calendario";
import { getApiErrorMessage } from "@/lib/api/client";

// ─── Tipos ────────────────────────────────────────────────────────────────────
type TipoEvento = "aula" | "sem_aula" | "feriado" | "recesso";

interface DiaEvento {
  data: string;
  tipo: TipoEvento;
  /** Texto exibido e gravado na API como `descricao` (ex.: "Natal", "Recesso de julho"). */
  titulo: string;
}

// ─── Config de eventos (contraste forte no grid e no painel) ──────────────────
const eventos: Record<
  TipoEvento,
  { label: string; cor: string; descricao: string; corPonto: string; pill: string; celulaDia: string }
> = {
  aula: {
    label: "Aula",
    cor: "border-zinc-300 bg-white text-zinc-900",
    descricao: "Dia normal de aula",
    corPonto: "bg-zinc-400",
    pill: "text-zinc-900 bg-zinc-100 border-zinc-400",
    celulaDia: "bg-zinc-100 text-zinc-900 border-2 border-zinc-300 font-semibold shadow-sm",
  },
  sem_aula: {
    label: "Sem Aula",
    cor: "border-amber-600 bg-amber-50 text-amber-950",
    descricao: "Cancela aulas nesse dia",
    corPonto: "bg-amber-600",
    pill: "text-amber-950 bg-amber-200 border-amber-600",
    celulaDia: "bg-amber-200 text-amber-950 border-2 border-amber-600 font-semibold shadow-sm",
  },
  feriado: {
    label: "Feriado",
    cor: "border-red-600 bg-red-50 text-red-950",
    descricao: "Cancela aulas e justifica ausência",
    corPonto: "bg-red-600",
    pill: "text-red-950 bg-red-200 border-red-600",
    celulaDia: "bg-red-200 text-red-950 border-2 border-red-600 font-semibold shadow-sm",
  },
  recesso: {
    label: "Recesso",
    cor: "border-blue-700 bg-sky-50 text-sky-950",
    descricao: "Férias escolares (sem aula)",
    corPonto: "bg-blue-700",
    pill: "text-sky-950 bg-sky-200 border-blue-700",
    celulaDia: "bg-sky-200 text-sky-950 border-2 border-blue-700 font-semibold shadow-sm",
  },
};

const MESES = [
  "Janeiro",
  "Fevereiro",
  "Março",
  "Abril",
  "Maio",
  "Junho",
  "Julho",
  "Agosto",
  "Setembro",
  "Outubro",
  "Novembro",
  "Dezembro",
];
const DIAS_SEMANA = ["Dom", "Seg", "Ter", "Qua", "Qui", "Sex", "Sáb"];

function formatarData(ano: number, mes: number, dia: number): string {
  return `${ano}-${String(mes + 1).padStart(2, "0")}-${String(dia).padStart(2, "0")}`;
}

/** yyyy-mm-dd → dd/mm */
function dataIsoParaDdMm(dataIso: string): string {
  const parts = dataIso.split("-");
  if (parts.length !== 3) return dataIso;
  const [, m, d] = parts;
  return `${d}/${m}`;
}

function dataHojeIsoLocal(): string {
  const d = new Date();
  return formatarData(d.getFullYear(), d.getMonth(), d.getDate());
}

/** Sem Aula, Feriado e Recesso: só a partir de amanhã (data ISO > hoje). */
function dataPermitidaEventoSuspensivo(dataIso: string): boolean {
  return dataIso > dataHojeIsoLocal();
}

function tipoSuspendeAulaLocal(t: TipoEvento): boolean {
  return t !== "aula";
}

function labelTipoCalendario(t: TipoEvento): string {
  return eventos[t].label;
}

function diasNoMes(ano: number, mes: number): number {
  return new Date(ano, mes + 1, 0).getDate();
}

function primeiroDiaSemana(ano: number, mes: number): number {
  return new Date(ano, mes, 1).getDay();
}

function descricaoPadrao(tipo: TipoEvento): string {
  if (tipo === "sem_aula") return "Dia sem aula";
  if (tipo === "feriado") return "Feriado";
  if (tipo === "recesso") return "Recesso";
  return "Dia letivo";
}

/** Só na hora de marcar ou trocar tipo. Cancelar = não altera. */
function perguntarTituloNaMarcacao(tipo: TipoEvento, valorInicial: string): string | null {
  const r = window.prompt(
    `${labelTipoCalendario(tipo)} — título (opcional, aparece ao passar o mouse no ponto):\n\nDeixe em branco para usar o texto padrão.`,
    valorInicial,
  );
  if (r === null) return null;
  const t = r.trim();
  return t.length > 0 ? t : descricaoPadrao(tipo);
}

export default function CalendarioPage() {
  const hoje = new Date();
  const [ano, setAno] = useState(hoje.getFullYear());
  const [mes, setMes] = useState(hoje.getMonth());
  const [tipoSelecionado, setTipoSelecionado] = useState<TipoEvento>("sem_aula");
  const [diasMarcados, setDiasMarcados] = useState<DiaEvento[]>([]);
  const [eventosCarregados, setEventosCarregados] = useState<EventoCalendario[]>([]);
  const [alteracoesPendentes, setAlteracoesPendentes] = useState(false);
  const [carregando, setCarregando] = useState(false);
  const [salvando, setSalvando] = useState(false);

  const totalDias = diasNoMes(ano, mes);
  const offset = primeiroDiaSemana(ano, mes);

  function getDiaMarcado(data: string): DiaEvento | undefined {
    return diasMarcados.find((d) => d.data === data);
  }

  function tipoParaApi(tipo: TipoEvento): EventoCalendario["tipoEvento"] {
    if (tipo === "sem_aula") return "SEM AULA";
    if (tipo === "feriado") return "FERIADO";
    if (tipo === "recesso") return "RECESSO";
    return "AULA";
  }

  function tipoDaApi(tipo: EventoCalendario["tipoEvento"]): TipoEvento {
    if (tipo === "SEM AULA") return "sem_aula";
    if (tipo === "FERIADO") return "feriado";
    if (tipo === "RECESSO") return "recesso";
    return "aula";
  }

  const carregarEventosMes = useCallback(async () => {
    setCarregando(true);
    try {
      const lista = await listarEventos(mes + 1, ano);
      setEventosCarregados(lista);
      setDiasMarcados(
        lista
          .filter((evento) => evento.tipoEvento !== "AULA")
          .map((evento) => {
            const tipo = tipoDaApi(evento.tipoEvento);
            const tituloApi = evento.descricao?.trim();
            return {
              data: evento.dataEvento,
              tipo,
              titulo: tituloApi && tituloApi.length > 0 ? tituloApi : descricaoPadrao(tipo),
            };
          }),
      );
      setAlteracoesPendentes(false);
    } finally {
      setCarregando(false);
    }
  }, [ano, mes]);

  useEffect(() => {
    void carregarEventosMes();
  }, [carregarEventosMes]);

  function clicarDia(dia: number) {
    const data = formatarData(ano, mes, dia);
    const existente = getDiaMarcado(data);

    if (tipoSuspendeAulaLocal(tipoSelecionado) && !dataPermitidaEventoSuspensivo(data)) {
      alert(
        `${labelTipoCalendario(tipoSelecionado)} só pode ser marcado para datas a partir de amanhã (não é permitido hoje nem datas passadas).`,
      );
      return;
    }

    if (existente) {
      if (existente.tipo === tipoSelecionado) {
        setDiasMarcados((prev) => prev.filter((d) => d.data !== data));
        setAlteracoesPendentes(true);
        return;
      }
      const sugestao =
        existente.titulo.trim() && existente.titulo !== descricaoPadrao(existente.tipo)
          ? existente.titulo
          : descricaoPadrao(tipoSelecionado);
      const tituloMudanca = perguntarTituloNaMarcacao(tipoSelecionado, sugestao);
      if (tituloMudanca === null) return;
      setDiasMarcados((prev) =>
        prev.map((d) => (d.data === data ? { ...d, tipo: tipoSelecionado, titulo: tituloMudanca } : d)),
      );
    } else {
      if (tipoSelecionado === "aula") return;
      const titulo = perguntarTituloNaMarcacao(tipoSelecionado, descricaoPadrao(tipoSelecionado));
      if (titulo === null) return;
      setDiasMarcados((prev) => [...prev, { data, tipo: tipoSelecionado, titulo }]);
    }
    setAlteracoesPendentes(true);
  }

  async function aplicarAlteracoes() {
    setSalvando(true);
    try {
      const invalidasDatas = diasMarcados.filter(
        (d) => tipoSuspendeAulaLocal(d.tipo) && !dataPermitidaEventoSuspensivo(d.data),
      );
      if (invalidasDatas.length > 0) {
        alert(
          "Sem Aula, Feriado e Recesso só podem ser salvos para datas a partir de amanhã. Ajuste ou remova as marcações inválidas antes de aplicar.",
        );
        return;
      }

      const marcadosPorData = new Map(diasMarcados.map((d) => [d.data, d]));
      const carregadosPorData = new Map(eventosCarregados.map((e) => [e.dataEvento, e]));

      for (const [data, marcado] of marcadosPorData) {
        const tipo = marcado.tipo;
        const tituloGravar = marcado.titulo.trim() || descricaoPadrao(tipo);
        const eventoExistente = carregadosPorData.get(data);

        if (!eventoExistente) {
          if (tipo !== "aula") {
            await criarEvento({
              dataEvento: data,
              tipoEvento: tipoParaApi(tipo),
              descricao: tituloGravar,
            });
          }
          continue;
        }

        if (tipo === "aula") {
          await deletarEvento(eventoExistente.id);
          continue;
        }

        const novoTipo = tipoParaApi(tipo);
        const descAnterior =
          eventoExistente.descricao?.trim() ||
          descricaoPadrao(tipoDaApi(eventoExistente.tipoEvento));
        if (eventoExistente.tipoEvento !== novoTipo || descAnterior !== tituloGravar) {
          await editarEvento(eventoExistente.id, {
            tipoEvento: novoTipo,
            descricao: tituloGravar,
          });
        }
      }

      for (const evento of eventosCarregados) {
        if (!marcadosPorData.has(evento.dataEvento)) {
          await deletarEvento(evento.id);
        }
      }

      await carregarEventosMes();
      alert("Alterações salvas com sucesso.");
    } catch (e) {
      alert(getApiErrorMessage(e, "Não foi possível salvar as alterações do calendário."));
    } finally {
      setSalvando(false);
    }
  }

  function cancelarAlteracoesPendentes() {
    setDiasMarcados(
      eventosCarregados
        .filter((evento) => evento.tipoEvento !== "AULA")
        .map((evento) => {
          const tipo = tipoDaApi(evento.tipoEvento);
          const tituloApi = evento.descricao?.trim();
          return {
            data: evento.dataEvento,
            tipo,
            titulo: tituloApi && tituloApi.length > 0 ? tituloApi : descricaoPadrao(tipo),
          };
        }),
    );
    setAlteracoesPendentes(false);
  }

  function mesAnterior() {
    if (mes === 0) {
      setMes(11);
      setAno(ano - 1);
    } else setMes(mes - 1);
  }

  function proximoMes() {
    if (mes === 11) {
      setMes(0);
      setAno(ano + 1);
    } else setMes(mes + 1);
  }

  const diasDoMes = Array.from({ length: totalDias }, (_, i) => i + 1);
  const celulas = [...Array(offset).fill(null), ...diasDoMes];

  const prefixoMes = `${ano}-${String(mes + 1).padStart(2, "0")}`;
  const eventosDoMesOrdenados = [...diasMarcados]
    .filter((d) => d.data.startsWith(prefixoMes))
    .sort((a, b) => a.data.localeCompare(b.data));

  return (
    <div className="flex flex-col gap-6">
      <div className="flex items-center justify-between gap-4">
        <div>
          <h1 className="text-xl font-semibold text-zinc-900">Calendário geral</h1>
          <p className="text-sm text-zinc-600 mt-0.5">
            Marque dias sem aula, feriados e recessos. As mudanças ficam pendentes até aplicar ou cancelar.
          </p>
        </div>
        {alteracoesPendentes && (
          <div className="flex flex-wrap items-center justify-end gap-2 shrink-0">
            <button
              type="button"
              onClick={cancelarAlteracoesPendentes}
              disabled={salvando}
              className="h-10 px-4 text-sm font-semibold text-zinc-800 bg-white border border-zinc-300 rounded-lg hover:bg-zinc-50 transition-colors shadow-sm disabled:opacity-60"
            >
              Cancelar
            </button>
            <button
              type="button"
              onClick={() => void aplicarAlteracoes()}
              disabled={salvando}
              className="flex items-center gap-2 h-10 px-5 text-sm font-semibold text-white bg-[#1F2A35] rounded-lg hover:bg-[#2d3d4d] transition-colors shadow-sm disabled:opacity-60"
            >
              <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
                <polyline points="20 6 9 17 4 12" />
              </svg>
              {salvando ? "Salvando…" : "Aplicar alterações"}
            </button>
          </div>
        )}
      </div>

      <div className="flex flex-col xl:flex-row gap-6 items-start">
        <div className="flex-1 w-full bg-white border border-zinc-300 rounded-xl overflow-hidden shadow-sm">
          <div className="flex items-center justify-between px-5 py-4 border-b border-zinc-200 bg-zinc-50/90">
            <button
              type="button"
              onClick={mesAnterior}
              className="w-10 h-10 flex items-center justify-center rounded-lg border border-zinc-300 bg-white text-zinc-800 hover:bg-zinc-100 transition-colors"
              aria-label="Mês anterior"
            >
              <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
                <polyline points="15 18 9 12 15 6" />
              </svg>
            </button>
            <span className="text-base font-bold text-zinc-900 tracking-tight">
              {MESES[mes]} {ano}
            </span>
            <button
              type="button"
              onClick={proximoMes}
              className="w-10 h-10 flex items-center justify-center rounded-lg border border-zinc-300 bg-white text-zinc-800 hover:bg-zinc-100 transition-colors"
              aria-label="Próximo mês"
            >
              <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
                <polyline points="9 18 15 12 9 6" />
              </svg>
            </button>
          </div>

          <div className="px-4 py-5 bg-zinc-50/40">
            {carregando && (
              <p className="text-sm font-medium text-zinc-700 mb-3">Carregando eventos…</p>
            )}
            <div className="grid grid-cols-7 gap-1 mb-2">
              {DIAS_SEMANA.map((d) => (
                <div
                  key={d}
                  className="text-center text-xs font-bold text-zinc-700 uppercase tracking-wide py-2"
                >
                  {d}
                </div>
              ))}
            </div>

            <div className="grid grid-cols-7 gap-1.5">
              {celulas.map((dia, i) => {
                if (!dia) return <div key={`empty-${i}`} className="min-h-[3.25rem]" />;
                const data = formatarData(ano, mes, dia);
                const marcado = getDiaMarcado(data);
                const isHoje =
                  data === formatarData(hoje.getFullYear(), hoje.getMonth(), hoje.getDate());
                const bloqueioDataSuspensivo =
                  tipoSuspendeAulaLocal(tipoSelecionado) && !dataPermitidaEventoSuspensivo(data);

                return (
                  <button
                    key={data}
                    type="button"
                    title={marcado ? marcado.titulo : undefined}
                    onClick={() => clicarDia(dia)}
                    className={`
                      relative min-h-[3.25rem] flex flex-col items-center justify-center rounded-xl text-sm font-semibold transition-all px-0.5 py-1
                      ${
                        marcado
                          ? eventos[marcado.tipo].celulaDia
                          : "bg-white text-zinc-900 border-2 border-zinc-200 hover:bg-zinc-100 hover:border-zinc-400"
                      }
                      ${isHoje && !marcado ? "ring-2 ring-[#1F2A35] ring-offset-2" : ""}
                      ${isHoje && marcado ? "ring-2 ring-[#1F2A35] ring-offset-1" : ""}
                      ${
                        bloqueioDataSuspensivo
                          ? "opacity-55 saturate-50 cursor-not-allowed border-dashed border-zinc-400"
                          : ""
                      }
                    `}
                  >
                    {isHoje ? (
                      <span
                        className="absolute top-1 right-1 w-1.5 h-1.5 rounded-full bg-[#1F2A35]"
                        title="Hoje"
                      />
                    ) : null}
                    <span className="shrink-0 leading-none tabular-nums">{dia}</span>
                  </button>
                );
              })}
            </div>
          </div>

          <div className="px-5 py-3.5 border-t border-zinc-200 bg-white">
            {eventosDoMesOrdenados.length === 0 ? (
              <p className="text-sm text-zinc-600">Nenhum evento especial neste mês</p>
            ) : (
              <div className="flex flex-wrap gap-2">
                {eventosDoMesOrdenados.map((d) => {
                  const pill = eventos[d.tipo].pill;
                  const rotuloCompleto = `${dataIsoParaDdMm(d.data)} — ${d.titulo}`;
                  return (
                    <span
                      key={d.data}
                      className={`inline-flex max-w-full min-w-0 items-center gap-2 rounded-full border px-3 py-1.5 text-xs font-semibold shadow-sm ${pill}`}
                      title={rotuloCompleto}
                    >
                      <span className="shrink-0 tabular-nums">{dataIsoParaDdMm(d.data)}</span>
                      <span className="min-w-0 truncate font-semibold">{d.titulo}</span>
                    </span>
                  );
                })}
              </div>
            )}
          </div>
        </div>

        <div className="w-full xl:w-80 shrink-0 min-w-0 flex flex-col gap-4">
          <div className="bg-white border border-zinc-300 rounded-xl p-4 shadow-sm">
            <p className="text-xs font-bold text-zinc-800 uppercase tracking-wide mb-3">Tipo de evento</p>
            <div className="flex flex-col gap-2">
              {(Object.keys(eventos) as TipoEvento[]).map((tipo) => {
                const ev = eventos[tipo];
                const ativo = tipoSelecionado === tipo;
                const ringCor =
                  tipo === "sem_aula"
                    ? "ring-amber-600"
                    : tipo === "feriado"
                      ? "ring-red-600"
                      : tipo === "recesso"
                        ? "ring-blue-700"
                        : "ring-zinc-500";
                return (
                  <button
                    key={tipo}
                    type="button"
                    onClick={() => setTipoSelecionado(tipo)}
                    className={`flex items-start gap-3 p-3.5 rounded-xl border-2 text-left transition-all ${
                      ativo
                        ? `${ev.cor} ring-2 ring-offset-2 ${ringCor} shadow-sm`
                        : "border-zinc-200 bg-zinc-50/50 hover:border-zinc-400 hover:bg-white text-zinc-900"
                    }`}
                  >
                    <div
                      className={`w-3.5 h-3.5 rounded-full mt-0.5 shrink-0 border border-black/10 ${
                        tipo === "aula"
                          ? "bg-zinc-400"
                          : tipo === "sem_aula"
                            ? "bg-amber-500"
                            : tipo === "feriado"
                              ? "bg-red-600"
                              : "bg-blue-700"
                      }`}
                    />
                    <div>
                      <p className="text-sm font-bold text-zinc-900">{ev.label}</p>
                      <p className="text-xs text-zinc-600 mt-1 leading-snug">{ev.descricao}</p>
                    </div>
                  </button>
                );
              })}
            </div>
          </div>

          <div className="bg-amber-50/80 border-2 border-amber-300 rounded-xl p-4 shadow-sm">
            <p className="text-xs font-bold text-amber-950 uppercase tracking-wide mb-2">Datas para Sem Aula, Feriado e Recesso</p>
            <p className="text-sm text-amber-950 leading-relaxed">
              Só é permitido marcar a partir de <span className="font-bold">amanhã</span> (não use o dia de hoje nem
              datas passadas). Se a API recusar, pode ser porque já existem{" "}
              <span className="font-bold">compromissos ativos</span> naquele dia — a mensagem de erro explica o motivo.
            </p>
          </div>

          <div className="bg-white border border-zinc-300 rounded-xl p-4 shadow-sm min-w-0">
            <p className="text-xs font-bold text-zinc-800 uppercase tracking-wide mb-2">Como usar</p>
            <ol className="list-decimal list-outside pl-5 space-y-2.5 text-sm text-zinc-800 leading-relaxed">
              <li className="pl-1 marker:font-bold">Escolha o tipo ao lado.</li>
              <li className="pl-1 marker:font-bold">
                Clique no dia: pede o título (cancelar não marca). O quadradinho fica na cor do tipo; embaixo do mês
                aparece uma etiqueta com a data e o título.
              </li>
              <li className="pl-1 marker:font-bold">Clique de novo no mesmo tipo no dia para remover.</li>
              <li className="pl-1 marker:font-bold">
                Use <span className="font-semibold">Aplicar alterações</span> para gravar no servidor, ou{" "}
                <span className="font-semibold">Cancelar</span> para descartar o que ainda não foi salvo.
              </li>
            </ol>
          </div>
        </div>
      </div>
    </div>
  );
}
