"use client";

import { useCallback, useEffect, useMemo, useState } from "react";
import Link from "next/link";
import { useParams } from "next/navigation";
import { buscarTurma, listarAvaliacoesTurma, salvarAvaliacoesTurma } from "@/lib/api/turmas";
import { listarMatriculas } from "@/lib/api/matriculas";
import {
  listarAulas,
  listarPresencas,
  listarHomework,
  registrarChamada,
  lancarHomework,
} from "@/lib/api/aulas";
import { getApiErrorMessage } from "@/lib/api";
import type { Turma } from "@/types/turma";
type PresencaCelula = "P" | "F" | "R" | null;

interface AulaRow {
  id: number;
  data: string;
  numero: number;
  capitulo: string;
}

interface AlunoRow {
  id: number;
  nome: string;
}

const DIAS = ["Dom", "Seg", "Ter", "Qua", "Qui", "Sex", "Sáb"];

const avaliacoesTipos = [
  "Speaking",
  "Listening",
  "Writing",
  "Class Participation",
  "Avaliação Final",
];

function formatarDataBr(iso: string): string {
  if (!iso) return "—";
  const [y, m, d] = iso.split("-");
  if (!y || !m || !d) return iso;
  return `${d}/${m}/${y}`;
}

function diasLabel(dias: number[]): string {
  if (!dias.length) return "—";
  return dias.map((d) => DIAS[d] ?? "").filter(Boolean).join(", ");
}

function horarioLabel(t: Turma): string {
  if (t.horarioInicio && t.horarioFim) return `${t.horarioInicio} – ${t.horarioFim}`;
  if (t.horarioInicio) return t.horarioInicio;
  return "—";
}

function statusPresencaParaCelula(s: string): PresencaCelula {
  if (s === "P" || s === "F" || s === "R") return s;
  if (s === "FJ") return "F";
  return null;
}

function celulaParaApi(v: PresencaCelula): string | null {
  if (v === "P" || v === "F") return v;
  return null;
}

function tipoAvaliacaoParaUi(tipo: string): string {
  if (tipo === "Avaliacao Final") return "Avaliação Final";
  return tipo;
}

function tipoAvaliacaoParaApi(tipo: string): string {
  if (tipo === "Avaliação Final") return "Avaliacao Final";
  return tipo;
}

function CelulaPresenca({
  valor,
  bloqueada,
  onChange,
}: {
  valor: PresencaCelula;
  bloqueada?: boolean;
  onChange: (v: PresencaCelula) => void;
}) {
  const cores: Record<string, string> = {
    P: "bg-green-100 text-green-700 border-green-300 shadow-sm",
    F: "bg-red-100 text-red-700 border-red-300 shadow-sm",
    R: "bg-blue-100 text-blue-700 border-blue-300 shadow-sm",
  };

  if (valor === "R" || bloqueada) {
    return (
      <div
        title={valor === "R" ? "Falta reposta" : "Justificada na secretaria"}
        className={`mx-auto flex h-8 w-8 cursor-not-allowed items-center justify-center rounded border text-xs font-bold opacity-80 ${cores[valor === "R" ? "R" : "F"]}`}
      >
        {valor === "R" ? "R" : "FJ"}
      </div>
    );
  }

  function ciclar() {
    if (valor === null) onChange("P");
    else if (valor === "P") onChange("F");
    else onChange(null);
  }

  return (
    <button
      type="button"
      onClick={ciclar}
      className={`mx-auto h-8 w-8 rounded border text-xs font-bold transition-all hover:scale-105 ${
        valor ? cores[valor] : "border-zinc-200 bg-zinc-50 text-zinc-400 hover:bg-zinc-100"
      }`}
    >
      {valor ?? "·"}
    </button>
  );
}

export default function PainelTurmaProfessorPage() {
  const params = useParams();
  const turmaId = Number(params.id);

  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [salvando, setSalvando] = useState(false);
  const [msgSalvo, setMsgSalvo] = useState<string | null>(null);

  const [turma, setTurma] = useState<Turma | null>(null);
  const [alunos, setAlunos] = useState<AlunoRow[]>([]);
  const [aulas, setAulas] = useState<AulaRow[]>([]);
  const [presencasBloqueadas, setPresencasBloqueadas] = useState<Record<string, boolean>>({});

  const [aba, setAba] = useState<"presenca" | "homework" | "avaliacoes">("presenca");
  const [presencas, setPresencas] = useState<Record<string, PresencaCelula>>({});
  const [homework, setHomework] = useState<Record<string, string>>({});
  const [avaliacoes, setAvaliacoes] = useState<Record<string, string>>({});

  const carregar = useCallback(async () => {
    if (!Number.isFinite(turmaId) || turmaId <= 0) {
      setError("Turma inválida.");
      setLoading(false);
      return;
    }

    setLoading(true);
    setError(null);
    try {
      const [turmaData, mats, todasAulas] = await Promise.all([
        buscarTurma(turmaId),
        listarMatriculas({ turmaId, status: "Ativo" }),
        listarAulas(),
      ]);

      setTurma(turmaData);

      const alunosRows: AlunoRow[] = mats.map((m) => ({
        id: m.alunoId,
        nome: m.alunoNomeCompleto?.trim() || `Aluno #${m.alunoId}`,
      }));
      setAlunos(alunosRows);

      const aulasTurma = todasAulas
        .filter((a) => a.turmaId === turmaId && a.status !== "Cancelada")
        .sort((a, b) => a.numeroAula - b.numeroAula || a.dataAula.localeCompare(b.dataAula))
        .map((a) => ({
          id: a.id,
          data: formatarDataBr(a.dataAula),
          numero: a.numeroAula,
          capitulo: a.conteudoDado?.trim() || `Aula ${a.numeroAula}`,
        }));
      setAulas(aulasTurma);

      const presMap: Record<string, PresencaCelula> = {};
      const bloq: Record<string, boolean> = {};
      const hwMap: Record<string, string> = {};

      await Promise.all(
        aulasTurma.map(async (aula) => {
          try {
            const lista = await listarPresencas(aula.id);
            for (const p of lista) {
              const key = `${p.alunoId}-${aula.id}`;
              const st = p.statusPresenca;
              presMap[key] = statusPresencaParaCelula(st);
              bloq[key] = st === "R" || st === "FJ";
            }
          } catch {
            /* endpoint pode não existir ainda */
          }
          try {
            const hws = await listarHomework(aula.id);
            for (const h of hws) {
              if (h.nota != null) {
                hwMap[`${h.alunoId}-${aula.id}`] = String(h.nota);
              }
            }
          } catch {
            /* idem */
          }
        }),
      );

      setPresencas(presMap);
      setPresencasBloqueadas(bloq);
      setHomework(hwMap);

      try {
        const avs = await listarAvaliacoesTurma(turmaId);
        const avMap: Record<string, string> = {};
        for (const a of avs) {
          avMap[`${a.alunoId}-${tipoAvaliacaoParaUi(a.tipo)}`] = String(a.nota);
        }
        setAvaliacoes(avMap);
      } catch {
        /* endpoint pode não existir ainda */
      }
    } catch (e) {
      setError(getApiErrorMessage(e, "Não foi possível carregar a turma."));
      setTurma(null);
      setAlunos([]);
      setAulas([]);
    } finally {
      setLoading(false);
    }
  }, [turmaId]);

  useEffect(() => {
    void carregar();
  }, [carregar]);

  const headerRows = useMemo(() => {
    if (!turma) return [];
    return [
      { label: "Book", value: turma.livroNome ?? `Livro #${turma.livroId}` },
      { label: "Room", value: turma.sala ?? "—" },
      { label: "Teacher", value: turma.professorNome ?? "—" },
      { label: "Day & Time", value: `${diasLabel(turma.diasSemana ?? [])}, ${horarioLabel(turma)}` },
      { label: "Start Date", value: turma.dataInicio ? formatarDataBr(turma.dataInicio) : "—" },
      {
        label: "End Date",
        value: turma.dataTerminoPrevista ? formatarDataBr(turma.dataTerminoPrevista) : "—",
      },
    ];
  }, [turma]);

  function setPresenca(alunoId: number, aulaId: number, valor: PresencaCelula) {
    const key = `${alunoId}-${aulaId}`;
    if (presencasBloqueadas[key]) return;
    setPresencas((prev) => ({ ...prev, [key]: valor }));
  }

  function setHW(alunoId: number, aulaId: number, valor: string) {
    setHomework((prev) => ({ ...prev, [`${alunoId}-${aulaId}`]: valor }));
  }

  function setAval(alunoId: number, tipo: string, valor: string) {
    setAvaliacoes((prev) => ({ ...prev, [`${alunoId}-${tipo}`]: valor }));
  }

  function contarFaltas(alunoId: number) {
    return aulas.filter((a) => presencas[`${alunoId}-${a.id}`] === "F").length;
  }

  function mediaHW(alunoId: number) {
    const notas = aulas
      .map((a) => parseFloat(homework[`${alunoId}-${a.id}`] || ""))
      .filter((n) => !Number.isNaN(n));
    if (!notas.length) return "—";
    return (notas.reduce((acc, n) => acc + n, 0) / notas.length).toFixed(1);
  }

  function mediaAval(alunoId: number) {
    const notas = avaliacoesTipos
      .map((t) => parseFloat(avaliacoes[`${alunoId}-${t}`] || ""))
      .filter((n) => !Number.isNaN(n));
    if (!notas.length) return "—";
    return (notas.reduce((acc, n) => acc + n, 0) / notas.length).toFixed(1);
  }

  async function salvarPresencas() {
    setSalvando(true);
    setMsgSalvo(null);
    try {
      for (const aula of aulas) {
        const payload = alunos
          .map((aluno) => {
            const api = celulaParaApi(presencas[`${aluno.id}-${aula.id}`] ?? null);
            return api ? { alunoId: aluno.id, status: api } : null;
          })
          .filter((x): x is { alunoId: number; status: string } => x != null);
        await registrarChamada(aula.id, payload);
      }
      setMsgSalvo("Presenças salvas.");
      await carregar();
    } catch (e) {
      alert(getApiErrorMessage(e, "Não foi possível salvar as presenças. Verifique se a API está atualizada."));
    } finally {
      setSalvando(false);
    }
  }

  async function salvarAvaliacoes() {
    setSalvando(true);
    setMsgSalvo(null);
    try {
      const payload = alunos.flatMap((aluno) =>
        avaliacoesTipos
          .map((tipo) => {
            const raw = avaliacoes[`${aluno.id}-${tipo}`];
            if (raw === undefined || raw === "") return null;
            const nota = parseFloat(raw);
            if (Number.isNaN(nota)) return null;
            return {
              alunoId: aluno.id,
              tipo: tipoAvaliacaoParaApi(tipo),
              nota,
            };
          })
          .filter((x): x is { alunoId: number; tipo: string; nota: number } => x != null),
      );
      await salvarAvaliacoesTurma(turmaId, payload);
      setMsgSalvo("Avaliações salvas.");
    } catch (e) {
      alert(getApiErrorMessage(e, "Não foi possível salvar as avaliações."));
    } finally {
      setSalvando(false);
    }
  }

  async function salvarHomework() {
    setSalvando(true);
    setMsgSalvo(null);
    try {
      for (const aula of aulas) {
        const notas = alunos
          .map((aluno) => {
            const raw = homework[`${aluno.id}-${aula.id}`];
            if (raw === undefined || raw === "") return null;
            const nota = parseFloat(raw);
            return Number.isNaN(nota) ? null : { alunoId: aluno.id, nota };
          })
          .filter((x): x is { alunoId: number; nota: number } => x != null);
        if (notas.length > 0) {
          await lancarHomework(aula.id, notas);
        }
      }
      setMsgSalvo("Notas de homework salvas.");
    } catch (e) {
      alert(getApiErrorMessage(e, "Não foi possível salvar as notas."));
    } finally {
      setSalvando(false);
    }
  }

  if (loading) {
    return <p className="py-16 text-center text-sm text-zinc-500">Carregando painel da turma…</p>;
  }

  if (error || !turma) {
    return (
      <div className="flex flex-col gap-4">
        <Link href="/turmas" className="flex w-fit items-center gap-1.5 text-sm text-zinc-500 hover:text-zinc-800">
          Voltar para minhas turmas
        </Link>
        <p className="rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-800">
          {error ?? "Turma não encontrada."}
        </p>
      </div>
    );
  }

  const abas = [
    { key: "presenca" as const, label: "Controle de Presença" },
    { key: "homework" as const, label: "Homework" },
    { key: "avaliacoes" as const, label: "Avaliações" },
  ];

  return (
    <div className="flex flex-col gap-6 pb-8">
      <Link
        href="/turmas"
        className="flex w-fit items-center gap-1.5 text-sm text-zinc-500 transition-colors hover:text-zinc-800"
      >
        <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
          <polyline points="15 18 9 12 15 6" />
        </svg>
        Voltar para minhas turmas
      </Link>

      <div className="rounded-xl border border-zinc-200 bg-white p-5">
        <h1 className="text-lg font-bold text-zinc-900">{turma.nome}</h1>
        <p className="mt-0.5 text-sm text-zinc-500">Presença, homework e avaliações da turma</p>

        <div className="mt-4 w-full max-w-md overflow-hidden rounded-lg border border-zinc-200 bg-zinc-50 text-sm">
          {headerRows.map(({ label, value }, i) => (
            <div key={label} className={`flex border-zinc-200 ${i !== 0 ? "border-t" : ""}`}>
              <div className="w-1/3 border-r border-zinc-200 bg-zinc-100 px-3 py-2 font-semibold text-zinc-600">
                {label}
              </div>
              <div className="w-2/3 bg-white px-3 py-2 font-medium text-zinc-800">{value}</div>
            </div>
          ))}
        </div>
      </div>

      {msgSalvo && (
        <p className="rounded-lg border border-green-200 bg-green-50 px-3 py-2 text-sm text-green-800">
          {msgSalvo}
        </p>
      )}

      <div className="flex gap-1 border-b border-zinc-200">
        {abas.map(({ key, label }) => (
          <button
            key={key}
            type="button"
            onClick={() => setAba(key)}
            className={`border-b-2 px-4 py-2.5 text-sm font-medium transition-colors ${
              aba === key ? "border-[#1F2A35] text-[#1F2A35]" : "border-transparent text-zinc-500 hover:text-zinc-700"
            }`}
          >
            {label}
          </button>
        ))}
      </div>

      {aba === "presenca" && (
        <div className="flex flex-col gap-3">
          <div className="flex items-center justify-between">
            <p className="text-xs text-zinc-500">Clique na célula: · → P → F → ·</p>
            <button
              type="button"
              disabled={salvando || alunos.length === 0}
              onClick={() => void salvarPresencas()}
              className="h-8 rounded-lg bg-[#1F2A35] px-3 text-xs font-medium text-white hover:bg-[#2d3d4d] disabled:opacity-50"
            >
              {salvando ? "Salvando…" : "Salvar presenças"}
            </button>
          </div>

          {aulas.length === 0 ? (
            <p className="rounded-xl border border-dashed border-zinc-300 py-12 text-center text-sm text-zinc-500">
              Nenhuma aula agendada nesta turma.
            </p>
          ) : (
            <div className="overflow-x-auto rounded-xl border border-zinc-200 bg-white">
              <table className="w-full text-sm">
                <thead>
                  <tr className="border-b border-zinc-200 bg-zinc-50">
                    <th className="w-16 border-r border-zinc-200 px-4 py-3 text-center font-semibold text-zinc-500">
                      Nº
                    </th>
                    <th className="w-28 border-r border-zinc-200 px-4 py-3 text-center font-semibold text-zinc-500">
                      Data
                    </th>
                    <th className="min-w-[200px] border-r border-zinc-200 px-4 py-3 text-left font-semibold text-zinc-500">
                      Capítulo / Lição
                    </th>
                    {alunos.map((aluno) => (
                      <th
                        key={aluno.id}
                        className="h-40 w-12 min-w-[48px] max-w-[48px] border-r border-zinc-200 px-1 py-3 align-bottom font-medium text-zinc-500"
                      >
                        <span
                          className="mx-auto whitespace-nowrap"
                          style={{ writingMode: "vertical-rl", transform: "rotate(180deg)" }}
                          title={aluno.nome}
                        >
                          {aluno.nome}
                        </span>
                      </th>
                    ))}
                  </tr>
                </thead>
                <tbody>
                  {aulas.map((aula) => (
                    <tr key={aula.id} className="border-b border-zinc-100 hover:bg-zinc-50">
                      <td className="border-r border-zinc-200 px-4 py-2 text-center font-medium text-zinc-500">
                        {aula.numero}
                      </td>
                      <td className="border-r border-zinc-200 px-4 py-2 text-center text-zinc-600">{aula.data}</td>
                      <td className="border-r border-zinc-200 px-4 py-2 font-medium text-zinc-700">{aula.capitulo}</td>
                      {alunos.map((aluno) => {
                        const key = `${aluno.id}-${aula.id}`;
                        return (
                          <td key={aluno.id} className="border-r border-zinc-200 px-1 py-2 text-center">
                            <CelulaPresenca
                              valor={presencas[key] ?? null}
                              bloqueada={presencasBloqueadas[key]}
                              onChange={(v) => setPresenca(aluno.id, aula.id, v)}
                            />
                          </td>
                        );
                      })}
                    </tr>
                  ))}
                  <tr className="border-t border-zinc-200 bg-zinc-50">
                    <td
                      colSpan={3}
                      className="border-r border-zinc-200 px-4 py-3 text-right text-xs font-semibold uppercase tracking-wide text-zinc-600"
                    >
                      Total de faltas
                    </td>
                    {alunos.map((aluno) => (
                      <td key={aluno.id} className="border-r border-zinc-200 px-1 py-3 text-center">
                        <span
                          className={`text-sm font-bold ${contarFaltas(aluno.id) > 0 ? "text-red-600" : "text-zinc-400"}`}
                        >
                          {contarFaltas(aluno.id)}
                        </span>
                      </td>
                    ))}
                  </tr>
                </tbody>
              </table>
            </div>
          )}

          <div className="mt-1 flex gap-4 text-xs text-zinc-400">
            <span className="flex items-center gap-1.5">
              <span className="flex h-5 w-5 items-center justify-center rounded border border-green-300 bg-green-100 text-xs font-bold text-green-700">
                P
              </span>
              Presente
            </span>
            <span className="flex items-center gap-1.5">
              <span className="flex h-5 w-5 items-center justify-center rounded border border-red-300 bg-red-100 text-xs font-bold text-red-700">
                F
              </span>
              Falta
            </span>
            <span className="flex items-center gap-1.5">
              <span className="flex h-5 w-5 items-center justify-center rounded border border-blue-300 bg-blue-100 text-xs font-bold text-blue-700">
                R
              </span>
              Reposição
            </span>
          </div>
        </div>
      )}

      {aba === "homework" && (
        <div className="flex flex-col gap-3">
          <div className="flex items-center justify-between">
            <p className="text-xs text-zinc-500">Notas de homework por aula (0 a 100)</p>
            <button
              type="button"
              disabled={salvando}
              onClick={() => void salvarHomework()}
              className="h-8 rounded-lg bg-[#1F2A35] px-3 text-xs font-medium text-white hover:bg-[#2d3d4d] disabled:opacity-50"
            >
              {salvando ? "Salvando…" : "Salvar notas"}
            </button>
          </div>
          <div className="overflow-x-auto rounded-xl border border-zinc-200 bg-white">
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b border-zinc-200 bg-zinc-50">
                  <th className="w-16 border-r border-zinc-200 px-4 py-3 text-center font-semibold text-zinc-500">
                    HW
                  </th>
                  <th className="w-32 border-r border-zinc-200 px-4 py-3 text-center font-semibold text-zinc-500">
                    Data
                  </th>
                  {alunos.map((aluno) => (
                    <th
                      key={aluno.id}
                      className="h-40 w-14 min-w-[56px] border-r border-zinc-200 px-1 py-3 align-bottom font-medium text-zinc-500"
                    >
                      <span
                        className="mx-auto whitespace-nowrap"
                        style={{ writingMode: "vertical-rl", transform: "rotate(180deg)" }}
                      >
                        {aluno.nome}
                      </span>
                    </th>
                  ))}
                </tr>
              </thead>
              <tbody>
                {aulas.map((aula) => (
                  <tr key={aula.id} className="border-b border-zinc-100">
                    <td className="border-r border-zinc-200 px-4 py-2 text-center font-medium text-zinc-500">
                      {aula.numero}
                    </td>
                    <td className="border-r border-zinc-200 px-4 py-2 text-center text-zinc-600">{aula.data}</td>
                    {alunos.map((aluno) => (
                      <td key={aluno.id} className="border-r border-zinc-200 px-1 py-2 text-center">
                        <input
                          type="number"
                          min={0}
                          max={100}
                          value={homework[`${aluno.id}-${aula.id}`] ?? ""}
                          onChange={(e) => setHW(aluno.id, aula.id, e.target.value)}
                          placeholder="—"
                          className="h-8 w-11 rounded border border-zinc-200 text-center text-xs outline-none focus:border-[#1F2A35] focus:ring-2 focus:ring-[#1F2A35]/10"
                        />
                      </td>
                    ))}
                  </tr>
                ))}
                <tr className="border-t border-zinc-200 bg-zinc-50">
                  <td
                    colSpan={2}
                    className="border-r border-zinc-200 px-4 py-3 text-right text-xs font-semibold uppercase text-zinc-600"
                  >
                    Média
                  </td>
                  {alunos.map((aluno) => (
                    <td key={aluno.id} className="border-r border-zinc-200 px-1 py-3 text-center text-sm font-bold">
                      {mediaHW(aluno.id)}
                    </td>
                  ))}
                </tr>
              </tbody>
            </table>
          </div>
        </div>
      )}

      {aba === "avaliacoes" && (
        <div className="flex flex-col gap-3">
          <div className="flex items-center justify-between">
            <p className="text-xs text-zinc-500">Avaliações por livro (0 a 100)</p>
            <button
              type="button"
              disabled={salvando || alunos.length === 0}
              onClick={() => void salvarAvaliacoes()}
              className="h-8 rounded-lg bg-[#1F2A35] px-3 text-xs font-medium text-white hover:bg-[#2d3d4d] disabled:opacity-50"
            >
              {salvando ? "Salvando…" : "Salvar avaliações"}
            </button>
          </div>
          <div className="overflow-x-auto rounded-xl border border-zinc-200 bg-white">
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b border-zinc-200 bg-zinc-50">
                  <th className="min-w-[180px] border-r border-zinc-200 px-4 py-3 text-left font-semibold text-zinc-500">
                    Aluno
                  </th>
                  {avaliacoesTipos.map((t) => (
                    <th
                      key={t}
                      className="min-w-[100px] border-r border-zinc-200 px-2 py-3 text-center text-xs font-semibold uppercase text-zinc-500"
                    >
                      {t}
                    </th>
                  ))}
                  <th className="px-3 py-3 text-center text-xs font-semibold uppercase text-zinc-500">Média</th>
                </tr>
              </thead>
              <tbody>
                {alunos.map((aluno) => (
                  <tr key={aluno.id} className="border-b border-zinc-100 hover:bg-zinc-50">
                    <td className="border-r border-zinc-200 px-4 py-3 font-medium text-zinc-900">{aluno.nome}</td>
                    {avaliacoesTipos.map((tipo) => (
                      <td key={tipo} className="border-r border-zinc-200 px-2 py-3 text-center">
                        <input
                          type="number"
                          min={0}
                          max={100}
                          value={avaliacoes[`${aluno.id}-${tipo}`] ?? ""}
                          onChange={(e) => setAval(aluno.id, tipo, e.target.value)}
                          placeholder="—"
                          className="h-8 w-14 rounded border border-zinc-200 text-center text-sm outline-none focus:border-[#1F2A35]"
                        />
                      </td>
                    ))}
                    <td className="bg-zinc-50 px-3 py-3 text-center text-sm font-bold">{mediaAval(aluno.id)}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      )}
    </div>
  );
}
