"use client";

import { useEffect, useState } from "react";
import Link from "next/link";
import { buscarDashboardGeral, type DashboardGeral } from "@/lib/api/dashboard";
import { getApiErrorMessage } from "@/lib/api";

const turnoColors: Record<string, string> = {
  morning: "bg-amber-50 text-amber-700",
  afternoon: "bg-blue-50 text-blue-700",
  evening: "bg-purple-50 text-purple-700",
};

const turnoLabel: Record<string, string> = {
  morning: "Morning",
  afternoon: "Afternoon",
  evening: "Evening",
};

const atividadeCores: Record<string, string> = {
  matricula: "bg-blue-50 text-blue-600",
  turma: "bg-purple-50 text-purple-600",
  financeiro: "bg-green-50 text-green-600",
  usuario: "bg-amber-50 text-amber-600",
  calendario: "bg-red-50 text-red-600",
};

function formatarMoeda(valor: number): string {
  return valor.toLocaleString("pt-BR", { style: "currency", currency: "BRL" });
}

function formatarHoraRelativa(iso: string): string {
  const data = new Date(iso);
  if (Number.isNaN(data.getTime())) return "—";

  const agora = new Date();
  const inicioHoje = new Date(agora.getFullYear(), agora.getMonth(), agora.getDate());
  const inicioOntem = new Date(inicioHoje);
  inicioOntem.setDate(inicioOntem.getDate() - 1);

  const hora = data.toLocaleTimeString("pt-BR", { hour: "2-digit", minute: "2-digit" });

  if (data >= inicioHoje) {
    return `Hoje, ${hora}`;
  }

  if (data >= inicioOntem) {
    return `Ontem, ${hora}`;
  }

  return data.toLocaleString("pt-BR", {
    day: "2-digit",
    month: "2-digit",
    year: "numeric",
    hour: "2-digit",
    minute: "2-digit",
  });
}

function Icon({ type }: { type: string }) {
  if (type === "users")
    return (
      <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
        <path d="M17 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2" />
        <circle cx="9" cy="7" r="4" />
        <path d="M23 21v-2a4 4 0 0 0-3-3.87" />
        <path d="M16 3.13a4 4 0 0 1 0 7.75" />
      </svg>
    );
  if (type === "class")
    return (
      <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
        <path d="M4 19.5A2.5 2.5 0 0 1 6.5 17H20" />
        <path d="M6.5 2H20v20H6.5A2.5 2.5 0 0 1 4 19.5v-15A2.5 2.5 0 0 1 6.5 2z" />
      </svg>
    );
  if (type === "teacher")
    return (
      <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
        <path d="M20 21v-2a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v2" />
        <circle cx="12" cy="7" r="4" />
      </svg>
    );
  if (type === "dollar")
    return (
      <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
        <line x1="12" y1="1" x2="12" y2="23" />
        <path d="M17 5H9.5a3.5 3.5 0 0 0 0 7h5a3.5 3.5 0 0 1 0 7H6" />
      </svg>
    );
  if (type === "matricula")
    return (
      <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
        <path d="M16 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2" />
        <circle cx="8.5" cy="7" r="4" />
        <line x1="20" y1="8" x2="20" y2="14" />
        <line x1="23" y1="11" x2="17" y2="11" />
      </svg>
    );
  if (type === "turma")
    return (
      <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
        <path d="M4 19.5A2.5 2.5 0 0 1 6.5 17H20" />
        <path d="M6.5 2H20v20H6.5A2.5 2.5 0 0 1 4 19.5v-15A2.5 2.5 0 0 1 6.5 2z" />
      </svg>
    );
  if (type === "financeiro")
    return (
      <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
        <line x1="12" y1="1" x2="12" y2="23" />
        <path d="M17 5H9.5a3.5 3.5 0 0 0 0 7h5a3.5 3.5 0 0 1 0 7H6" />
      </svg>
    );
  if (type === "usuario")
    return (
      <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
        <path d="M20 21v-2a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v2" />
        <circle cx="12" cy="7" r="4" />
      </svg>
    );
  return (
    <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
      <rect x="3" y="4" width="18" height="18" rx="2" />
      <line x1="16" y1="2" x2="16" y2="6" />
      <line x1="8" y1="2" x2="8" y2="6" />
      <line x1="3" y1="10" x2="21" y2="10" />
    </svg>
  );
}

export default function DashboardPage() {
  const hoje = new Date().toLocaleDateString("pt-BR", {
    weekday: "long",
    day: "2-digit",
    month: "long",
    year: "numeric",
  });

  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [dados, setDados] = useState<DashboardGeral | null>(null);

  useEffect(() => {
    let ativo = true;
    (async () => {
      setLoading(true);
      setError(null);
      try {
        const res = await buscarDashboardGeral();
        if (ativo) setDados(res);
      } catch (e) {
        if (ativo) {
          setError(getApiErrorMessage(e, "Não foi possível carregar o dashboard."));
          setDados(null);
        }
      } finally {
        if (ativo) setLoading(false);
      }
    })();
    return () => {
      ativo = false;
    };
  }, []);

  const stats = dados
    ? [
        { label: "Alunos ativos", value: String(dados.resumo.alunosAtivos), icon: "users", cor: "bg-blue-50 text-blue-600" },
        { label: "Turmas ativas", value: String(dados.resumo.turmasAtivas), icon: "class", cor: "bg-purple-50 text-purple-600" },
        { label: "Professores", value: String(dados.resumo.professores), icon: "teacher", cor: "bg-amber-50 text-amber-600" },
        { label: "Parcelas em aberto", value: String(dados.resumo.parcelasEmAberto), icon: "dollar", cor: "bg-red-50 text-red-600" },
      ]
    : [];

  return (
    <div className="flex flex-col gap-6">
      <div>
        <h1 className="text-xl font-semibold text-zinc-900">Dashboard</h1>
        <p className="mt-0.5 text-sm capitalize text-zinc-500">{hoje}</p>
      </div>

      {error && (
        <p className="rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-800">{error}</p>
      )}

      {loading && !dados && <p className="py-12 text-center text-sm text-zinc-500">Carregando dashboard…</p>}

      {!loading && dados && (
        <>
          <div className="grid grid-cols-2 gap-4 lg:grid-cols-4">
            {stats.map((s) => (
              <div key={s.label} className="flex items-center gap-4 rounded-xl border border-zinc-200 bg-white p-5">
                <div className={`flex h-10 w-10 shrink-0 items-center justify-center rounded-lg ${s.cor}`}>
                  <Icon type={s.icon} />
                </div>
                <div>
                  <p className="text-2xl font-bold text-zinc-900">{s.value}</p>
                  <p className="mt-0.5 text-xs text-zinc-400">{s.label}</p>
                </div>
              </div>
            ))}
          </div>

          <div className="grid grid-cols-1 gap-6 lg:grid-cols-2">
            <div className="overflow-hidden rounded-xl border border-zinc-200 bg-white">
              <div className="flex items-center justify-between border-b border-zinc-100 px-5 py-4">
                <h2 className="text-sm font-semibold text-zinc-900">Aulas de hoje</h2>
                <span className="text-xs text-zinc-400">{dados.aulasHoje.length} aulas</span>
              </div>
              {dados.aulasHoje.length === 0 ? (
                <p className="px-5 py-8 text-center text-sm text-zinc-500">Nenhuma aula agendada para hoje.</p>
              ) : (
                <div className="divide-y divide-zinc-100">
                  {dados.aulasHoje.map((aula) => (
                    <div key={aula.aulaId} className="flex items-center justify-between gap-4 px-5 py-3">
                      <div className="flex min-w-0 items-center gap-3">
                        <div className="flex h-10 w-10 shrink-0 items-center justify-center rounded-lg bg-zinc-100">
                          <span className="text-xs font-bold text-zinc-600">{aula.horario}</span>
                        </div>
                        <div className="min-w-0">
                          <p className="truncate text-sm font-medium text-zinc-900">{aula.turmaNome}</p>
                          <p className="text-xs text-zinc-400">
                            {aula.professorNome} · {aula.totalAlunos} alunos
                          </p>
                        </div>
                      </div>
                      <span
                        className={`shrink-0 rounded-full px-2.5 py-0.5 text-xs font-medium ${turnoColors[aula.turno] ?? turnoColors.afternoon}`}
                      >
                        {turnoLabel[aula.turno] ?? aula.turno}
                      </span>
                    </div>
                  ))}
                </div>
              )}
            </div>

            <div className="overflow-hidden rounded-xl border border-zinc-200 bg-white">
              <div className="flex items-center justify-between border-b border-zinc-100 px-5 py-4">
                <h2 className="text-sm font-semibold text-zinc-900">Parcelas vencidas</h2>
                <span className="rounded-full bg-red-50 px-2 py-0.5 text-xs font-medium text-red-600">
                  {dados.parcelasVencidas.length} em aberto
                </span>
              </div>
              {dados.parcelasVencidas.length === 0 ? (
                <p className="px-5 py-8 text-center text-sm text-zinc-500">Nenhuma parcela vencida.</p>
              ) : (
                <div className="divide-y divide-zinc-100">
                  {dados.parcelasVencidas.map((p) => (
                    <div key={p.parcelaId} className="flex items-center justify-between gap-4 px-5 py-3">
                      <div className="min-w-0">
                        <p className="truncate text-sm font-medium text-zinc-900">{p.alunoNome}</p>
                        <p className="text-xs text-zinc-400">
                          {p.turmaNome} · Venceu {p.dataVencimento}
                        </p>
                      </div>
                      <div className="shrink-0 text-right">
                        <p className="text-sm font-semibold text-red-600">{formatarMoeda(p.valor)}</p>
                        <Link href="/financeiro" className="text-xs text-zinc-400 transition-colors hover:text-zinc-600">
                          Ver financeiro
                        </Link>
                      </div>
                    </div>
                  ))}
                </div>
              )}
            </div>
          </div>

          <div className="overflow-hidden rounded-xl border border-zinc-200 bg-white">
            <div className="border-b border-zinc-100 px-5 py-4">
              <h2 className="text-sm font-semibold text-zinc-900">Atividade recente</h2>
            </div>
            {dados.atividadeRecente.length === 0 ? (
              <p className="px-5 py-8 text-center text-sm text-zinc-500">Nenhuma atividade recente.</p>
            ) : (
              <div className="divide-y divide-zinc-100">
                {dados.atividadeRecente.map((a, i) => (
                  <div key={`${a.tipo}-${a.ocorridoEm}-${i}`} className="flex items-center gap-4 px-5 py-3">
                    <div
                      className={`flex h-7 w-7 shrink-0 items-center justify-center rounded-lg ${atividadeCores[a.tipo] ?? "bg-zinc-50 text-zinc-600"}`}
                    >
                      <Icon type={a.tipo} />
                    </div>
                    <div className="min-w-0 flex-1">
                      <p className="text-sm text-zinc-900">{a.acao}</p>
                      <p className="truncate text-xs text-zinc-400">{a.detalhe}</p>
                    </div>
                    <span className="shrink-0 text-xs text-zinc-400">{formatarHoraRelativa(a.ocorridoEm)}</span>
                  </div>
                ))}
              </div>
            )}
          </div>
        </>
      )}
    </div>
  );
}
