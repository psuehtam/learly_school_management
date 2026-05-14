"use client";

import { useEffect, useMemo, useState } from "react";
import {
  listarEventos,
  listarAulas,
  listarCompromissosAgendaGlobal,
  listarHorariosFuncionamento,
  listarParticipantesCompromissos,
  type EventoCalendario,
  type HorarioFuncionamentoDto,
  type ParticipanteCompromisso,
  getApiErrorMessage,
} from "@/lib/api";
import {
  mapearAulasParaOcupacoesAgenda,
  mapearCompromissosParaOcupacoesAgenda,
  AGENDA_ALTURA_MINIMA_CARD_OCUPACAO_PX,
  atribuirFaixasSobrepostasAgenda,
  type OcupacaoAgendaItem,
} from "@/lib/agenda";
import { AgendaOcupacaoCard } from "@/components/escola/agenda-ocupacao-card";
import { AgendaOcupacaoDetalheModal } from "@/components/escola/agenda-ocupacao-detalhe-modal";

interface UsuarioAgenda {
  id: number;
  nome: string;
}

const HORAS_DIA = [8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21];

/** Altura útil da grade (8h–22h, um bloco de 80px por hora listada). */
const GRID_ALTURA_CONTEUDO_PX = HORAS_DIA.length * 80;

function calcularTop(horario: string) {
  const [h, m] = horario.split(":").map(Number);
  return ((h - 8) * 60 + m) * (80 / 60);
}

function calcularHeight(inicio: string, fim: string) {
  const [h1, m1] = inicio.split(":").map(Number);
  const [h2, m2] = fim.split(":").map(Number);
  return ((h2 * 60 + m2) - (h1 * 60 + m1)) * (80 / 60);
}

/**
 * Faixas (top/height em px) fora do expediente configurado, no mesmo sistema dos cards da agenda.
 * Ignora faixas muito pequenas (menos de 8px).
 */
function faixasForaDoHorarioEscola(abertura: string, fechamento: string): { topPx: number; heightPx: number }[] {
  const out: { topPx: number; heightPx: number }[] = [];
  const topAb = calcularTop(abertura);
  const topFe = calcularTop(fechamento);
  if (topAb > 2) {
    const h = Math.min(topAb, GRID_ALTURA_CONTEUDO_PX);
    if (h >= 8) out.push({ topPx: 0, heightPx: h });
  }
  if (topFe < GRID_ALTURA_CONTEUDO_PX - 2) {
    const h = GRID_ALTURA_CONTEUDO_PX - topFe;
    if (h >= 8) out.push({ topPx: topFe, heightPx: h });
  }
  return out;
}

function corEventoCalendario(tipoEvento: EventoCalendario["tipoEvento"]) {
  if (tipoEvento === "FERIADO") return "bg-red-50 text-red-700 border-red-200";
  if (tipoEvento === "RECESSO") return "bg-blue-50 text-blue-700 border-blue-200";
  return "bg-amber-50 text-amber-700 border-amber-200";
}

function corColuna(index: number) {
  const cores = ["bg-blue-600", "bg-purple-600", "bg-emerald-600", "bg-orange-600", "bg-pink-600", "bg-cyan-600"];
  return cores[index % cores.length];
}

// ─── Página Principal ─────────────────────────────────────────────────────────
export default function AgendaGlobalPage() {
  const [dataSelecionada, setDataSelecionada] = useState(new Date().toISOString().slice(0, 10));
  const [busca, setBusca] = useState("");
  const [usuarioSelecionadoId, setUsuarioSelecionadoId] = useState<number | "todos">("todos");
  const [eventosCalendario, setEventosCalendario] = useState<EventoCalendario[]>([]);
  /** Colunas do grid (filtrado). */
  const [usuariosAgenda, setUsuariosAgenda] = useState<UsuarioAgenda[]>([]);
  /** Opções do select: sempre quem pode aparecer no dia, independente do filtro atual. */
  const [usuariosOpcoesFiltro, setUsuariosOpcoesFiltro] = useState<UsuarioAgenda[]>([]);
  const [itensAgenda, setItensAgenda] = useState<OcupacaoAgendaItem[]>([]);
  const [horariosFuncionamento, setHorariosFuncionamento] = useState<HorarioFuncionamentoDto[]>([]);
  const [erro, setErro] = useState<string | null>(null);
  const [detalheAgenda, setDetalheAgenda] = useState<{
    item: OcupacaoAgendaItem;
    usuarioNome: string;
  } | null>(null);

  useEffect(() => {
    async function carregarAgenda() {
      setErro(null);
      try {
        const data = new Date(`${dataSelecionada}T00:00:00`);
        const filtroUsuario = usuarioSelecionadoId === "todos" ? undefined : usuarioSelecionadoId;
        const [eventos, aulas, compromissos, participantes, horarios] = await Promise.all([
          listarEventos(data.getMonth() + 1, data.getFullYear()),
          listarAulas(),
          listarCompromissosAgendaGlobal(dataSelecionada),
          listarParticipantesCompromissos(),
          listarHorariosFuncionamento().catch(() => [] as HorarioFuncionamentoDto[]),
        ]);

        setEventosCalendario(eventos);
        setHorariosFuncionamento(horarios);

        const mapaUsuarios = new Map<number, string>();
        participantes.forEach((p: ParticipanteCompromisso) => mapaUsuarios.set(p.id, p.nomeCompleto));

        const itensAulaTodos = mapearAulasParaOcupacoesAgenda(aulas, dataSelecionada, {});
        const itensCompTodos = mapearCompromissosParaOcupacoesAgenda(compromissos, {});

        const idsSelect = new Set<number>();
        itensAulaTodos.forEach((i) => idsSelect.add(i.usuarioId));
        itensCompTodos.forEach((i) => idsSelect.add(i.usuarioId));
        participantes.forEach((p: ParticipanteCompromisso) => idsSelect.add(p.id));

        const opcoesFiltro = Array.from(idsSelect)
          .map((id) => ({
            id,
            nome: mapaUsuarios.get(id) ?? `Usuario ${id}`,
          }))
          .sort((a, b) => a.nome.localeCompare(b.nome, "pt-BR"));
        setUsuariosOpcoesFiltro(opcoesFiltro);

        const itensAula = mapearAulasParaOcupacoesAgenda(aulas, dataSelecionada, {
          professorId: filtroUsuario,
        });

        const itensCompromisso = mapearCompromissosParaOcupacoesAgenda(compromissos, {
          usuarioId: filtroUsuario,
        });

        const todosItens = [...itensAula, ...itensCompromisso];
        setItensAgenda(todosItens);

        let idsColunas = Array.from(new Set(todosItens.map((i) => i.usuarioId)));
        if (filtroUsuario != null && !idsColunas.includes(filtroUsuario)) {
          idsColunas = [...idsColunas, filtroUsuario];
        }
        const usuariosColunas = idsColunas.map((id) => ({
          id,
          nome: mapaUsuarios.get(id) ?? `Usuario ${id}`,
        }));
        setUsuariosAgenda(usuariosColunas);
      } catch (e) {
        setErro(getApiErrorMessage(e, "Nao foi possivel carregar agenda global."));
      }
    }
    void carregarAgenda();
  }, [dataSelecionada, usuarioSelecionadoId]);

  // Verifica se o dia selecionado é um feriado/recesso
  const eventoHoje = eventosCalendario.find(
    (f) => f.dataEvento === dataSelecionada && f.suspendeAula,
  );

  const diaSemanaAgenda = useMemo(() => {
    const d = new Date(`${dataSelecionada}T12:00:00`);
    return d.getDay();
  }, [dataSelecionada]);

  const configHorarioDia = useMemo(
    () => horariosFuncionamento.find((h) => h.diaSemana === diaSemanaAgenda),
    [horariosFuncionamento, diaSemanaAgenda],
  );

  const escolaFechadaDiaInteiro =
    !eventoHoje && configHorarioDia != null && !configHorarioDia.aberto;

  const faixasForaExpediente = useMemo(() => {
    if (eventoHoje || escolaFechadaDiaInteiro) return [];
    if (
      !configHorarioDia?.aberto ||
      !configHorarioDia.horarioAbertura ||
      !configHorarioDia.horarioFechamento
    ) {
      return [];
    }
    return faixasForaDoHorarioEscola(configHorarioDia.horarioAbertura, configHorarioDia.horarioFechamento);
  }, [eventoHoje, escolaFechadaDiaInteiro, configHorarioDia]);

  const termoBusca = busca.toLowerCase();
  function verificaDestaque(item: OcupacaoAgendaItem) {
    if (!termoBusca) return true;
    const hay = [
      item.titulo,
      item.subtitulo,
      item.contextoAulaExtra,
      item.categoriaCompromisso,
    ]
      .filter(Boolean)
      .join(" ")
      .toLowerCase();
    return hay.includes(termoBusca);
  }

  return (
    <>
      <AgendaOcupacaoDetalheModal
        open={detalheAgenda !== null}
        onClose={() => setDetalheAgenda(null)}
        item={detalheAgenda?.item ?? null}
        usuarioColunaNome={detalheAgenda?.usuarioNome}
      />
      <div className="flex flex-col gap-6 h-full">
        
        <div className="flex flex-col sm:flex-row sm:items-end justify-between gap-4">
          <div>
            <h1 className="text-xl font-semibold text-zinc-900">Agenda Global</h1>
            <p className="text-sm text-zinc-500 mt-0.5">Visao de aulas, reposicoes e compromissos por usuario e dia.</p>
          </div>

          <div className="flex gap-3">
            <div className="flex flex-col gap-1.5">
              <label className="text-xs font-bold text-zinc-500 uppercase tracking-wide">Dia Letivo</label>
              <div className="flex items-center bg-white border border-zinc-300 rounded-lg h-9 overflow-hidden shadow-sm">
                <input type="date" value={dataSelecionada} onChange={(e) => setDataSelecionada(e.target.value)} className="h-full px-3 text-sm font-medium text-zinc-800 outline-none w-36" />
              </div>
            </div>
            <div className="flex flex-col gap-1.5">
              <label className="text-xs font-bold text-zinc-500 uppercase tracking-wide">Destacar Aula</label>
              <input type="text" placeholder="Aula, reposicao, compromisso..." value={busca} onChange={e => setBusca(e.target.value)} className="h-9 border border-zinc-300 rounded-lg px-3 text-sm outline-none focus:border-[#1F2A35] transition w-64 shadow-sm" />
            </div>
            <div className="flex flex-col gap-1.5">
              <label className="text-xs font-bold text-zinc-500 uppercase tracking-wide">Usuario</label>
              <select
                value={usuarioSelecionadoId}
                onChange={(e) => setUsuarioSelecionadoId(e.target.value === "todos" ? "todos" : Number(e.target.value))}
                className="h-9 border border-zinc-300 rounded-lg px-3 text-sm outline-none focus:border-[#1F2A35] transition w-56 bg-white shadow-sm"
              >
                <option value="todos">Todos</option>
                {usuariosOpcoesFiltro.map((u) => (
                  <option key={u.id} value={u.id}>{u.nome}</option>
                ))}
              </select>
            </div>
          </div>
        </div>

        {erro && (
          <div className="rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700">
            {erro}
          </div>
        )}

        <div className="flex-1 bg-white border border-zinc-200 rounded-xl overflow-auto shadow-sm relative flex">
          
          {/* 👇 RENDERIZAÇÃO CONDICIONAL: SE FOR FERIADO, BLOQUEIA A AGENDA 👇 */}
          {eventoHoje ? (
            <div className={`absolute inset-0 z-50 flex flex-col items-center justify-center ${corEventoCalendario(eventoHoje.tipoEvento)} bg-opacity-10 backdrop-blur-[2px]`}>
              <svg className="mb-4 opacity-80" width="64" height="64" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.5"><rect x="3" y="4" width="18" height="18" rx="2" ry="2"/><line x1="16" y1="2" x2="16" y2="6"/><line x1="8" y1="2" x2="8" y2="6"/><line x1="3" y1="10" x2="21" y2="10"/><path d="M8 14h.01"/><path d="M12 14h.01"/><path d="M16 14h.01"/><path d="M8 18h.01"/><path d="M12 18h.01"/><path d="M16 18h.01"/></svg>
              <h2 className="text-2xl font-bold uppercase tracking-widest">{eventoHoje.descricao ?? eventoHoje.tipoEvento}</h2>
              <p className="font-medium mt-2">Não há aulas agendadas para este dia.</p>
              <p className="text-xs mt-1 opacity-70">O bloqueio foi configurado no Calendário Geral da escola.</p>
            </div>
          ) : escolaFechadaDiaInteiro ? (
            <div className="absolute inset-0 z-50 flex flex-col items-center justify-center bg-amber-50/95 text-amber-950 border border-amber-200 backdrop-blur-[2px] px-6">
              <svg className="mb-4 opacity-80" width="56" height="56" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.5" aria-hidden>
                <rect x="3" y="4" width="18" height="18" rx="2" ry="2" />
                <line x1="3" y1="10" x2="21" y2="10" />
                <path d="M8 14h8M8 18h5" />
              </svg>
              <h2 className="text-xl font-bold uppercase tracking-wide text-center">Sem aula</h2>
              <p className="font-medium mt-2 text-center max-w-md">
                Escola fechada neste dia da semana (horário de funcionamento).
              </p>
              <p className="text-xs mt-2 opacity-80 text-center max-w-sm">
                Ajuste em <span className="font-semibold">Horário de funcionamento</span> se precisar abrir este dia.
              </p>
            </div>
          ) : null}

          {/* GRID NORMAL DA AGENDA (Fica atrás do aviso se for feriado) */}
          <div className="w-16 shrink-0 bg-zinc-50 border-r border-zinc-200 sticky left-0 z-20 flex flex-col pt-12 shadow-[2px_0_5px_rgba(0,0,0,0.02)]">
            {HORAS_DIA.map(h => (
              <div key={h} className="h-[80px] relative">
                <span className="absolute -top-2.5 w-full text-center text-xs font-bold text-zinc-400">{h.toString().padStart(2, '0')}:00</span>
              </div>
            ))}
          </div>

          <div className="flex flex-1 min-w-max relative pb-10">
            <div className="absolute inset-0 pt-12 z-0 pointer-events-none flex flex-col">
              {HORAS_DIA.map(h => <div key={h} className="h-[80px] border-t border-zinc-100 w-full" />)}
            </div>

            {usuariosAgenda.map((usuario, index) => {
              const itensDoUsuario = itensAgenda.filter(i => i.usuarioId === usuario.id);
              const posicionados = itensDoUsuario.map((item) => {
                const topPx = calcularTop(item.inicio);
                const alturaReal = calcularHeight(item.inicio, item.fim);
                const alturaExibicaoPx = Math.max(alturaReal, AGENDA_ALTURA_MINIMA_CARD_OCUPACAO_PX);
                return { item, topPx, alturaExibicaoPx };
              });
              const faixas = atribuirFaixasSobrepostasAgenda(posicionados);
              return (
                <div key={usuario.id} className={`flex-1 min-w-[220px] relative ${index !== usuariosAgenda.length - 1 ? 'border-r border-zinc-100' : ''}`}>
                  <div className="h-12 bg-white border-b border-zinc-200 sticky top-0 z-10 flex items-center justify-center gap-2 px-2 shadow-sm">
                    <span className={`w-2.5 h-2.5 rounded-full shrink-0 ${corColuna(index)}`}></span>
                    <span className="text-sm font-semibold text-zinc-900 text-center leading-tight line-clamp-2">
                      {usuario.nome}
                    </span>
                  </div>
                  <div className="relative pt-12">
                    {!eventoHoje &&
                      !escolaFechadaDiaInteiro &&
                      faixasForaExpediente.map((faixa, fi) => (
                        <div
                          key={`fora-${fi}-${usuario.id}`}
                          className="absolute left-1 right-1 z-[2] flex items-center justify-center rounded-md border border-amber-300/90 bg-amber-100/90 px-1 py-0.5 pointer-events-none shadow-sm"
                          style={{ top: faixa.topPx, height: faixa.heightPx, minHeight: 8 }}
                          title="Fora do horário de funcionamento da escola — sem aula neste período"
                        >
                          <span className="text-[10px] font-bold uppercase tracking-wide text-amber-950 text-center leading-tight">
                            Sem aula · Fora do horário
                          </span>
                        </div>
                      ))}
                    {posicionados.map(({ item, topPx, alturaExibicaoPx }) => {
                      const temDestaque = verificaDestaque(item);
                      const fh = faixas.get(item.id) ?? { indice: 0, total: 1 };
                      return (
                        <AgendaOcupacaoCard
                          key={item.id}
                          item={item}
                          topPx={topPx}
                          alturaExibicaoPx={alturaExibicaoPx}
                          faixaHorizontal={fh}
                          temDestaque={temDestaque}
                          corBarraLateral={corColuna(index)}
                          onClick={() => setDetalheAgenda({ item, usuarioNome: usuario.nome })}
                        />
                      );
                    })}
                  </div>
                </div>
              );
            })}
          </div>

        </div>
      </div>
    </>
  );
}