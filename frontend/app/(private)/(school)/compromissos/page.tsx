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
import { getCurrentUser } from "@/lib/api/auth";
import { hasPermission } from "@/lib/permissions";
import type { User } from "@/lib/api/types";

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

function formatDateLocal(date: Date): string {
  const year = date.getFullYear();
  const month = String(date.getMonth() + 1).padStart(2, "0");
  const day = String(date.getDate()).padStart(2, "0");
  return `${year}-${month}-${day}`;
}

export default function CompromissosPage() {
  const [user, setUser] = useState<User | null>(null);
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

  const podeCriar = user ? hasPermission(user, "CRIAR_COMPROMISSO") : false;
  const podeEditar = user ? hasPermission(user, "EDITAR_COMPROMISSO") : false;
  const podeCancelarCompromisso = user ? hasPermission(user, "EXCLUIR_COMPROMISSO") : false;

  const compromissosOrdenados = useMemo(
    () =>
      [...compromissos].sort(
        (a, b) => new Date(a.dataInicio).getTime() - new Date(b.dataInicio).getTime(),
      ),
    [compromissos],
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
    void getCurrentUser().then(setUser).catch(() => setUser(null));
  }, []);

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

  return (
    <div className="flex flex-col gap-6">
      <div>
        <h1 className="text-xl font-semibold text-zinc-900">Compromissos</h1>
        <p className="text-sm text-zinc-500 mt-1">
          Secretaria, Coordenador e Administrador podem criar reunioes e compromissos.
        </p>
      </div>

      {error && (
        <div className="rounded-lg border border-red-200 bg-red-50 text-red-700 px-4 py-3 text-sm">
          {error}
        </div>
      )}

      {(podeCriar || (podeEditar && editingId !== null)) && (
      <form onSubmit={onSubmit} className="grid grid-cols-1 md:grid-cols-3 gap-3 rounded-xl border border-zinc-200 bg-white p-4">
        <input
          className="h-10 rounded-lg border border-zinc-300 px-3 text-sm"
          placeholder="Titulo da reuniao"
          value={form.titulo}
          onChange={(e) => setForm((f) => ({ ...f, titulo: e.target.value }))}
          required
        />
        <input
          type="date"
          className="h-10 rounded-lg border border-zinc-300 px-3 text-sm"
          value={form.data}
          onChange={(e) => setForm((f) => ({ ...f, data: e.target.value }))}
          required
        />
        <input
          className="h-10 rounded-lg border border-zinc-300 px-3 text-sm"
          placeholder="Local"
          value={form.local}
          onChange={(e) => setForm((f) => ({ ...f, local: e.target.value }))}
        />
        <input
          type="time"
          className="h-10 rounded-lg border border-zinc-300 px-3 text-sm"
          value={form.horaInicio}
          onChange={(e) => setForm((f) => ({ ...f, horaInicio: e.target.value }))}
          required
        />
        <input
          type="time"
          className="h-10 rounded-lg border border-zinc-300 px-3 text-sm"
          value={form.horaFim}
          onChange={(e) => setForm((f) => ({ ...f, horaFim: e.target.value }))}
          required
        />
        <button
          type="submit"
          disabled={saving}
          className="h-10 rounded-lg bg-[#1F2A35] text-white text-sm font-medium disabled:opacity-60"
        >
          {saving ? "Salvando..." : editingId ? "Salvar edicao" : "Criar compromisso"}
        </button>
        <textarea
          className="md:col-span-3 rounded-lg border border-zinc-300 px-3 py-2 text-sm"
          placeholder="Descricao (opcional)"
          value={form.descricao}
          onChange={(e) => setForm((f) => ({ ...f, descricao: e.target.value }))}
          rows={3}
        />
        <div className="md:col-span-3">
          <p className="text-xs font-semibold text-zinc-600 mb-2">Participantes</p>
          <div className="grid grid-cols-1 md:grid-cols-2 gap-2">
            {participantesDisponiveis.map((p) => {
              const marcado = form.participantesUsuarioIds.includes(p.id);
              return (
                <label
                  key={p.id}
                  className={`flex items-center justify-between border rounded-lg px-3 py-2 text-sm cursor-pointer ${
                    marcado ? "border-[#1F2A35] bg-zinc-50" : "border-zinc-300"
                  }`}
                >
                  <span>{p.nomeCompleto}</span>
                  <span className="text-xs text-zinc-500">{p.perfilNome}</span>
                  <input
                    type="checkbox"
                    checked={marcado}
                    onChange={() => toggleParticipante(p.id)}
                    className="ml-3"
                  />
                </label>
              );
            })}
          </div>
        </div>
        {editingId && (
          <button
            type="button"
            onClick={cancelarEdicao}
            className="md:col-span-3 h-9 rounded-lg border border-zinc-300 text-zinc-700 text-sm font-medium hover:bg-zinc-50"
          >
            Cancelar edicao
          </button>
        )}
      </form>
      )}

      <div className="rounded-xl border border-zinc-200 bg-white overflow-hidden">
        <div className="px-4 py-3 border-b border-zinc-100 text-sm font-semibold text-zinc-700">
          Lista de compromissos
        </div>
        {loading ? (
          <p className="px-4 py-4 text-sm text-zinc-500">Carregando...</p>
        ) : compromissosAtivos.length === 0 && compromissosConcluidos.length === 0 && compromissosCancelados.length === 0 ? (
          <p className="px-4 py-4 text-sm text-zinc-500">Nenhum compromisso cadastrado.</p>
        ) : (
          <div>
            <div className="px-4 py-2 text-xs font-semibold uppercase tracking-wide text-zinc-500 bg-zinc-50 border-b border-zinc-100">
              Ativos
            </div>
            <div className="divide-y divide-zinc-100 border-b border-zinc-100">
              {compromissosAtivos.length === 0 ? (
                <p className="px-4 py-4 text-sm text-zinc-500">Nenhum compromisso ativo.</p>
              ) : (
                compromissosAtivos.map((c) => (
                  <div key={c.id} className="px-4 py-3 flex items-center justify-between gap-3">
                    <div>
                      <p className="font-medium text-zinc-900">{c.titulo}</p>
                      <p className="text-xs text-zinc-500">
                        {new Date(c.dataInicio).toLocaleString()} - {new Date(c.dataFim).toLocaleTimeString()}
                        {c.local ? ` | ${c.local}` : ""}
                      </p>
                    </div>
                    <div className="flex items-center gap-2">
                      {podeConcluir(c) && podeEditar && (
                        <button
                          onClick={() => onConcluir(c.id)}
                          className="h-8 px-3 rounded border border-emerald-300 text-xs font-medium text-emerald-700 hover:bg-emerald-50"
                        >
                          Concluir reuniao
                        </button>
                      )}
                      {podeEditar && (
                        <button
                          onClick={() => preencherFormularioParaEdicao(c)}
                          className="h-8 px-3 rounded border border-zinc-300 text-xs font-medium text-zinc-700 hover:bg-zinc-100"
                        >
                          Editar
                        </button>
                      )}
                      {podeCancelarCompromisso && (
                        <button
                          onClick={() => onCancelar(c.id)}
                          className="h-8 px-3 rounded border border-zinc-300 text-xs font-medium text-zinc-700 hover:bg-zinc-100"
                        >
                          Cancelar
                        </button>
                      )}
                    </div>
                  </div>
                ))
              )}
            </div>

            <div className="px-4 py-2 text-xs font-semibold uppercase tracking-wide text-zinc-500 bg-zinc-50 border-b border-zinc-100">
              Concluidos
            </div>
            <div className="divide-y divide-zinc-100">
              {compromissosConcluidos.length === 0 ? (
                <p className="px-4 py-4 text-sm text-zinc-500">Nenhum compromisso concluido.</p>
              ) : (
                compromissosConcluidos.map((c) => (
                  <div key={c.id} className="px-4 py-3 flex items-center justify-between gap-3">
                    <div>
                      <p className="font-medium text-zinc-700">{c.titulo}</p>
                      <p className="text-xs text-zinc-500">
                        {new Date(c.dataInicio).toLocaleString()} - {new Date(c.dataFim).toLocaleTimeString()}
                        {c.local ? ` | ${c.local}` : ""}
                      </p>
                    </div>
                    <span className="text-xs font-semibold text-emerald-700 bg-emerald-50 px-2 py-1 rounded">
                      Concluido
                    </span>
                  </div>
                ))
              )}
            </div>

            <div className="px-4 py-2 text-xs font-semibold uppercase tracking-wide text-zinc-500 bg-zinc-50 border-b border-zinc-100">
              Cancelados
            </div>
            <div className="divide-y divide-zinc-100">
              {compromissosCancelados.length === 0 ? (
                <p className="px-4 py-4 text-sm text-zinc-500">Nenhum compromisso cancelado.</p>
              ) : (
                compromissosCancelados.map((c) => (
                  <div key={c.id} className="px-4 py-3 flex items-center justify-between gap-3">
                    <div>
                      <p className="font-medium text-zinc-700">{c.titulo}</p>
                      <p className="text-xs text-zinc-500">
                        {new Date(c.dataInicio).toLocaleString()} - {new Date(c.dataFim).toLocaleTimeString()}
                        {c.local ? ` | ${c.local}` : ""}
                      </p>
                    </div>
                    <span className="text-xs font-semibold text-red-700 bg-red-50 px-2 py-1 rounded">
                      Cancelado
                    </span>
                  </div>
                ))
              )}
            </div>
          </div>
        )}
      </div>

      {confirmarConcluirId !== null && (
        <div className="fixed inset-0 bg-black/35 flex items-center justify-center z-50">
          <div className="w-full max-w-md bg-white rounded-xl shadow-xl border border-zinc-200 p-5">
            <h3 className="text-base font-semibold text-zinc-900">Concluir reuniao</h3>
            <p className="text-sm text-zinc-600 mt-2">
              Tem certeza que deseja marcar este compromisso como concluido?
            </p>
            <div className="mt-4 flex justify-end gap-2">
              <button
                onClick={() => setConfirmarConcluirId(null)}
                className="h-9 px-4 rounded-lg border border-zinc-300 text-sm text-zinc-700 hover:bg-zinc-50"
              >
                Voltar
              </button>
              <button
                onClick={confirmarConclusao}
                className="h-9 px-4 rounded-lg bg-emerald-600 text-white text-sm font-medium hover:bg-emerald-700"
              >
                Confirmar
              </button>
            </div>
          </div>
        </div>
      )}

      {cancelarId !== null && (
        <div className="fixed inset-0 bg-black/35 flex items-center justify-center z-50">
          <div className="w-full max-w-lg bg-white rounded-xl shadow-xl border border-zinc-200 p-5">
            <h3 className="text-base font-semibold text-zinc-900">Cancelar compromisso</h3>
            <p className="text-sm text-zinc-600 mt-2">
              Informe o motivo do cancelamento.
            </p>
            <textarea
              value={motivoCancelamento}
              onChange={(e) => setMotivoCancelamento(e.target.value)}
              rows={4}
              className="mt-3 w-full rounded-lg border border-zinc-300 px-3 py-2 text-sm"
              placeholder="Motivo do cancelamento"
            />
            <div className="mt-4 flex justify-end gap-2">
              <button
                onClick={() => {
                  setCancelarId(null);
                  setMotivoCancelamento("");
                }}
                className="h-9 px-4 rounded-lg border border-zinc-300 text-sm text-zinc-700 hover:bg-zinc-50"
              >
                Voltar
              </button>
              <button
                onClick={confirmarCancelamento}
                className="h-9 px-4 rounded-lg bg-red-600 text-white text-sm font-medium hover:bg-red-700"
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
