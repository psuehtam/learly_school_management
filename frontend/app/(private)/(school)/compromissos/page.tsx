"use client";

import { useEffect, useMemo, useState } from "react";
import {
  criarCompromisso,
  listarCompromissos,
  listarParticipantesCompromissos,
  cancelarCompromisso,
  editarCompromisso,
  getApiErrorMessage,
  type Compromisso,
  type ParticipanteCompromisso,
} from "@/lib/api";

type FormState = {
  titulo: string;
  descricao: string;
  data: string;
  horaInicio: string;
  horaFim: string;
  local: string;
  participantesUsuarioIds: number[];
};

const initialForm: FormState = {
  titulo: "",
  descricao: "",
  data: "",
  horaInicio: "",
  horaFim: "",
  local: "",
  participantesUsuarioIds: [],
};

type AbaLista = "pendente" | "em_andamento" | "concluido" | "cancelado";

function formatDateLocal(date: Date): string {
  const year = date.getFullYear();
  const month = String(date.getMonth() + 1).padStart(2, "0");
  const day = String(date.getDate()).padStart(2, "0");
  return `${year}-${month}-${day}`;
}

/** Motivo gravado pelo backend na última linha `[CANCELADO EM …] Motivo: …`. */
function extrairMotivoCancelamento(descricao: string | undefined): string | null {
  if (!descricao?.trim()) return null;
  const lines = descricao.split("\n");
  for (let i = lines.length - 1; i >= 0; i--) {
    const line = lines[i]!.trim();
    const m = line.match(/^\[CANCELADO EM .+\]\s*Motivo:\s*(.+)$/);
    if (m?.[1]) return m[1].trim();
  }
  return null;
}

const INPUT_CANCEL_MODAL =
  "mt-3 w-full min-h-[100px] rounded-xl border border-zinc-200 bg-zinc-50/80 px-4 py-3 text-sm text-zinc-900 placeholder:text-zinc-400 shadow-inner outline-none transition focus:border-red-300 focus:bg-white focus:ring-2 focus:ring-red-100";

function dataHojeIsoLocal(): string {
  const d = new Date();
  return formatDateLocal(d);
}

/** Data + hora início e só hora fim, tudo 24h (pt-BR, hour12: false). */
function formatarPeriodoCompromisso(dataInicioIso: string, dataFimIso: string): string {
  const i = new Date(dataInicioIso);
  const f = new Date(dataFimIso);
  const dataStr = i.toLocaleDateString("pt-BR", {
    day: "2-digit",
    month: "short",
    year: "numeric",
  });
  const hi = i.toLocaleTimeString("pt-BR", { hour: "2-digit", minute: "2-digit", hour12: false });
  const hf = f.toLocaleTimeString("pt-BR", { hour: "2-digit", minute: "2-digit", hour12: false });
  return `${dataStr} · ${hi} – ${hf}`;
}

function nomesParticipantes(ids: number[], participantes: ParticipanteCompromisso[]): string {
  const map = new Map(participantes.map((p) => [p.id, p.nomeCompleto]));
  return ids.map((id) => map.get(id) ?? `Usuário ${id}`).join(", ");
}

function IconRelogio({ className }: { className?: string }) {
  return (
    <svg className={className} width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" aria-hidden>
      <circle cx="12" cy="12" r="9" />
      <path d="M12 7v5l3 2" />
    </svg>
  );
}

function IconLocal({ className }: { className?: string }) {
  return (
    <svg className={className} width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" aria-hidden>
      <path d="M12 21s7-4.35 7-10a7 7 0 1 0-14 0c0 5.65 7 10 7 10z" />
      <circle cx="12" cy="11" r="2.5" />
    </svg>
  );
}

function IconPessoas({ className }: { className?: string }) {
  return (
    <svg className={className} width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" aria-hidden>
      <path d="M17 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2" />
      <circle cx="9" cy="7" r="4" />
      <path d="M23 21v-2a4 4 0 0 0-3-3.87M16 3.13a4 4 0 0 1 0 7.75" />
    </svg>
  );
}

function BadgeStatus({ status }: { status: string }) {
  const s = status.trim();
  if (s === "Pendente") {
    return (
      <span className="inline-flex items-center gap-1 rounded-full border border-amber-200 bg-amber-50 px-2.5 py-0.5 text-xs font-semibold text-amber-900 shadow-sm">
        <span className="h-1.5 w-1.5 rounded-full bg-amber-500" aria-hidden />
        Pendente
      </span>
    );
  }
  if (s === "Em andamento") {
    return (
      <span className="inline-flex items-center gap-1 rounded-full border border-sky-200 bg-sky-50 px-2.5 py-0.5 text-xs font-semibold text-sky-950 shadow-sm">
        <span className="h-1.5 w-1.5 rounded-full bg-sky-500" aria-hidden />
        Em andamento
      </span>
    );
  }
  if (s === "Concluido") {
    return (
      <span className="inline-flex items-center gap-1 rounded-full border border-emerald-200 bg-emerald-50 px-2.5 py-0.5 text-xs font-semibold text-emerald-900 shadow-sm">
        <span className="h-1.5 w-1.5 rounded-full bg-emerald-500" aria-hidden />
        Concluído
      </span>
    );
  }
  if (s === "Cancelado") {
    return (
      <span className="inline-flex items-center gap-1 rounded-full border border-red-200 bg-red-50 px-2.5 py-0.5 text-xs font-semibold text-red-900 shadow-sm">
        <span className="h-1.5 w-1.5 rounded-full bg-red-500" aria-hidden />
        Cancelado
      </span>
    );
  }
  return (
    <span className="inline-flex rounded-full border border-zinc-200 bg-zinc-50 px-2.5 py-0.5 text-xs font-semibold text-zinc-700">
      {status}
    </span>
  );
}

export default function CompromissosPage() {
  const dataMinimaCompromisso = dataHojeIsoLocal();
  const [compromissos, setCompromissos] = useState<Compromisso[]>([]);
  const [form, setForm] = useState<FormState>(initialForm);
  const [editingId, setEditingId] = useState<number | null>(null);
  const [loading, setLoading] = useState(false);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [participantesDisponiveis, setParticipantesDisponiveis] = useState<ParticipanteCompromisso[]>([]);
  const [confirmarConcluirId, setConfirmarConcluirId] = useState<number | null>(null);
  const [cancelarId, setCancelarId] = useState<number | null>(null);
  const [motivoCancelamento, setMotivoCancelamento] = useState("");
  const [abaLista, setAbaLista] = useState<AbaLista>("pendente");

  const compromissosOrdenados = useMemo(
    () =>
      [...compromissos].sort(
        (a, b) => new Date(a.dataInicio).getTime() - new Date(b.dataInicio).getTime(),
      ),
    [compromissos],
  );

  const compromissosPendentes = useMemo(
    () => compromissosOrdenados.filter((c) => c.status === "Pendente"),
    [compromissosOrdenados],
  );

  const compromissosEmAndamento = useMemo(
    () => compromissosOrdenados.filter((c) => c.status === "Em andamento"),
    [compromissosOrdenados],
  );

  const compromissosAtivos = useMemo(
    () => compromissosOrdenados.filter((c) => c.status !== "Concluido" && c.status !== "Cancelado"),
    [compromissosOrdenados],
  );

  const compromissosConcluidos = useMemo(
    () => compromissosOrdenados.filter((c) => c.status === "Concluido"),
    [compromissosOrdenados],
  );

  const compromissosCancelados = useMemo(
    () => compromissosOrdenados.filter((c) => c.status === "Cancelado"),
    [compromissosOrdenados],
  );

  const listaDaAba = useMemo(() => {
    switch (abaLista) {
      case "pendente":
        return compromissosPendentes;
      case "em_andamento":
        return compromissosEmAndamento;
      case "concluido":
        return compromissosConcluidos;
      case "cancelado":
        return compromissosCancelados;
      default:
        return compromissosPendentes;
    }
  }, [abaLista, compromissosPendentes, compromissosEmAndamento, compromissosConcluidos, compromissosCancelados]);

  async function carregar() {
    setLoading(true);
    setError(null);
    try {
      const [listaCompromissos, listaParticipantes] = await Promise.all([
        listarCompromissos(),
        listarParticipantesCompromissos(),
      ]);
      setCompromissos(listaCompromissos);
      setParticipantesDisponiveis(listaParticipantes);
    } catch (err) {
      setError(getApiErrorMessage(err, "Nao foi possivel carregar compromissos."));
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    void carregar();
  }, []);

  function preencherFormularioParaEdicao(compromisso: Compromisso) {
    const inicio = new Date(compromisso.dataInicio);
    const fim = new Date(compromisso.dataFim);
    const data = formatDateLocal(inicio);
    const horaInicio = `${String(inicio.getHours()).padStart(2, "0")}:${String(inicio.getMinutes()).padStart(2, "0")}`;
    const horaFim = `${String(fim.getHours()).padStart(2, "0")}:${String(fim.getMinutes()).padStart(2, "0")}`;

    setForm({
      titulo: compromisso.titulo,
      descricao: compromisso.descricao ?? "",
      data,
      horaInicio,
      horaFim,
      local: compromisso.local ?? "",
      participantesUsuarioIds: compromisso.participantesUsuarioIds,
    });
    setEditingId(compromisso.id);
    setError(null);
  }

  function cancelarEdicao() {
    setEditingId(null);
    setForm(initialForm);
    setError(null);
  }

  async function onSubmit(e: React.FormEvent) {
    e.preventDefault();
    setSaving(true);
    setError(null);

    try {
      const titulo = form.titulo.trim();
      if (!titulo) {
        setError("Informe o titulo do compromisso.");
        return;
      }

      if (!form.data || !form.horaInicio || !form.horaFim) {
        setError("Informe data e horario de inicio/fim.");
        return;
      }

      if (form.participantesUsuarioIds.length === 0) {
        setError("Selecione ao menos um participante.");
        return;
      }

      if (form.data < dataMinimaCompromisso) {
        setError("Nao e permitido agendar compromisso em data anterior a hoje.");
        return;
      }

      const dataInicio = `${form.data}T${form.horaInicio}:00`;
      const dataFim = `${form.data}T${form.horaFim}:00`;
      if (new Date(dataFim).getTime() <= new Date(dataInicio).getTime()) {
        setError("Horario fim deve ser maior que horario inicio.");
        return;
      }

      if (editingId) {
        await editarCompromisso(editingId, {
          titulo,
          descricao: form.descricao || undefined,
          local: form.local || undefined,
          dataInicio,
          dataFim,
          tipo: "Reuniao",
          prioridade: "Media",
          participantesUsuarioIds: form.participantesUsuarioIds,
        });
      } else {
        await criarCompromisso({
          titulo,
          descricao: form.descricao || undefined,
          local: form.local || undefined,
          dataInicio,
          dataFim,
          tipo: "Reuniao",
          prioridade: "Media",
          participantesUsuarioIds: form.participantesUsuarioIds,
        });
      }

      setEditingId(null);
      setForm(initialForm);
      await carregar();
    } catch (err) {
      setError(getApiErrorMessage(err, editingId ? "Nao foi possivel editar compromisso." : "Nao foi possivel criar compromisso."));
    } finally {
      setSaving(false);
    }
  }

  async function onCancelar(id: number) {
    setCancelarId(id);
    setMotivoCancelamento("");
    setError(null);
  }

  async function confirmarCancelamento() {
    if (!cancelarId) return;
    if (!motivoCancelamento.trim()) {
      setError("Informe o motivo para cancelar o compromisso.");
      return;
    }

    try {
      await cancelarCompromisso(cancelarId, motivoCancelamento.trim());
      if (editingId === cancelarId) {
        cancelarEdicao();
      }
      setCancelarId(null);
      setMotivoCancelamento("");
      await carregar();
    } catch (err) {
      setError(getApiErrorMessage(err, "Nao foi possivel cancelar compromisso."));
    }
  }

  async function onConcluir(id: number) {
    setConfirmarConcluirId(id);
    setError(null);
  }

  async function confirmarConclusao() {
    if (!confirmarConcluirId) return;

    try {
      await editarCompromisso(confirmarConcluirId, { status: "Concluido" });
      if (editingId === confirmarConcluirId) {
        cancelarEdicao();
      }
      setConfirmarConcluirId(null);
      await carregar();
    } catch (err) {
      setError(getApiErrorMessage(err, "Nao foi possivel concluir compromisso."));
    }
  }

  function podeConcluir(c: Compromisso): boolean {
    if (c.status === "Cancelado" || c.status === "Concluido") return false;
    return new Date(c.dataFim).getTime() <= Date.now();
  }

  function toggleParticipante(usuarioId: number) {
    setForm((prev) => {
      const existe = prev.participantesUsuarioIds.includes(usuarioId);
      return {
        ...prev,
        participantesUsuarioIds: existe
          ? prev.participantesUsuarioIds.filter((id) => id !== usuarioId)
          : [...prev.participantesUsuarioIds, usuarioId],
      };
    });
  }

  const inputClass =
    "h-11 w-full rounded-xl border border-zinc-200 bg-white px-3.5 text-sm text-zinc-900 shadow-sm outline-none transition placeholder:text-zinc-400 focus:border-[#1F2A35] focus:ring-2 focus:ring-[#1F2A35]/15";

  const abas: { id: AbaLista; label: string; count: number; hint: string }[] = [
    { id: "pendente", label: "Pendente", count: compromissosPendentes.length, hint: "Aguardando a data da reunião" },
    { id: "em_andamento", label: "Em andamento", count: compromissosEmAndamento.length, hint: "Reunião em curso ou já iniciada" },
    { id: "concluido", label: "Concluídos", count: compromissosConcluidos.length, hint: "Encerrados com sucesso" },
    { id: "cancelado", label: "Cancelados", count: compromissosCancelados.length, hint: "Registro com motivo" },
  ];

  const nenhumCompromissoNoSistema =
    compromissosPendentes.length === 0 &&
    compromissosEmAndamento.length === 0 &&
    compromissosConcluidos.length === 0 &&
    compromissosCancelados.length === 0;

  return (
    <div className="mx-auto flex max-w-6xl flex-col gap-8 pb-10">
      <header className="border-b border-zinc-200/80 pb-6">
        <h1 className="text-2xl font-bold tracking-tight text-zinc-900">Compromissos</h1>
        <p className="mt-2 max-w-3xl text-sm leading-relaxed text-zinc-600">
          Secretaria, Coordenador e Administrador podem criar reuniões. A data deve ser hoje ou futura, em dia de
          funcionamento da escola e sem feriado, recesso ou &quot;sem aula&quot; no calendário geral.
        </p>
      </header>

      {error && (
        <div
          role="alert"
          className="rounded-xl border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-800 shadow-sm"
        >
          {error}
        </div>
      )}

      <section className="rounded-2xl border border-zinc-200/90 bg-white p-6 shadow-sm shadow-zinc-200/40">
        <div className="mb-5 flex flex-col gap-1 border-b border-zinc-100 pb-4 sm:flex-row sm:items-end sm:justify-between">
          <div>
            <h2 className="text-lg font-semibold text-zinc-900">
              {editingId ? "Editar compromisso" : "Novo compromisso"}
            </h2>
            <p className="text-xs font-medium uppercase tracking-wide text-zinc-500">
              Reunião · prioridade média (padrão do sistema)
            </p>
          </div>
          {editingId ? (
            <span className="text-xs font-semibold text-amber-800">Edição #{editingId}</span>
          ) : null}
        </div>

        <form onSubmit={onSubmit} className="flex flex-col gap-5">
          <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
            <div className="sm:col-span-2 lg:col-span-3">
              <label htmlFor="comp-titulo" className="mb-1.5 block text-xs font-bold uppercase tracking-wide text-zinc-500">
                Título da reunião
              </label>
              <input
                id="comp-titulo"
                className={inputClass}
                placeholder="Ex.: Alinhamento pedagógico — fevereiro"
                value={form.titulo}
                onChange={(e) => setForm((f) => ({ ...f, titulo: e.target.value }))}
                required
              />
            </div>
            <div>
              <label htmlFor="comp-data" className="mb-1.5 block text-xs font-bold uppercase tracking-wide text-zinc-500">
                Data
              </label>
              <input
                id="comp-data"
                type="date"
                min={dataMinimaCompromisso}
                className={inputClass}
                value={form.data}
                onChange={(e) => setForm((f) => ({ ...f, data: e.target.value }))}
                required
              />
            </div>
            <div>
              <label htmlFor="comp-ini" className="mb-1.5 block text-xs font-bold uppercase tracking-wide text-zinc-500">
                Início (24h)
              </label>
              <input
                id="comp-ini"
                type="time"
                className={inputClass}
                value={form.horaInicio}
                onChange={(e) => setForm((f) => ({ ...f, horaInicio: e.target.value }))}
                required
              />
            </div>
            <div>
              <label htmlFor="comp-fim" className="mb-1.5 block text-xs font-bold uppercase tracking-wide text-zinc-500">
                Término (24h)
              </label>
              <input
                id="comp-fim"
                type="time"
                className={inputClass}
                value={form.horaFim}
                onChange={(e) => setForm((f) => ({ ...f, horaFim: e.target.value }))}
                required
              />
            </div>
            <div className="sm:col-span-2">
              <label htmlFor="comp-local" className="mb-1.5 block text-xs font-bold uppercase tracking-wide text-zinc-500">
                Local (opcional)
              </label>
              <input
                id="comp-local"
                className={inputClass}
                placeholder="Sala, link ou endereço"
                value={form.local}
                onChange={(e) => setForm((f) => ({ ...f, local: e.target.value }))}
              />
            </div>
          </div>

          <div>
            <label htmlFor="comp-desc" className="mb-1.5 block text-xs font-bold uppercase tracking-wide text-zinc-500">
              Descrição (opcional)
            </label>
            <textarea
              id="comp-desc"
              className="min-h-[96px] w-full rounded-xl border border-zinc-200 bg-white px-3.5 py-3 text-sm text-zinc-900 shadow-sm outline-none transition placeholder:text-zinc-400 focus:border-[#1F2A35] focus:ring-2 focus:ring-[#1F2A35]/15"
              placeholder="Pauta, observações ou links úteis…"
              value={form.descricao}
              onChange={(e) => setForm((f) => ({ ...f, descricao: e.target.value }))}
              rows={3}
            />
          </div>

          <div>
            <p className="mb-2 text-xs font-bold uppercase tracking-wide text-zinc-500">Participantes</p>
            <p className="mb-3 text-xs text-zinc-500">Selecione quem participa da reunião.</p>
            <div className="grid gap-2.5 sm:grid-cols-2">
              {participantesDisponiveis.map((p) => {
                const marcado = form.participantesUsuarioIds.includes(p.id);
                return (
                  <label
                    key={p.id}
                    className={`flex cursor-pointer items-center gap-3 rounded-xl border-2 px-4 py-3.5 transition ${
                      marcado
                        ? "border-[#1F2A35] bg-[#1F2A35]/[0.04] shadow-sm"
                        : "border-zinc-200 bg-zinc-50/50 hover:border-zinc-300 hover:bg-white"
                    }`}
                  >
                    <input
                      type="checkbox"
                      checked={marcado}
                      onChange={() => toggleParticipante(p.id)}
                      className="h-4 w-4 shrink-0 accent-[#1F2A35]"
                    />
                    <div className="min-w-0 flex-1">
                      <span className="block truncate text-sm font-semibold text-zinc-900">{p.nomeCompleto}</span>
                      <span className="mt-0.5 block truncate text-xs font-medium text-zinc-500">{p.perfilNome}</span>
                    </div>
                  </label>
                );
              })}
            </div>
          </div>

          <div className="flex flex-wrap items-center gap-3 border-t border-zinc-100 pt-4">
            <button
              type="submit"
              disabled={saving}
              className="h-11 min-w-[160px] rounded-xl bg-[#1F2A35] px-5 text-sm font-semibold text-white shadow-md shadow-zinc-900/10 transition hover:bg-[#2a3847] disabled:opacity-50"
            >
              {saving ? "Salvando…" : editingId ? "Salvar alterações" : "Criar compromisso"}
            </button>
            {editingId ? (
              <button
                type="button"
                onClick={cancelarEdicao}
                className="h-11 rounded-xl border border-zinc-300 bg-white px-5 text-sm font-semibold text-zinc-700 transition hover:bg-zinc-50"
              >
                Cancelar edição
              </button>
            ) : null}
          </div>
        </form>
      </section>

      <section className="overflow-hidden rounded-2xl border border-zinc-200/90 bg-white shadow-sm shadow-zinc-200/40">
        <div className="border-b border-zinc-100 bg-zinc-50/80 px-5 py-4">
          <h2 className="text-lg font-semibold text-zinc-900">Lista de compromissos</h2>
          <p className="mt-1 text-xs text-zinc-500">
            {compromissosAtivos.length} em aberto (pendente + em andamento) · {compromissos.length} no total
          </p>
        </div>

        <div className="border-b border-zinc-100 bg-white px-2 pt-2 sm:px-4">
          <div
            role="tablist"
            aria-label="Filtrar por status"
            className="flex flex-wrap gap-1.5 pb-2"
          >
            {abas.map((aba) => {
              const ativo = abaLista === aba.id;
              return (
                <button
                  key={aba.id}
                  type="button"
                  role="tab"
                  aria-selected={ativo}
                  onClick={() => setAbaLista(aba.id)}
                  className={`relative flex items-center gap-2 rounded-xl px-4 py-2.5 text-sm font-semibold transition ${
                    ativo
                      ? "bg-[#1F2A35] text-white shadow-md"
                      : "bg-zinc-100 text-zinc-600 hover:bg-zinc-200 hover:text-zinc-900"
                  }`}
                  title={aba.hint}
                >
                  {aba.label}
                  <span
                    className={`rounded-full px-2 py-0.5 text-xs font-bold tabular-nums ${
                      ativo ? "bg-white/20 text-white" : "bg-white text-zinc-800 ring-1 ring-zinc-200"
                    }`}
                  >
                    {aba.count}
                  </span>
                </button>
              );
            })}
          </div>
        </div>

        <div className="min-h-[200px] bg-gradient-to-b from-zinc-50/30 to-white p-4 sm:p-6">
          {loading ? (
            <div className="flex flex-col items-center justify-center gap-3 py-16 text-zinc-500">
              <div className="h-9 w-9 animate-spin rounded-full border-2 border-zinc-200 border-t-[#1F2A35]" aria-hidden />
              <p className="text-sm font-medium">Carregando compromissos…</p>
            </div>
          ) : nenhumCompromissoNoSistema ? (
            <p className="py-12 text-center text-sm text-zinc-500">Nenhum compromisso cadastrado ainda.</p>
          ) : listaDaAba.length === 0 ? (
            <div className="rounded-xl border border-dashed border-zinc-200 bg-zinc-50/50 py-12 text-center">
              <p className="text-sm font-medium text-zinc-600">Nenhum compromisso nesta aba.</p>
              <p className="mt-1 text-xs text-zinc-400">Escolha outro status ou crie um novo compromisso.</p>
            </div>
          ) : (
            <ul className="flex flex-col gap-4">
              {listaDaAba.map((c) => {
                const periodo = formatarPeriodoCompromisso(c.dataInicio, c.dataFim);
                const participantesStr = nomesParticipantes(c.participantesUsuarioIds, participantesDisponiveis);
                const motivoRegistrado = c.status === "Cancelado" ? extrairMotivoCancelamento(c.descricao) : null;
                const mostrarAcoes = c.status !== "Concluido" && c.status !== "Cancelado";

                return (
                  <li
                    key={c.id}
                    className="overflow-hidden rounded-2xl border border-zinc-200/90 bg-white shadow-md shadow-zinc-200/30 transition hover:shadow-lg hover:shadow-zinc-300/25"
                  >
                    <div className="flex flex-col gap-4 p-5 sm:flex-row sm:items-start sm:justify-between sm:gap-6">
                      <div className="min-w-0 flex-1 space-y-3">
                        <div className="flex flex-wrap items-start gap-2">
                          <h3 className="text-base font-bold leading-snug text-zinc-900 sm:text-lg">{c.titulo}</h3>
                          <BadgeStatus status={c.status} />
                        </div>

                        <div className="flex flex-col gap-2 text-sm text-zinc-600">
                          <div className="flex flex-wrap items-center gap-2 text-zinc-800">
                            <IconRelogio className="shrink-0 text-zinc-400" />
                            <span className="font-semibold tabular-nums text-zinc-900">{periodo}</span>
                            <span className="hidden text-zinc-300 sm:inline" aria-hidden>
                              ·
                            </span>
                            <span className="text-xs font-medium uppercase tracking-wide text-zinc-400">24h</span>
                          </div>
                          {c.local?.trim() ? (
                            <div className="flex items-start gap-2">
                              <IconLocal className="mt-0.5 shrink-0 text-zinc-400" />
                              <span className="leading-snug">{c.local.trim()}</span>
                            </div>
                          ) : null}
                          <div className="flex items-start gap-2">
                            <IconPessoas className="mt-0.5 shrink-0 text-zinc-400" />
                            <span className="leading-relaxed text-zinc-700">{participantesStr}</span>
                          </div>
                        </div>

                        {motivoRegistrado ? (
                          <div className="rounded-xl border border-red-100 bg-red-50/60 px-4 py-3 text-sm text-red-950">
                            <span className="font-bold text-red-900">Motivo do cancelamento</span>
                            <p className="mt-1 whitespace-pre-wrap break-words leading-relaxed">{motivoRegistrado}</p>
                          </div>
                        ) : c.status === "Cancelado" ? (
                          <p className="text-xs text-zinc-400">Motivo não registrado neste compromisso.</p>
                        ) : null}
                      </div>

                      {mostrarAcoes ? (
                        <div className="flex shrink-0 flex-col gap-2 sm:w-44">
                          {podeConcluir(c) && (
                            <button
                              type="button"
                              onClick={() => onConcluir(c.id)}
                              className="h-10 w-full rounded-xl border border-emerald-200 bg-emerald-50 text-xs font-bold uppercase tracking-wide text-emerald-900 transition hover:bg-emerald-100"
                            >
                              Concluir reunião
                            </button>
                          )}
                          <button
                            type="button"
                            onClick={() => preencherFormularioParaEdicao(c)}
                            className="h-10 w-full rounded-xl border border-zinc-200 bg-white text-xs font-bold uppercase tracking-wide text-zinc-800 transition hover:bg-zinc-50"
                          >
                            Editar
                          </button>
                          <button
                            type="button"
                            onClick={() => onCancelar(c.id)}
                            className="h-10 w-full rounded-xl border border-red-100 bg-red-50/80 text-xs font-bold uppercase tracking-wide text-red-800 transition hover:bg-red-100"
                          >
                            Cancelar
                          </button>
                        </div>
                      ) : null}
                    </div>
                  </li>
                );
              })}
            </ul>
          )}
        </div>
      </section>

      {confirmarConcluirId !== null && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-zinc-900/40 p-4 backdrop-blur-sm">
          <div
            role="dialog"
            aria-modal="true"
            aria-labelledby="modal-concluir-titulo"
            className="w-full max-w-md rounded-2xl border border-zinc-200 bg-white p-6 shadow-2xl"
          >
            <h3 id="modal-concluir-titulo" className="text-lg font-bold text-zinc-900">
              Concluir reunião
            </h3>
            <p className="mt-2 text-sm leading-relaxed text-zinc-600">
              Tem certeza de que deseja marcar este compromisso como concluído?
            </p>
            <div className="mt-6 flex flex-wrap justify-end gap-2">
              <button
                type="button"
                onClick={() => setConfirmarConcluirId(null)}
                className="h-10 rounded-xl border border-zinc-300 bg-white px-4 text-sm font-semibold text-zinc-800 hover:bg-zinc-50"
              >
                Voltar
              </button>
              <button
                type="button"
                onClick={() => void confirmarConclusao()}
                className="h-10 rounded-xl bg-emerald-600 px-4 text-sm font-semibold text-white shadow-sm hover:bg-emerald-700"
              >
                Confirmar
              </button>
            </div>
          </div>
        </div>
      )}

      {cancelarId !== null && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-zinc-900/40 p-4 backdrop-blur-sm">
          <div
            role="dialog"
            aria-modal="true"
            aria-labelledby="modal-cancelar-titulo"
            className="w-full max-w-lg rounded-2xl border border-zinc-200 bg-white p-6 shadow-2xl"
          >
            <h3 id="modal-cancelar-titulo" className="text-lg font-bold text-zinc-900">
              Cancelar compromisso
            </h3>
            <p className="mt-2 text-sm leading-relaxed text-zinc-600">Informe o motivo do cancelamento.</p>
            <label htmlFor="motivo-cancelamento-compromisso" className="sr-only">
              Motivo do cancelamento
            </label>
            <textarea
              id="motivo-cancelamento-compromisso"
              value={motivoCancelamento}
              onChange={(e) => setMotivoCancelamento(e.target.value)}
              rows={4}
              className={INPUT_CANCEL_MODAL}
              placeholder="Descreva o motivo do cancelamento…"
              autoComplete="off"
            />
            <div className="mt-6 flex flex-wrap justify-end gap-2">
              <button
                type="button"
                onClick={() => {
                  setCancelarId(null);
                  setMotivoCancelamento("");
                }}
                className="h-10 rounded-xl border border-zinc-300 bg-white px-4 text-sm font-semibold text-zinc-800 hover:bg-zinc-50"
              >
                Voltar
              </button>
              <button
                type="button"
                onClick={() => void confirmarCancelamento()}
                className="h-10 rounded-xl bg-red-600 px-4 text-sm font-semibold text-white shadow-sm hover:bg-red-700"
              >
                Confirmar cancelamento
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
