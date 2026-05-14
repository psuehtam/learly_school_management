"use client";

import { useEffect, useState } from "react";
import {
  listarEventos,
  listarAulas,
  listarCompromissosAgendaGlobal,
  listarParticipantesCompromissos,
  type EventoCalendario,
  type Aula,
  type Compromisso,
  type ParticipanteCompromisso,
  getApiErrorMessage,
} from "@/lib/api";

interface UsuarioAgenda {
  id: number;
  nome: string;
}

type ItemAgenda =
  | { id: string; usuarioId: number; inicio: string; fim: string; titulo: string; subtitulo: string; tipo: "AULA" | "REPOSICAO" }
  | { id: string; usuarioId: number; inicio: string; fim: string; titulo: string; subtitulo: string; tipo: "COMPROMISSO" };

const HORAS_DIA = [8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21];

function calcularTop(horario: string) {
  const [h, m] = horario.split(":").map(Number);
  return ((h - 8) * 60 + m) * (80 / 60);
}

function calcularHeight(inicio: string, fim: string) {
  const [h1, m1] = inicio.split(":").map(Number);
  const [h2, m2] = fim.split(":").map(Number);
  return ((h2 * 60 + m2) - (h1 * 60 + m1)) * (80 / 60);
}

function corEventoCalendario(tipoEvento: EventoCalendario["tipoEvento"]) {
  if (tipoEvento === "FERIADO") return "bg-red-50 text-red-700 border-red-200";
  if (tipoEvento === "RECESSO") return "bg-blue-50 text-blue-700 border-blue-200";
  return "bg-amber-50 text-amber-700 border-amber-200";
}

function toTime(value: string) {
  return value.length >= 5 ? value.slice(0, 5) : value;
}

function corColuna(index: number) {
  const cores = ["bg-blue-600", "bg-purple-600", "bg-emerald-600", "bg-orange-600", "bg-pink-600", "bg-cyan-600"];
  return cores[index % cores.length];
}

function cardClasses(tipo: ItemAgenda["tipo"]) {
  if (tipo === "REPOSICAO") return "bg-amber-50 border-amber-200 text-amber-900";
  if (tipo === "COMPROMISSO") return "bg-violet-50 border-violet-200 text-violet-900";
  return "bg-blue-50 border-blue-200 text-blue-900";
}

// ─── Página Principal ─────────────────────────────────────────────────────────
export default function AgendaGlobalPage() {
  const [dataSelecionada, setDataSelecionada] = useState(new Date().toISOString().slice(0, 10));
  const [busca, setBusca] = useState("");
  const [usuarioSelecionadoId, setUsuarioSelecionadoId] = useState<number | "todos">("todos");
  const [eventosCalendario, setEventosCalendario] = useState<EventoCalendario[]>([]);
  const [usuariosAgenda, setUsuariosAgenda] = useState<UsuarioAgenda[]>([]);
  const [itensAgenda, setItensAgenda] = useState<ItemAgenda[]>([]);
  const [erro, setErro] = useState<string | null>(null);

  useEffect(() => {
    async function carregarAgenda() {
      setErro(null);
      try {
        const data = new Date(`${dataSelecionada}T00:00:00`);
        const filtroUsuario = usuarioSelecionadoId === "todos" ? undefined : usuarioSelecionadoId;
        const [eventos, aulas, compromissos, participantes] = await Promise.all([
          listarEventos(data.getMonth() + 1, data.getFullYear()),
          listarAulas(),
          listarCompromissosAgendaGlobal(dataSelecionada, filtroUsuario),
          listarParticipantesCompromissos(),
        ]);

        setEventosCalendario(eventos);

        const mapaUsuarios = new Map<number, string>();
        participantes.forEach((p: ParticipanteCompromisso) => mapaUsuarios.set(p.id, p.nomeCompleto));

        const aulasDoDia = aulas.filter((a: Aula) => a.dataAula === dataSelecionada && a.status !== "Cancelada");
        const aulasFiltradas = filtroUsuario
          ? aulasDoDia.filter((a: Aula) => a.professorId === filtroUsuario)
          : aulasDoDia;

        const itensAula: ItemAgenda[] = aulasFiltradas.map((a: Aula) => ({
          id: `aula-${a.id}`,
          usuarioId: a.professorId,
          inicio: toTime(a.horarioInicio),
          fim: toTime(a.horarioFim),
          titulo: a.tipoAula === "Reposicao" ? `Reposicao #${a.numeroAula}` : `Aula #${a.numeroAula}`,
          subtitulo: `Turma ${a.turmaId}`,
          tipo: a.tipoAula === "Reposicao" ? "REPOSICAO" : "AULA",
        }));

        const compromissosValidos = compromissos.filter((c: Compromisso) => c.status !== "Cancelado");
        const itensCompromisso: ItemAgenda[] = compromissosValidos.flatMap((c: Compromisso) => {
          const ini = new Date(c.dataInicio);
          const fim = new Date(c.dataFim);
          const inicio = `${String(ini.getHours()).padStart(2, "0")}:${String(ini.getMinutes()).padStart(2, "0")}`;
          const termino = `${String(fim.getHours()).padStart(2, "0")}:${String(fim.getMinutes()).padStart(2, "0")}`;
          return c.participantesUsuarioIds
            .filter((uid) => (filtroUsuario ? uid === filtroUsuario : true))
            .map((uid) => ({
              id: `comp-${c.id}-${uid}`,
              usuarioId: uid,
              inicio,
              fim: termino,
              titulo: c.titulo,
              subtitulo: c.local ?? "Compromisso",
              tipo: "COMPROMISSO" as const,
            }));
        });

        const todosItens = [...itensAula, ...itensCompromisso];
        setItensAgenda(todosItens);

        const ids = Array.from(new Set(todosItens.map((i) => i.usuarioId)));
        const usuarios = ids.map((id) => ({
          id,
          nome: mapaUsuarios.get(id) ?? `Usuario ${id}`,
        }));
        setUsuariosAgenda(usuarios);
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

  const termoBusca = busca.toLowerCase();
  function verificaDestaque(item: ItemAgenda) {
    if (!termoBusca) return true;
    return item.titulo.toLowerCase().includes(termoBusca) || item.subtitulo.toLowerCase().includes(termoBusca);
  }

  return (
    <>
      <div className="flex flex-col gap-6 h-full">
        
        <div className="flex flex-col sm:flex-row sm:items-end justify-between gap-4">
          <div>
            <h1 className="text-xl font-semibold text-zinc-900">Agenda Global</h1>
            <p className="text-sm text-zinc-500 mt-0.5">Visao de aulas, reposicoes e compromissos por usuario e dia.</p>
          </div>

          <div className="flex gap-3">
            {/* O SEGREDO ESTÁ AQUI: TESTE COLOCAR "2026-02-16" NESSE CALENDÁRIO */}
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
                {usuariosAgenda.map((u) => (
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
              return (
                <div key={usuario.id} className={`flex-1 min-w-[220px] relative ${index !== usuariosAgenda.length - 1 ? 'border-r border-zinc-100' : ''}`}>
                  <div className="h-12 bg-white border-b border-zinc-200 sticky top-0 z-10 flex items-center justify-center gap-2 shadow-sm">
                    <span className={`w-2.5 h-2.5 rounded-full ${corColuna(index)}`}></span>
                    <span className="text-sm font-bold text-zinc-800">{usuario.nome}</span>
                  </div>
                  <div className="relative pt-12">
                    {itensDoUsuario.map(item => {
                      const top = calcularTop(item.inicio);
                      const height = calcularHeight(item.inicio, item.fim);
                      const temDestaque = verificaDestaque(item);
                      return (
                        <div key={item.id} style={{ top: `${top}px`, height: `${height}px` }} className={`absolute left-1 right-1 rounded-lg p-2.5 border cursor-pointer overflow-hidden transition-all duration-200 ${temDestaque ? 'opacity-100 hover:shadow-md z-10' : 'opacity-30 grayscale hover:opacity-50'} ${cardClasses(item.tipo)}`}>
                          <div className={`absolute left-0 top-0 bottom-0 w-1 ${corColuna(index)}`}></div>
                          <div className="flex flex-col h-full ml-1 justify-between">
                            <div>
                              <h4 className="text-xs font-bold leading-tight">{item.titulo}</h4>
                              <p className="text-[10px] font-medium mt-0.5">{item.inicio} - {item.fim}</p>
                              <p className="text-[10px] mt-1 opacity-80">{item.subtitulo}</p>
                            </div>
                          </div>
                        </div>
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