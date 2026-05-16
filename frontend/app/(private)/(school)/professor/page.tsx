"use client";

import { useCallback, useEffect, useMemo, useState } from "react";
import Link from "next/link";
import { listarAulas } from "@/lib/api/aulas";
import { listarCompromissos, type Compromisso } from "@/lib/api/compromissos";
import type { EventoCalendario } from "@/lib/api/calendario";
import {
  listarHorariosFuncionamentoConsultaCompromissos,
  type HorarioFuncionamentoDto,
} from "@/lib/api/configuracoes";
import { getApiErrorMessage } from "@/lib/api";
import {
  AGENDA_ALTURA_HORA_PX,
  AGENDA_ALTURA_MINIMA_CARD_OCUPACAO_PX,
  AGENDA_GRID_ALTURA_PX,
  AGENDA_HORAS_DIA,
  AGENDA_HORA_INICIO_GRID,
  alturaCardPxAgenda,
  atribuirFaixasSobrepostasAgenda,
  calcularEstadoDiaAgendaColuna,
  dataParaIsoLocal,
  diasDaSemana,
  getInicioSemanaSegunda,
  horarioParaTopPxAgenda,
  itensMinhaAgendaNoDia,
  listarEventosParaDatas,
  parseAulaIdDeOcupacao,
  type OcupacaoAgendaItem,
  type EstadoDiaAgendaColuna,
} from "@/lib/agenda";
import type { Aula } from "@/types/aula";
import { useAuth } from "@/hooks/use-auth";
import { AgendaOcupacaoCard } from "@/components/escola/agenda-ocupacao-card";
import { AgendaOcupacaoDetalheModal } from "@/components/escola/agenda-ocupacao-detalhe-modal";
import { AgendaDiaAvisoOverlay } from "@/components/escola/agenda-dia-aviso-overlay";
import { AgendaForaExpedienteFaixas } from "@/components/escola/agenda-fora-expediente-faixas";

const DIAS_CURTO = ["Seg", "Ter", "Qua", "Qui", "Sex", "Sáb", "Dom"];

function formatarDataCurta(data: Date): string {
  return data.toLocaleDateString("pt-BR", { day: "2-digit", month: "2-digit" });
}

function verificaDestaque(item: OcupacaoAgendaItem, termo: string): boolean {
  if (!termo) return true;
  const hay = [item.titulo, item.subtitulo, item.contextoAulaExtra, item.categoriaCompromisso]
    .filter(Boolean)
    .join(" ")
    .toLowerCase();
  return hay.includes(termo);
}

export default function MinhaAgendaPage() {
  const hoje = useMemo(() => new Date(), []);
  const { user, isLoading: authLoading } = useAuth();

  const [semanaBase, setSemanaBase] = useState(() => getInicioSemanaSegunda(hoje));
  const [aulas, setAulas] = useState<Aula[]>([]);
  const [compromissos, setCompromissos] = useState<Compromisso[]>([]);
  const [eventosCalendario, setEventosCalendario] = useState<EventoCalendario[]>([]);
  const [horariosFuncionamento, setHorariosFuncionamento] = useState<HorarioFuncionamentoDto[]>([]);
  const [carregando, setCarregando] = useState(true);
  const [erro, setErro] = useState<string | null>(null);
  const [busca, setBusca] = useState("");
  const [itemSelecionado, setItemSelecionado] = useState<OcupacaoAgendaItem | null>(null);

  const usuarioId = user?.id ?? null;
  const diasSemana = useMemo(() => diasDaSemana(semanaBase), [semanaBase]);

  const carregarAgenda = useCallback(async () => {
    if (!usuarioId) {
      setAulas([]);
      setCompromissos([]);
      setEventosCalendario([]);
      setCarregando(false);
      return;
    }

    setCarregando(true);
    setErro(null);
    try {
      const dias = diasDaSemana(semanaBase);
      const [aulasApi, compsApi, eventos, horarios] = await Promise.all([
        listarAulas(),
        listarCompromissos(),
        listarEventosParaDatas(dias),
        listarHorariosFuncionamentoConsultaCompromissos().catch(
          () => [] as HorarioFuncionamentoDto[],
        ),
      ]);

      setAulas(aulasApi.filter((a) => a.professorId === usuarioId));
      setCompromissos(compsApi);
      setEventosCalendario(eventos);
      setHorariosFuncionamento(horarios);
    } catch (e) {
      setErro(getApiErrorMessage(e, "Não foi possível carregar sua agenda."));
      setAulas([]);
      setCompromissos([]);
      setEventosCalendario([]);
    } finally {
      setCarregando(false);
    }
  }, [usuarioId, semanaBase]);

  useEffect(() => {
    if (authLoading) return;
    void carregarAgenda();
  }, [authLoading, carregarAgenda]);

  const estadosPorDia = useMemo((): EstadoDiaAgendaColuna[] => {
    return diasSemana.map((data) =>
      calcularEstadoDiaAgendaColuna(
        dataParaIsoLocal(data),
        eventosCalendario,
        horariosFuncionamento,
      ),
    );
  }, [diasSemana, eventosCalendario, horariosFuncionamento]);

  const turmaPorAulaId = useMemo(() => {
    const map = new Map<number, number>();
    for (const a of aulas) {
      map.set(a.id, a.turmaId);
    }
    return map;
  }, [aulas]);

  const termoBusca = busca.trim().toLowerCase();

  const itensPorDia = useMemo(() => {
    if (!usuarioId) return diasSemana.map(() => [] as OcupacaoAgendaItem[]);
    return diasSemana.map((data) => {
      const iso = dataParaIsoLocal(data);
      let itens = itensMinhaAgendaNoDia(iso, aulas, compromissos, usuarioId);
      if (termoBusca) {
        itens = itens.filter((i) => verificaDestaque(i, termoBusca));
      }
      return itens;
    });
  }, [diasSemana, aulas, compromissos, usuarioId, termoBusca]);

  const mesAno = semanaBase.toLocaleDateString("pt-BR", { month: "long", year: "numeric" });

  const aulaIdModal = itemSelecionado ? parseAulaIdDeOcupacao(itemSelecionado) : null;
  const turmaIdModal = aulaIdModal != null ? turmaPorAulaId.get(aulaIdModal) : undefined;

  function semanaAnterior() {
    const d = new Date(semanaBase);
    d.setDate(d.getDate() - 7);
    setSemanaBase(d);
  }

  function proximaSemana() {
    const d = new Date(semanaBase);
    d.setDate(d.getDate() + 7);
    setSemanaBase(d);
  }

  function irParaHoje() {
    setSemanaBase(getInicioSemanaSegunda(hoje));
  }

  function isHoje(data: Date) {
    return dataParaIsoLocal(data) === dataParaIsoLocal(hoje);
  }

  const temAlgumEventoNaSemana = itensPorDia.some((d) => d.length > 0);

  return (
    <>
      <div className="flex flex-col gap-4">
        <div className="flex flex-wrap items-center justify-between gap-3">
          <div>
            <h1 className="text-xl font-semibold text-zinc-900">Minha Agenda</h1>
            <p className="mt-0.5 text-sm text-zinc-500 capitalize">
              Sua visão semanal · aulas, reuniões e calendário escolar · {mesAno}
            </p>
          </div>

          <div className="flex flex-wrap items-center gap-3">
            <div className="relative">
              <input
                type="text"
                placeholder="Buscar aula, turma ou reunião..."
                value={busca}
                onChange={(e) => setBusca(e.target.value)}
                className="h-9 w-56 rounded-lg border border-zinc-300 pl-3 pr-8 text-sm shadow-sm outline-none transition focus:border-[#1F2A35] focus:ring-2 focus:ring-[#1F2A35]/10"
              />
              <svg
                className="absolute right-2.5 top-1/2 -translate-y-1/2 text-zinc-400"
                width="13"
                height="13"
                viewBox="0 0 24 24"
                fill="none"
                stroke="currentColor"
                strokeWidth="2"
                aria-hidden
              >
                <circle cx="11" cy="11" r="8" />
                <line x1="21" y1="21" x2="16.65" y2="16.65" />
              </svg>
            </div>

            <div className="flex items-center gap-1">
              <button
                type="button"
                onClick={semanaAnterior}
                className="flex h-8 w-8 items-center justify-center rounded-lg border border-zinc-300 text-zinc-600 transition-colors hover:bg-zinc-50"
                aria-label="Semana anterior"
              >
                <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
                  <polyline points="15 18 9 12 15 6" />
                </svg>
              </button>
              <button
                type="button"
                onClick={irParaHoje}
                className="h-8 rounded-lg border border-zinc-300 px-3 text-xs font-medium text-zinc-600 transition-colors hover:bg-zinc-50"
              >
                Hoje
              </button>
              <button
                type="button"
                onClick={proximaSemana}
                className="flex h-8 w-8 items-center justify-center rounded-lg border border-zinc-300 text-zinc-600 transition-colors hover:bg-zinc-50"
                aria-label="Próxima semana"
              >
                <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
                  <polyline points="9 18 15 12 9 6" />
                </svg>
              </button>
            </div>
          </div>
        </div>

        {erro && (
          <p className="rounded-lg border border-red-200 bg-red-50 px-3 py-2 text-sm text-red-700">{erro}</p>
        )}

        <div className="flex flex-wrap gap-x-4 gap-y-2 text-xs text-zinc-500">
          <span className="flex items-center gap-1.5">
            <span className="h-3 w-3 rounded-sm bg-blue-100 ring-1 ring-blue-200" />
            Aula
          </span>
          <span className="flex items-center gap-1.5">
            <span className="h-3 w-3 rounded-sm bg-amber-100 ring-1 ring-amber-200" />
            Reposição
          </span>
          <span className="flex items-center gap-1.5">
            <span className="h-3 w-3 rounded-sm bg-violet-100 ring-1 ring-violet-200" />
            Compromisso / reunião
          </span>
          <span className="flex items-center gap-1.5">
            <span
              className="h-3 w-3 rounded-sm border border-zinc-300 bg-zinc-100"
              style={{
                backgroundImage:
                  "repeating-linear-gradient(-45deg, transparent, transparent 2px, rgba(113,113,122,0.2) 2px, rgba(113,113,122,0.2) 4px)",
              }}
            />
            Fora do expediente
          </span>
        </div>

        <div className="overflow-hidden rounded-xl border border-zinc-200 bg-white shadow-sm">
          <div
            className="grid border-b border-zinc-200"
            style={{ gridTemplateColumns: "56px repeat(7, 1fr)" }}
          >
            <div className="border-r border-zinc-100 bg-zinc-50" />
            {diasSemana.map((data, i) => {
              const estado = estadosPorDia[i];
              const bloqueado =
                estado?.eventoCalendario != null || estado?.escolaFechadaDiaInteiro;
              return (
                <div
                  key={dataParaIsoLocal(data)}
                  className={`border-r border-zinc-100 px-2 py-3 text-center last:border-r-0 ${
                    isHoje(data) ? "bg-[#1F2A35]/5" : ""
                  } ${bloqueado ? "bg-zinc-50/80" : ""}`}
                >
                  <p className="text-xs font-medium text-zinc-400">{DIAS_CURTO[i]}</p>
                  <p
                    className={`mt-0.5 text-sm font-semibold ${
                      isHoje(data) ? "text-[#1F2A35]" : "text-zinc-700"
                    }`}
                  >
                    {formatarDataCurta(data)}
                  </p>
                  {bloqueado && (
                    <p className="mt-1 text-[9px] font-semibold uppercase tracking-wide text-zinc-500">
                      {estado.eventoCalendario
                        ? estado.eventoCalendario.tipoEvento
                        : "Fechado"}
                    </p>
                  )}
                  {isHoje(data) && !bloqueado && (
                    <div className="mx-auto mt-1 h-1.5 w-1.5 rounded-full bg-[#1F2A35]" />
                  )}
                </div>
              );
            })}
          </div>

          {carregando || authLoading ? (
            <div className="px-6 py-16 text-center text-sm text-zinc-500">Carregando sua agenda...</div>
          ) : (
            <div className="overflow-x-auto overflow-y-auto" style={{ maxHeight: "620px" }}>
              <div
                className="relative grid min-w-[840px]"
                style={{ gridTemplateColumns: "56px repeat(7, 1fr)" }}
              >
                <div className="sticky left-0 z-20 border-r border-zinc-100 bg-zinc-50">
                  {AGENDA_HORAS_DIA.map((hora) => (
                    <div
                      key={hora}
                      className="relative border-b border-zinc-100"
                      style={{ height: `${AGENDA_ALTURA_HORA_PX}px` }}
                    >
                      <span className="absolute -top-2.5 w-full text-center text-xs font-bold text-zinc-400">
                        {String(hora).padStart(2, "0")}:00
                      </span>
                    </div>
                  ))}
                </div>

                {diasSemana.map((data, diaIdx) => {
                  const estado = estadosPorDia[diaIdx];
                  const itens = itensPorDia[diaIdx] ?? [];
                  const posicionados = itens.map((item) => ({
                    item,
                    topPx: horarioParaTopPxAgenda(item.inicio),
                    alturaExibicaoPx: alturaCardPxAgenda(
                      item.inicio,
                      item.fim,
                      AGENDA_ALTURA_MINIMA_CARD_OCUPACAO_PX,
                    ),
                  }));
                  const faixas = atribuirFaixasSobrepostasAgenda(posicionados);
                  const diaBloqueado =
                    estado.eventoCalendario != null || estado.escolaFechadaDiaInteiro;

                  return (
                    <div
                      key={dataParaIsoLocal(data)}
                      className={`relative border-r border-zinc-100 last:border-r-0 ${
                        isHoje(data) ? "bg-[#1F2A35]/[0.02]" : ""
                      }`}
                      style={{ height: `${AGENDA_GRID_ALTURA_PX}px` }}
                    >
                      {AGENDA_HORAS_DIA.map((hora) => (
                        <div
                          key={hora}
                          className="absolute w-full border-t border-zinc-100"
                          style={{
                            top: `${(hora - AGENDA_HORA_INICIO_GRID) * AGENDA_ALTURA_HORA_PX}px`,
                          }}
                        />
                      ))}

                      <AgendaDiaAvisoOverlay
                        eventoCalendario={estado.eventoCalendario}
                        escolaFechadaDiaInteiro={estado.escolaFechadaDiaInteiro}
                        compacto
                      />

                      {!diaBloqueado && (
                        <AgendaForaExpedienteFaixas
                          faixas={estado.faixasForaExpediente}
                          compacto
                        />
                      )}

                      {!diaBloqueado &&
                        posicionados.map((p) => {
                          const temDestaque = verificaDestaque(p.item, termoBusca);
                          const fh = faixas.get(p.item.id) ?? { indice: 0, total: 1 };
                          return (
                            <AgendaOcupacaoCard
                              key={p.item.id}
                              item={p.item}
                              topPx={p.topPx}
                              alturaExibicaoPx={p.alturaExibicaoPx}
                              faixaHorizontal={fh}
                              temDestaque={temDestaque}
                              corBarraLateral="bg-[#1F2A35]"
                              classNameDestaque={
                                temDestaque
                                  ? "opacity-100 hover:shadow-md hover:z-[40]"
                                  : "opacity-30 grayscale hover:opacity-50 z-[1]"
                              }
                              onClick={() => setItemSelecionado(p.item)}
                            />
                          );
                        })}

                      {isHoje(data) &&
                        !diaBloqueado &&
                        (() => {
                          const agora = new Date();
                          const minAgora = agora.getHours() * 60 + agora.getMinutes();
                          const topAgora =
                            ((minAgora - AGENDA_HORA_INICIO_GRID * 60) / 60) *
                            AGENDA_ALTURA_HORA_PX;
                          if (topAgora < 0 || topAgora > AGENDA_GRID_ALTURA_PX) return null;
                          return (
                            <div
                              className="pointer-events-none absolute z-20 flex w-full items-center"
                              style={{ top: `${topAgora}px` }}
                            >
                              <div className="-ml-1 h-2 w-2 shrink-0 rounded-full bg-red-500" />
                              <div className="h-px flex-1 bg-red-400" />
                            </div>
                          );
                        })()}
                    </div>
                  );
                })}
              </div>
            </div>
          )}
        </div>

        {!carregando && !authLoading && usuarioId && !temAlgumEventoNaSemana && (
          <p className="text-center text-sm text-zinc-500">
            Nenhuma aula ou compromisso nesta semana nos horários letivos. Dias com feriado ou escola
            fechada aparecem destacados na grade.
          </p>
        )}
      </div>

      <AgendaOcupacaoDetalheModal
        open={itemSelecionado != null}
        onClose={() => setItemSelecionado(null)}
        item={itemSelecionado}
        usuarioColunaNome={user?.nome}
      />

      {itemSelecionado && turmaIdModal != null && turmaIdModal > 0 && (
        <div className="fixed bottom-6 right-6 z-[60]">
          <Link
            href={`/professor/turma/${turmaIdModal}`}
            className="inline-flex h-10 items-center gap-2 rounded-lg bg-[#1F2A35] px-4 text-sm font-medium text-white shadow-lg hover:bg-[#2d3d4d]"
          >
            Abrir turma
            <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
              <polyline points="9 18 15 12 9 6" />
            </svg>
          </Link>
        </div>
      )}
    </>
  );
}
