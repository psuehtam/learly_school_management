"use client";

import { useEffect, useState } from "react";
import Link from "next/link";
import { useParams } from "next/navigation";
import {
  atualizarOcorrenciaAluno,
  criarOcorrenciaAluno,
  justificarFaltaAluno,
  listarDocumentosAluno,
  listarFaltasAluno,
  listarOcorrenciasAluno,
  type AlunoDocumento,
  type AlunoFalta,
  type AlunoOcorrencia,
  type SalvarOcorrenciaPayload,
} from "@/lib/api/aluno-perfil";
import { buscarAluno, type AlunoDetalhe } from "@/lib/api/alunos";
import { ApiError } from "@/lib/api/client";
import { listarMatriculas, type MatriculaListItem, type MatriculaStatus } from "@/lib/api/matriculas";

// ─── Tipos ────────────────────────────────────────────────────────────────────
interface HistoricoMatricula {
  id: number;
  periodoLetivo: string;
  unidade: string;
  turma: string;
  situacao: "ATIVO" | "CANCELADO" | "CONCLUÍDO" | "TRANCADO" | "EM ESPERA";
}

type Ocorrencia = AlunoOcorrencia;
type Documento = AlunoDocumento;
type Falta = AlunoFalta;

interface AlunoInfo {
  id: string | number;
  nome: string;
  cpf: string;
  naturalidade: string;
  dataIngresso: string;
  dataNascimento: string;
  telefoneAluno: string;
  rg: string;
  endereco: string;
  telefoneResponsavel: string;
  nomeResponsavel: string;
  cpfResponsavel: string;
}

function formatarDataBr(iso: string): string {
  if (!iso) return "—";
  if (iso.includes("/")) return iso;
  const parte = iso.split("T")[0];
  const [y, m, d] = parte.split("-");
  if (!y || !m || !d) return iso;
  return `${d}/${m}/${y}`;
}

function montarEndereco(d: AlunoDetalhe): string {
  const linha1 = `${d.tipoLogradouro} ${d.logradouro}, ${d.numero}`.trim();
  const partes = [linha1, d.complemento, d.bairro, d.municipio].filter((p) => p && String(p).trim());
  return partes.join(" - ").toUpperCase() || "—";
}

function alunoDetalheParaInfo(d: AlunoDetalhe): AlunoInfo {
  const naturalidade = [d.naturalidadeCidade, d.naturalidadeEstado].filter(Boolean).join(" / ");
  const rgPartes = [d.rgNumero, d.rgOrgao].filter(Boolean);
  const rgExp = d.rgExpedicao ? formatarDataBr(d.rgExpedicao) : "";
  const rg = rgPartes.length ? `${rgPartes.join(" - ")}${rgExp ? ` (${rgExp})` : ""}` : "—";
  const nomeResp = [d.responsavelNome, d.responsavelSobrenome].filter(Boolean).join(" ").trim();

  return {
    id: d.id,
    nome: `${d.nome} ${d.sobrenome}`.trim().toUpperCase(),
    cpf: d.cpf?.trim() || "—",
    naturalidade: naturalidade ? naturalidade.toUpperCase() : "—",
    dataIngresso: formatarDataBr(d.dataIngresso),
    dataNascimento: formatarDataBr(d.dataNascimento),
    telefoneAluno: d.telefoneAluno?.trim() || "—",
    rg: rg.toUpperCase(),
    endereco: montarEndereco(d),
    telefoneResponsavel: d.telefoneResponsavel?.trim() || "—",
    nomeResponsavel: nomeResp ? nomeResp.toUpperCase() : "—",
    cpfResponsavel: d.responsavelCpf?.trim() || "—",
  };
}

function statusMatriculaParaSituacao(status: MatriculaStatus): HistoricoMatricula["situacao"] {
  switch (status) {
    case "Ativo":
      return "ATIVO";
    case "Cancelado":
      return "CANCELADO";
    case "Concluido":
      return "CONCLUÍDO";
    case "Trancado":
      return "TRANCADO";
    case "Em Espera":
    default:
      return "EM ESPERA";
  }
}

function matriculaParaHistorico(m: MatriculaListItem, unidade: string): HistoricoMatricula {
  const turma = m.turmaNome?.trim() || "—";
  const periodo = turma !== "—" ? turma : `Matrícula ${formatarDataBr(m.dataMatricula)}`;
  return {
    id: m.id,
    periodoLetivo: periodo,
    unidade,
    turma,
    situacao: statusMatriculaParaSituacao(m.status),
  };
}

function ocorrenciaParaPayload(dados: Ocorrencia): SalvarOcorrenciaPayload {
  return {
    data: dados.data,
    hora: dados.hora,
    tipo: dados.tipo,
    descricao: dados.descricao,
    resolucao: dados.resolucao || undefined,
    aulaVinculada: dados.aulaVinculada || undefined,
  };
}

// ─── Componentes Auxiliares ───────────────────────────────────────────────────
function LabelValue({ label, value, whatsapp }: { label: string, value: string, whatsapp?: boolean }) {
  return (
    <div className="flex flex-col gap-0.5">
      <span className="text-xs font-semibold text-zinc-500 uppercase tracking-wide">{label}</span>
      <div className="flex items-center gap-2">
        <span className="text-sm font-medium text-zinc-900">{value || "—"}</span>
        {whatsapp && value && value !== "—" && (
          <svg width="16" height="16" viewBox="0 0 24 24" fill="#25D366" stroke="none">
            <path d="M12.031 0C5.385 0 0 5.385 0 12.031c0 2.656.844 5.12 2.312 7.156L.657 24l5.03-1.625a11.93 11.93 0 0 0 6.344 1.782c6.646 0 12.03-5.385 12.03-12.031S18.677 0 12.031 0zm0 22.156a9.95 9.95 0 0 1-5.187-1.468l-.375-.219-3.75 1.188 1.218-3.625-.25-.375A9.957 9.957 0 0 1 2.062 12.03C2.062 6.53 6.53 2.062 12.03 2.062S22 6.53 22 12.03c0 5.5-4.469 9.968-9.969 9.968h-.002zm5.406-7.375c-.281-.156-1.75-.875-2.031-.968-.281-.094-.469-.156-.656.156-.188.281-.781.968-.969 1.156-.187.219-.375.219-.656.094-.281-.156-1.25-.469-2.375-1.469-.875-.781-1.469-1.75-1.625-2.031-.156-.281-.031-.437.094-.562.125-.125.281-.344.437-.531.156-.187.219-.312.312-.531.094-.219.063-.406 0-.531-.063-.125-.656-1.593-.875-2.187-.219-.562-.437-.469-.625-.469-.156 0-.344-.031-.531-.031-.188 0-.5.063-.781.375-.281.281-1.062 1.031-1.062 2.5s1.094 2.875 1.25 3.094c.156.219 2.125 3.187 5.125 4.469.719.312 1.281.5 1.719.656.719.219 1.375.187 1.875.094.594-.094 1.75-.719 2-1.406.25-.687.25-1.281.188-1.406-.063-.125-.25-.219-.531-.344z"/>
          </svg>
        )}
      </div>
    </div>
  );
}

// ─── Modais (Mantidos exatamente iguais) ──────────────────────────────────────
function createDefaultOcorrencia(): Ocorrencia {
  const d = new Date();
  const pad = (n: number) => String(n).padStart(2, "0");
  return {
    id: d.getTime(),
    data: d.toISOString().split("T")[0],
    hora: `${pad(d.getHours())}:${pad(d.getMinutes())}`,
    tipo: "Acadêmica",
    descricao: "",
    resolucao: "",
    aulaVinculada: "",
    autor: "Você (Usuário Logado)",
  };
}

function ModalEditarHistorico({ matricula, onClose, onSave }: { matricula: HistoricoMatricula, onClose: () => void, onSave: (m: HistoricoMatricula) => void }) {
  const [form, setForm] = useState<HistoricoMatricula>(matricula);
  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40">
      <div className="bg-white rounded-xl shadow-xl w-full max-w-md mx-4 flex flex-col">
        <div className="flex items-center justify-between px-6 py-4 border-b border-zinc-200">
          <div><h2 className="text-base font-semibold text-zinc-900">Editar Histórico</h2><p className="text-xs text-zinc-500 mt-0.5">Matrícula #{matricula.id}</p></div>
          <button onClick={onClose} className="text-zinc-400 hover:text-zinc-600 transition-colors"><svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2"><line x1="18" y1="6" x2="6" y2="18"/><line x1="6" y1="6" x2="18" y2="18"/></svg></button>
        </div>
        <div className="p-6 flex flex-col gap-4">
          <div className="flex flex-col gap-1.5"><label className="text-sm font-medium text-zinc-700">Período Letivo</label><input type="text" value={form.periodoLetivo} onChange={(e) => setForm({ ...form, periodoLetivo: e.target.value })} className="h-10 border border-zinc-300 rounded-lg px-3 text-sm outline-none focus:border-[#1F2A35] transition" /></div>
          <div className="flex flex-col gap-1.5"><label className="text-sm font-medium text-zinc-700">Unidade</label><input type="text" value={form.unidade} onChange={(e) => setForm({ ...form, unidade: e.target.value })} className="h-10 border border-zinc-300 rounded-lg px-3 text-sm outline-none focus:border-[#1F2A35] transition" /></div>
          <div className="flex flex-col gap-1.5"><label className="text-sm font-medium text-zinc-700">Turma</label><input type="text" value={form.turma} onChange={(e) => setForm({ ...form, turma: e.target.value })} className="h-10 border border-zinc-300 rounded-lg px-3 text-sm outline-none focus:border-[#1F2A35] transition" /></div>
          <div className="flex flex-col gap-1.5">
            <label className="text-sm font-medium text-zinc-700">Situação</label>
            <select value={form.situacao} onChange={(e) => setForm({ ...form, situacao: e.target.value as HistoricoMatricula["situacao"] })} className="h-10 border border-zinc-300 rounded-lg px-3 text-sm outline-none focus:border-[#1F2A35] transition bg-white">
              <option value="ATIVO">ATIVO</option><option value="CANCELADO">CANCELADO</option><option value="CONCLUÍDO">CONCLUÍDO</option><option value="TRANCADO">TRANCADO</option><option value="EM ESPERA">EM ESPERA</option>
            </select>
          </div>
        </div>
        <div className="flex justify-end gap-3 px-6 py-4 border-t border-zinc-200">
          <button onClick={onClose} className="h-9 px-4 text-sm font-medium text-zinc-600 border border-zinc-300 rounded-lg hover:bg-zinc-50 transition-colors">Cancelar</button>
          <button onClick={() => { onSave(form); onClose(); }} className="h-9 px-4 text-sm font-medium text-white bg-[#1F2A35] rounded-lg hover:bg-[#2d3d4d] transition-colors">Salvar Alterações</button>
        </div>
      </div>
    </div>
  );
}

function ModalOcorrencia({
  ocorrencia,
  onClose,
  onSave,
}: {
  ocorrencia?: Ocorrencia | null;
  onClose: () => void;
  onSave: (o: Ocorrencia) => Promise<void>;
}) {
  const [form, setForm] = useState<Ocorrencia>(() => ocorrencia ?? createDefaultOcorrencia());
  const [salvandoModal, setSalvandoModal] = useState(false);

  async function handleSubmit() {
    setSalvandoModal(true);
    try {
      await onSave(form);
      onClose();
    } catch {
      // erro exibido na página
    } finally {
      setSalvandoModal(false);
    }
  }
  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40">
      <div className="bg-white rounded-xl shadow-xl w-full max-w-2xl mx-4 flex flex-col max-h-[95vh]">
        <div className="flex items-center justify-between px-6 py-4 border-b border-zinc-200">
          <div><h2 className="text-base font-semibold text-zinc-900">{ocorrencia ? "Editar Ocorrência" : "Registrar Nova Ocorrência"}</h2><p className="text-xs text-zinc-500 mt-0.5">As ocorrências são permanentes e mantêm histórico de auditoria.</p></div>
          <button onClick={onClose} className="text-zinc-400 hover:text-zinc-600 transition-colors"><svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2"><line x1="18" y1="6" x2="6" y2="18"/><line x1="6" y1="6" x2="18" y2="18"/></svg></button>
        </div>
        <div className="p-6 flex flex-col gap-4 overflow-y-auto">
          <div className="grid grid-cols-2 md:grid-cols-3 gap-4">
            <div className="flex flex-col gap-1.5"><label className="text-sm font-medium text-zinc-700">Data *</label><input type="date" value={form.data} onChange={(e) => setForm({ ...form, data: e.target.value })} className="h-10 border border-zinc-300 rounded-lg px-3 text-sm outline-none focus:border-[#1F2A35] transition" /></div>
            <div className="flex flex-col gap-1.5"><label className="text-sm font-medium text-zinc-700">Hora *</label><input type="time" value={form.hora} onChange={(e) => setForm({ ...form, hora: e.target.value })} className="h-10 border border-zinc-300 rounded-lg px-3 text-sm outline-none focus:border-[#1F2A35] transition" /></div>
            <div className="flex flex-col gap-1.5 col-span-2 md:col-span-1"><label className="text-sm font-medium text-zinc-700">Tipo *</label><select value={form.tipo} onChange={(e) => setForm({ ...form, tipo: e.target.value as Ocorrencia["tipo"] })} className="h-10 border border-zinc-300 rounded-lg px-3 text-sm outline-none focus:border-[#1F2A35] transition bg-white"><option value="Acadêmica">Acadêmica</option><option value="Administrativa">Administrativa</option></select></div>
          </div>
          <div className="flex flex-col gap-1.5"><label className="text-sm font-medium text-zinc-700">Descrição da Ocorrência *</label><textarea value={form.descricao} onChange={(e) => setForm({ ...form, descricao: e.target.value })} placeholder="Descreva o que aconteceu de forma clara e objetiva..." className="h-24 border border-zinc-300 rounded-lg p-3 text-sm outline-none focus:border-[#1F2A35] transition resize-none" /></div>
          <div className="flex flex-col gap-1.5"><label className="text-sm font-medium text-zinc-700">Resolução / Encaminhamento (Opcional)</label><textarea value={form.resolucao} onChange={(e) => setForm({ ...form, resolucao: e.target.value })} placeholder="Qual medida foi adotada? Houve orientação?" className="h-20 border border-zinc-300 rounded-lg p-3 text-sm outline-none focus:border-[#1F2A35] transition resize-none" /></div>
          <div className="flex flex-col gap-1.5"><label className="text-sm font-medium text-zinc-700">Aula Vinculada (Opcional)</label><select value={form.aulaVinculada} onChange={(e) => setForm({ ...form, aulaVinculada: e.target.value })} className="h-10 border border-zinc-300 rounded-lg px-3 text-sm outline-none focus:border-[#1F2A35] transition bg-white"><option value="">Nenhuma aula vinculada</option><option value="Lesson 1 - Introdução">Lesson 1 - Introdução</option><option value="Lesson 2 - Simple Present">Lesson 2 - Simple Present</option><option value="Lesson 3 - Simple Past">Lesson 3 - Simple Past</option></select></div>
        </div>
        <div className="flex justify-end gap-3 px-6 py-4 border-t border-zinc-200">
          <button onClick={onClose} className="h-9 px-4 text-sm font-medium text-zinc-600 border border-zinc-300 rounded-lg hover:bg-zinc-50 transition-colors">Cancelar</button>
          <button disabled={!form.data || !form.hora || !form.descricao || salvandoModal} onClick={() => void handleSubmit()} className="h-9 px-4 text-sm font-medium text-white bg-[#1F2A35] rounded-lg hover:bg-[#2d3d4d] transition-colors disabled:opacity-50 disabled:cursor-not-allowed">{salvandoModal ? "Salvando..." : ocorrencia ? "Salvar Alterações" : "Registrar Ocorrência"}</button>
        </div>
      </div>
    </div>
  );
}

function ModalAnexarDocumento({ onClose, onSave }: { onClose: () => void, onSave: () => Promise<void> }) {
  const [nome, setNome] = useState("");
  const [tipo, setTipo] = useState("");
  const [arquivo, setArquivo] = useState<File | null>(null);
  const [enviando, setEnviando] = useState(false);

  async function handleSave() {
    if (!nome || !tipo || !arquivo) return;
    setEnviando(true);
    try {
      await onSave();
      onClose();
    } finally {
      setEnviando(false);
    }
  }
  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40">
      <div className="bg-white rounded-xl shadow-xl w-full max-w-lg mx-4 flex flex-col">
        <div className="flex items-center justify-between px-6 py-4 border-b border-zinc-200">
          <div><h2 className="text-base font-semibold text-zinc-900">Anexar Novo Documento</h2><p className="text-xs text-zinc-500 mt-0.5">Formatos suportados: PDF, JPG, PNG.</p></div>
          <button onClick={onClose} className="text-zinc-400 hover:text-zinc-600 transition-colors"><svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2"><line x1="18" y1="6" x2="6" y2="18"/><line x1="6" y1="6" x2="18" y2="18"/></svg></button>
        </div>
        <div className="p-6 flex flex-col gap-4">
          <div className="flex flex-col gap-1.5"><label className="text-sm font-medium text-zinc-700">Arquivo *</label><input type="file" accept=".pdf, image/png, image/jpeg" onChange={(e) => setArquivo(e.target.files?.[0] || null)} className="block w-full text-sm text-zinc-500 file:mr-4 file:py-2 file:px-4 file:rounded-lg file:border-0 file:text-sm file:font-semibold file:bg-[#1F2A35]/10 file:text-[#1F2A35] hover:file:bg-[#1F2A35]/20 transition-colors border border-zinc-300 rounded-lg p-1.5" /></div>
          <div className="flex flex-col gap-1.5"><label className="text-sm font-medium text-zinc-700">Nome de Identificação *</label><input type="text" placeholder="Ex: Contrato Assinado 2026" value={nome} onChange={(e) => setNome(e.target.value)} className="h-10 border border-zinc-300 rounded-lg px-3 text-sm outline-none focus:border-[#1F2A35] transition" /></div>
          <div className="flex flex-col gap-1.5"><label className="text-sm font-medium text-zinc-700">Tipo de Documento *</label><select value={tipo} onChange={(e) => setTipo(e.target.value)} className="h-10 border border-zinc-300 rounded-lg px-3 text-sm outline-none focus:border-[#1F2A35] transition bg-white"><option value="">Selecione...</option><option value="Contrato (PDF)">Contrato (PDF)</option><option value="Documento Pessoal">Documento Pessoal (RG, CPF)</option><option value="Comprovante de Endereço">Comprovante de Endereço</option><option value="Conversa WhatsApp">Conversa WhatsApp (Print)</option><option value="Outros">Outros</option></select></div>
        </div>
        <div className="flex justify-end gap-3 px-6 py-4 border-t border-zinc-200">
          <button onClick={onClose} className="h-9 px-4 text-sm font-medium text-zinc-600 border border-zinc-300 rounded-lg hover:bg-zinc-50 transition-colors">Cancelar</button>
          <button disabled={!nome || !tipo || !arquivo || enviando} onClick={() => void handleSave()} className="h-9 px-4 text-sm font-medium text-white bg-[#1F2A35] rounded-lg hover:bg-[#2d3d4d] transition-colors disabled:opacity-50 disabled:cursor-not-allowed">{enviando ? "Enviando..." : "Fazer Upload"}</button>
        </div>
      </div>
    </div>
  );
}

function ModalJustificativaFalta({
  falta,
  onClose,
  onSave,
}: {
  falta: Falta;
  onClose: () => void;
  onSave: (id: number, motivo: string) => Promise<void>;
}) {
  const [motivo, setMotivo] = useState(falta.motivo || "");
  const [salvandoModal, setSalvandoModal] = useState(false);
  const isJustificada = falta.justificada;

  async function handleJustificar() {
    setSalvandoModal(true);
    try {
      await onSave(falta.id, motivo);
      onClose();
    } catch {
      // erro exibido na página
    } finally {
      setSalvandoModal(false);
    }
  }
  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40">
      <div className="bg-white rounded-xl shadow-xl w-full max-w-lg mx-4 flex flex-col">
        <div className="flex items-center justify-between px-6 py-4 border-b border-zinc-200">
          <div><h2 className="text-base font-semibold text-zinc-900">{isJustificada ? "Detalhes da Justificativa" : "Justificar Falta"}</h2><p className="text-xs text-zinc-500 mt-0.5">Aula: {falta.aula} - Data: {falta.data}</p></div>
          <button onClick={onClose} className="text-zinc-400 hover:text-zinc-600 transition-colors"><svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2"><line x1="18" y1="6" x2="6" y2="18"/><line x1="6" y1="6" x2="18" y2="18"/></svg></button>
        </div>
        <div className="p-6 flex flex-col gap-4">
          {isJustificada ? (
            <div className="flex flex-col gap-4">
              <div className="bg-green-50 border border-green-200 rounded-lg p-4"><span className="text-xs font-semibold text-green-700 uppercase tracking-wide block mb-2">Motivo da Falta</span><p className="text-sm text-zinc-800 leading-relaxed">{falta.motivo}</p></div>
              <div className="flex flex-col gap-1 mt-2"><span className="text-[10px] text-zinc-400 uppercase font-semibold">Justificado por:</span><span className="text-sm font-medium text-zinc-700">{falta.autorJustificativa} em {falta.dataJustificativa}</span></div>
            </div>
          ) : (
            <>
              <div className="bg-amber-50 border border-amber-200 rounded-lg p-4 text-sm text-amber-800">Ao justificar esta falta, ela não será mais contabilizada no limite de reprovação por faltas do aluno nesta etapa.</div>
              <div className="flex flex-col gap-1.5 mt-2"><label className="text-sm font-medium text-zinc-700">Motivo / Observação *</label><textarea value={motivo} onChange={(e) => setMotivo(e.target.value)} placeholder="Ex: Atestado médico anexado, problemas familiares..." className="h-28 border border-zinc-300 rounded-lg p-3 text-sm outline-none focus:border-[#1F2A35] transition resize-none" /></div>
              <div className="flex flex-col gap-1.5 mt-2"><label className="text-sm font-medium text-zinc-700">Anexar Comprovante (Opcional)</label><input type="file" className="block w-full text-sm text-zinc-500 file:mr-4 file:py-2 file:px-4 file:rounded-lg file:border-0 file:text-sm file:font-semibold file:bg-zinc-100 file:text-zinc-700 hover:file:bg-zinc-200 transition-colors border border-zinc-300 rounded-lg p-1.5"/></div>
            </>
          )}
        </div>
        <div className="flex justify-end gap-3 px-6 py-4 border-t border-zinc-200">
          <button onClick={onClose} className="h-9 px-4 text-sm font-medium text-zinc-600 border border-zinc-300 rounded-lg hover:bg-zinc-50 transition-colors">{isJustificada ? "Fechar" : "Cancelar"}</button>
          {!isJustificada && (<button disabled={!motivo.trim() || salvandoModal} onClick={() => void handleJustificar()} className="h-9 px-4 text-sm font-medium text-white bg-[#1F2A35] rounded-lg hover:bg-[#2d3d4d] transition-colors disabled:opacity-50 disabled:cursor-not-allowed">{salvandoModal ? "Salvando..." : "Salvar Justificativa"}</button>)}
        </div>
      </div>
    </div>
  );
}

// ─── Página Principal ─────────────────────────────────────────────────────────
export default function PerfilAlunoPage() {
  const params = useParams();
  const alunoIdParam = params.id as string;
  const alunoIdNumerico = Number.parseInt(alunoIdParam, 10);

  const [carregando, setCarregando] = useState(true);
  const [erro, setErro] = useState<string | null>(null);
  const [alunoSelecionado, setAlunoSelecionado] = useState<AlunoInfo>({
    id: "N/A",
    nome: "Carregando...",
    cpf: "—",
    naturalidade: "—",
    dataIngresso: "—",
    dataNascimento: "—",
    telefoneAluno: "—",
    rg: "—",
    endereco: "—",
    telefoneResponsavel: "—",
    nomeResponsavel: "—",
    cpfResponsavel: "—",
  });

  const [abaAtual, setAbaAtual] = useState("Histórico escolar");

  const [historico, setHistorico] = useState<HistoricoMatricula[]>([]);
  const [ocorrencias, setOcorrencias] = useState<Ocorrencia[]>([]);
  const [documentos, setDocumentos] = useState<Documento[]>([]);
  const [faltas, setFaltas] = useState<Falta[]>([]);
  const [carregandoAbas, setCarregandoAbas] = useState(false);
  const [erroAbas, setErroAbas] = useState<string | null>(null);

  // Modal Control
  const [matriculaEditando, setMatriculaEditando] = useState<HistoricoMatricula | null>(null);
  const [ocorrenciaModal, setOcorrenciaModal] = useState<{ isOpen: boolean, oco: Ocorrencia | null }>({ isOpen: false, oco: null });
  const [documentoModalAberto, setDocumentoModalAberto] = useState(false);
  const [faltaModalAberto, setFaltaModalAberto] = useState<Falta | null>(null);

  useEffect(() => {
    if (!Number.isFinite(alunoIdNumerico) || alunoIdNumerico <= 0) {
      setErro("ID de aluno inválido.");
      setAlunoSelecionado({
        id: "N/A",
        nome: "Aluno não encontrado",
        cpf: "—",
        naturalidade: "—",
        dataIngresso: "—",
        dataNascimento: "—",
        telefoneAluno: "—",
        rg: "—",
        endereco: "—",
        telefoneResponsavel: "—",
        nomeResponsavel: "—",
        cpfResponsavel: "—",
      });
      setHistorico([]);
      setOcorrencias([]);
      setDocumentos([]);
      setFaltas([]);
      setCarregando(false);
      return;
    }

    let cancelado = false;

    async function carregar() {
      setCarregando(true);
      setCarregandoAbas(true);
      setErro(null);
      setErroAbas(null);
      try {
        const [detalhe, matriculas, ocorrenciasLista, documentosLista, faltasLista] = await Promise.all([
          buscarAluno(alunoIdNumerico),
          listarMatriculas({ alunoId: alunoIdNumerico }),
          listarOcorrenciasAluno(alunoIdNumerico),
          listarDocumentosAluno(alunoIdNumerico),
          listarFaltasAluno(alunoIdNumerico),
        ]);
        if (cancelado) return;

        const unidade = detalhe.escolaNome.trim() || "—";
        setAlunoSelecionado(alunoDetalheParaInfo(detalhe));
        setHistorico(
          matriculas
            .map((m) => matriculaParaHistorico(m, unidade))
            .sort((a, b) => b.id - a.id),
        );
        setOcorrencias(ocorrenciasLista);
        setDocumentos(documentosLista);
        setFaltas(faltasLista);
      } catch (e) {
        if (cancelado) return;
        const msg = e instanceof Error ? e.message : "Não foi possível carregar o perfil do aluno.";
        setErro(msg);
        setErroAbas(msg);
        setAlunoSelecionado({
          id: "N/A",
          nome: "Aluno não encontrado",
          cpf: "—",
          naturalidade: "—",
          dataIngresso: "—",
          dataNascimento: "—",
          telefoneAluno: "—",
          rg: "—",
          endereco: "—",
          telefoneResponsavel: "—",
          nomeResponsavel: "—",
          cpfResponsavel: "—",
        });
        setHistorico([]);
        setOcorrencias([]);
        setDocumentos([]);
        setFaltas([]);
      } finally {
        if (!cancelado) {
          setCarregando(false);
          setCarregandoAbas(false);
        }
      }
    }

    void carregar();
    return () => {
      cancelado = true;
    };
  }, [alunoIdNumerico]);

  const abas = [
    "Histórico escolar",
    "Ocorrências",
    "Gestão de documentos",
    "Faltas justificadas"
  ];

  function salvarEdicaoHistorico(dadosEditados: HistoricoMatricula) {
    setHistorico(prev => prev.map(m => m.id === dadosEditados.id ? dadosEditados : m));
  }

  async function recarregarOcorrencias() {
    const lista = await listarOcorrenciasAluno(alunoIdNumerico);
    setOcorrencias(lista);
  }

  async function recarregarFaltas() {
    const lista = await listarFaltasAluno(alunoIdNumerico);
    setFaltas(lista);
  }

  async function salvarOcorrencia(dadosOcorrencia: Ocorrencia, ocorrenciaIdEdicao?: number) {
    setErroAbas(null);
    try {
      const payload = ocorrenciaParaPayload(dadosOcorrencia);
      if (ocorrenciaIdEdicao) {
        const atualizada = await atualizarOcorrenciaAluno(alunoIdNumerico, ocorrenciaIdEdicao, payload);
        if (atualizada) {
          setOcorrencias((prev) =>
            prev.map((o) => (o.id === ocorrenciaIdEdicao ? atualizada : o)),
          );
        } else {
          await recarregarOcorrencias();
        }
      } else {
        const criada = await criarOcorrenciaAluno(alunoIdNumerico, payload);
        if (criada) {
          setOcorrencias((prev) => [criada, ...prev]);
        } else {
          await recarregarOcorrencias();
        }
      }
    } catch (e) {
      const msg =
        e instanceof ApiError
          ? e.message
          : e instanceof Error
            ? e.message
            : "Não foi possível salvar a ocorrência.";
      setErroAbas(msg);
      throw e;
    }
  }

  async function salvarDocumento() {
    setErroAbas(
      "Upload de documentos será habilitado em breve. O endpoint já está preparado na API.",
    );
  }

  async function deletarDocumento(id: number) {
    if (!confirm("Tem certeza que deseja excluir este documento permanentemente?")) return;
    setErroAbas("Exclusão de documentos será habilitada em breve. O endpoint já está preparado na API.");
  }

  async function justificarFalta(id: number, motivo: string) {
    setErroAbas(null);
    try {
      await justificarFaltaAluno(alunoIdNumerico, id, motivo);
      await recarregarFaltas();
    } catch (e) {
      const msg =
        e instanceof ApiError
          ? e.message
          : e instanceof Error
            ? e.message
            : "Não foi possível justificar a falta.";
      setErroAbas(msg);
      throw e;
    }
  }

  function formatDate(isoDate: string) {
    return formatarDataBr(isoDate);
  }

  const livrosComFaltas = Array.from(new Set(faltas.map(f => f.book)));

  return (
    <>
      <div className="flex flex-col gap-6">

        {/* Voltar e Título */}
        <div>
          <Link href="/secretaria" className="flex items-center gap-1.5 text-sm text-zinc-500 hover:text-zinc-800 transition-colors w-fit mb-3">
            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2"><polyline points="15 18 9 12 15 6"/></svg>
            Voltar para Secretaria
          </Link>
          <div className="flex items-center justify-between">
            <h1 className="text-xl font-semibold text-zinc-900">Perfil do Aluno</h1>
            {alunoSelecionado.id !== "N/A" && !carregando && (
               <span className="text-sm text-zinc-500 bg-white px-3 py-1.5 rounded-lg border border-zinc-200 shadow-sm">ID: {alunoSelecionado.id}</span>
            )}
          </div>
          {erro && (
            <p className="mt-2 text-sm text-red-600 bg-red-50 border border-red-200 rounded-lg px-3 py-2">{erro}</p>
          )}
        </div>

        {/* ─── CARD: INFORMAÇÕES GERAIS (COM DADOS DINÂMICOS) ─── */}
        <div className="bg-white border border-zinc-200 rounded-xl overflow-hidden shadow-sm">
          <div className="bg-zinc-50 border-b border-zinc-200 px-5 py-3">
            <h2 className="text-sm font-semibold text-zinc-700 uppercase tracking-wide">Informações Gerais</h2>
          </div>
          
          <div className="p-6 grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-y-6 gap-x-8">
            <LabelValue label="Nome" value={alunoSelecionado.nome} />
            <LabelValue label="CPF" value={alunoSelecionado.cpf} />
            <div className="hidden lg:block"></div> 

            <LabelValue label="Naturalidade" value={alunoSelecionado.naturalidade} />
            <LabelValue label="Data de ingresso" value={alunoSelecionado.dataIngresso} />
            <div className="hidden lg:block"></div>

            <LabelValue label="Telefone(s) do aluno" value={alunoSelecionado.telefoneAluno !== "—" ? `CELULAR: ${alunoSelecionado.telefoneAluno} [OUTRA]` : "—"} whatsapp />
            <LabelValue label="Data de nascimento" value={alunoSelecionado.dataNascimento} />
            <div className="hidden lg:block"></div>

            <LabelValue label="RG" value={alunoSelecionado.rg} />
            <div className="col-span-1 md:col-span-2">
              <LabelValue label="Endereço do aluno" value={alunoSelecionado.endereco} />
            </div>

            <LabelValue label="Telefone(s) do responsável" value={alunoSelecionado.telefoneResponsavel !== "—" ? `CELULAR: ${alunoSelecionado.telefoneResponsavel} [OUTRA]` : "—"} whatsapp />
            <div className="col-span-1 md:col-span-2"></div>

            <LabelValue label="Responsável" value={alunoSelecionado.nomeResponsavel} />
            <LabelValue label="Responsável | CPF" value={alunoSelecionado.cpfResponsavel} />
          </div>
        </div>

        {erroAbas && (
          <p className="text-sm text-amber-800 bg-amber-50 border border-amber-200 rounded-lg px-3 py-2">{erroAbas}</p>
        )}

        {/* ─── BARRA DE ABAS ─── */}
        <div className="flex flex-wrap gap-2 border-b border-zinc-200">
          {abas.map((aba) => (
            <button 
              key={aba} 
              onClick={() => setAbaAtual(aba)}
              className={`px-4 py-2.5 text-sm font-medium border-b-2 transition-colors ${
                abaAtual === aba 
                  ? "border-[#1F2A35] text-[#1F2A35]" 
                  : "border-transparent text-zinc-500 hover:text-zinc-700"
              }`}
            >
              {aba}
            </button>
          ))}
        </div>

        {/* ─── CONTEÚDO DAS ABAS ─── */}
        
        {/* ABA: Histórico Escolar */}
        {abaAtual === "Histórico escolar" && (
          <div className="bg-white border border-zinc-200 rounded-xl overflow-hidden shadow-sm">
            <div className="bg-zinc-50 border-b border-zinc-200 px-5 py-4">
              <h3 className="text-sm font-bold text-zinc-800">Histórico de matrículas</h3>
            </div>
            <div className="overflow-x-auto">
              <table className="w-full text-sm">
                <thead>
                  <tr className="bg-white border-b border-zinc-200">
                    <th className="text-left px-5 py-3 font-bold text-zinc-500 uppercase tracking-wide text-xs">Matrícula</th>
                    <th className="text-left px-5 py-3 font-bold text-zinc-500 uppercase tracking-wide text-xs">Período Letivo</th>
                    <th className="text-left px-5 py-3 font-bold text-zinc-500 uppercase tracking-wide text-xs">Unidade</th>
                    <th className="text-left px-5 py-3 font-bold text-zinc-500 uppercase tracking-wide text-xs">Turma</th>
                    <th className="text-left px-5 py-3 font-bold text-zinc-500 uppercase tracking-wide text-xs">Situação</th>
                    <th className="text-center px-5 py-3 font-bold text-zinc-500 uppercase tracking-wide text-xs">Opções</th>
                  </tr>
                </thead>
                <tbody>
                  {carregando && (
                    <tr>
                      <td colSpan={6} className="px-5 py-8 text-center text-sm text-zinc-500">
                        Carregando histórico de matrículas...
                      </td>
                    </tr>
                  )}
                  {!carregando && historico.length === 0 && (
                    <tr>
                      <td colSpan={6} className="px-5 py-8 text-center text-sm text-zinc-500">
                        Nenhuma matrícula encontrada para este aluno.
                      </td>
                    </tr>
                  )}
                  {!carregando && historico.map((mat, i) => (
                    <tr key={mat.id} className={`border-b border-zinc-100 hover:bg-zinc-50 transition-colors ${i === historico.length - 1 ? "border-b-0" : ""}`}>
                      <td className="px-5 py-3 text-zinc-600">{mat.id}</td>
                      <td className="px-5 py-3 font-medium text-zinc-800">{mat.periodoLetivo}</td>
                      <td className="px-5 py-3 text-zinc-600">{mat.unidade}</td>
                      <td className="px-5 py-3 text-zinc-600">{mat.turma}</td>
                      <td className="px-5 py-3">
                        <span className={`text-xs font-bold ${mat.situacao === 'ATIVO' ? 'text-green-600' : 'text-zinc-500'}`}>
                          {mat.situacao}
                        </span>
                      </td>
                      <td className="px-5 py-3">
                        <div className="flex items-center justify-center gap-2">
                          <button onClick={() => setMatriculaEditando(mat)} className="w-8 h-8 flex items-center justify-center rounded border border-zinc-200 hover:bg-zinc-100 text-zinc-500 transition-colors" title="Editar">
                            <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2"><path d="M11 4H4a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7"/><path d="M18.5 2.5a2.121 2.121 0 0 1 3 3L12 15l-4 1 1-4 9.5-9.5z"/></svg>
                          </button>
                          <button className="w-8 h-8 flex items-center justify-center rounded border border-zinc-200 hover:bg-zinc-100 text-blue-600 transition-colors" title="Ver Diário">
                            <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2"><path d="M4 19.5A2.5 2.5 0 0 1 6.5 17H20"/><path d="M6.5 2H20v20H6.5A2.5 2.5 0 0 1 4 19.5v-15A2.5 2.5 0 0 1 6.5 2z"/></svg>
                          </button>
                        </div>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </div>
        )}

        {/* ABA: Ocorrências */}
        {abaAtual === "Ocorrências" && (
          <div className="flex flex-col gap-4">
            <div className="flex items-center justify-between">
              <h3 className="text-lg font-medium text-zinc-800">Ocorrências Registradas</h3>
              <button onClick={() => setOcorrenciaModal({ isOpen: true, oco: null })} className="flex items-center gap-2 h-9 px-4 text-sm font-medium text-white bg-[#1F2A35] rounded-lg hover:bg-[#2d3d4d] transition-colors">
                <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5"><line x1="12" y1="5" x2="12" y2="19"/><line x1="5" y1="12" x2="19" y2="12"/></svg>
                Nova ocorrência
              </button>
            </div>
            <div className="flex flex-col gap-4">
              {carregandoAbas ? (
                <div className="bg-white border border-zinc-200 rounded-xl p-10 text-center text-zinc-500">Carregando ocorrencias...</div>
              ) : ocorrencias.length === 0 ? (
                <div className="bg-white border border-zinc-200 rounded-xl p-10 text-center text-zinc-500">Nenhuma ocorrência registrada.</div>
              ) : (
                ocorrencias.map((oco) => (
                  <div key={oco.id} className="bg-white border border-zinc-200 rounded-xl p-5 shadow-sm flex flex-col md:flex-row gap-4 md:gap-8 relative">
                    <button onClick={() => setOcorrenciaModal({ isOpen: true, oco: oco })} className="absolute top-4 right-4 w-8 h-8 flex items-center justify-center rounded border border-zinc-200 hover:bg-zinc-50 text-zinc-400 hover:text-zinc-700 transition-colors" title="Editar ocorrência">
                      <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2"><path d="M11 4H4a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7"/><path d="M18.5 2.5a2.121 2.121 0 0 1 3 3L12 15l-4 1 1-4 9.5-9.5z"/></svg>
                    </button>
                    <div className="min-w-[150px] shrink-0 border-l-4 pl-3 py-1 flex flex-col justify-center border-blue-500">
                      <span className="text-sm font-bold text-zinc-900">{formatDate(oco.data)}</span>
                      <span className="text-xs text-zinc-500 mt-0.5">{oco.hora}</span>
                      <span className={`mt-2 mb-1 inline-flex items-center px-2 py-0.5 rounded text-[10px] font-bold uppercase tracking-wide w-fit ${oco.tipo === 'Acadêmica' ? 'bg-indigo-50 text-indigo-700' : 'bg-orange-50 text-orange-700'}`}>{oco.tipo}</span>
                      <span className="text-[10px] text-zinc-400 mt-1 uppercase font-semibold">Registrado por:</span>
                      <span className="text-xs font-medium text-zinc-700">{oco.autor}</span>
                    </div>
                    <div className="flex-1 flex flex-col gap-3 pt-1">
                      <div><span className="text-xs font-semibold text-zinc-500 uppercase tracking-wide block mb-1">Descrição</span><p className="text-sm text-zinc-800 leading-relaxed">{oco.descricao}</p></div>
                      {oco.resolucao && (<div className="bg-zinc-50 rounded p-3 border border-zinc-100"><span className="text-xs font-semibold text-zinc-500 uppercase tracking-wide block mb-1">Resolução Adotada</span><p className="text-sm text-zinc-700">{oco.resolucao}</p></div>)}
                    </div>
                  </div>
                ))
              )}
            </div>
          </div>
        )}

        {/* ABA: Gestão de Documentos */}
        {abaAtual === "Gestão de documentos" && (
          <div className="flex flex-col gap-4">
            <div className="flex items-center justify-between">
              <h3 className="text-lg font-medium text-zinc-800">Documentos Anexados</h3>
              <button onClick={() => setDocumentoModalAberto(true)} className="flex items-center gap-2 h-9 px-4 text-sm font-medium text-white bg-[#1F2A35] rounded-lg hover:bg-[#2d3d4d] transition-colors">
                <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5"><path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4"/><polyline points="17 8 12 3 7 8"/><line x1="12" y1="3" x2="12" y2="15"/></svg>
                Anexar documento
              </button>
            </div>
            <div className="bg-white border border-zinc-200 rounded-xl overflow-hidden shadow-sm">
              <table className="w-full text-sm">
                <thead>
                  <tr className="bg-zinc-50 border-b border-zinc-200">
                    <th className="text-left px-5 py-3 font-bold text-zinc-500 uppercase tracking-wide text-xs">Nome do Arquivo</th>
                    <th className="text-left px-5 py-3 font-bold text-zinc-500 uppercase tracking-wide text-xs">Tipo</th>
                    <th className="text-left px-5 py-3 font-bold text-zinc-500 uppercase tracking-wide text-xs">Tamanho</th>
                    <th className="text-left px-5 py-3 font-bold text-zinc-500 uppercase tracking-wide text-xs">Enviado em</th>
                    <th className="text-left px-5 py-3 font-bold text-zinc-500 uppercase tracking-wide text-xs">Autor</th>
                    <th className="text-center px-5 py-3 font-bold text-zinc-500 uppercase tracking-wide text-xs">Ações</th>
                  </tr>
                </thead>
                <tbody>
                  {carregandoAbas ? (
                    <tr><td colSpan={6} className="text-center py-12 text-zinc-400">Carregando documentos...</td></tr>
                  ) : documentos.length === 0 ? (
                    <tr><td colSpan={6} className="text-center py-12 text-zinc-400">Nenhum documento anexado.</td></tr>
                  ) : (
                    documentos.map((doc, i) => (
                      <tr key={doc.id} className={`border-b border-zinc-100 hover:bg-zinc-50 transition-colors ${i === documentos.length - 1 ? "border-b-0" : ""}`}>
                        <td className="px-5 py-3 font-medium text-zinc-800 flex items-center gap-2">
                          <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" className="text-blue-500"><path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"/><polyline points="14 2 14 8 20 8"/><line x1="16" y1="13" x2="8" y2="13"/><line x1="16" y1="17" x2="8" y2="17"/><polyline points="10 9 9 9 8 9"/></svg>
                          {doc.nome}
                        </td>
                        <td className="px-5 py-3 text-zinc-600">{doc.tipo}</td>
                        <td className="px-5 py-3 text-zinc-600">{doc.tamanho}</td>
                        <td className="px-5 py-3 text-zinc-600">{doc.dataEnvio}</td>
                        <td className="px-5 py-3 text-zinc-600">{doc.autor}</td>
                        <td className="px-5 py-3">
                          <div className="flex items-center justify-center gap-2">
                            <button className="w-8 h-8 flex items-center justify-center rounded border border-zinc-200 hover:bg-blue-50 text-blue-600 transition-colors" title="Baixar Arquivo"><svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2"><path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4"/><polyline points="7 10 12 15 17 10"/><line x1="12" y1="15" x2="12" y2="3"/></svg></button>
                            <button onClick={() => deletarDocumento(doc.id)} className="w-8 h-8 flex items-center justify-center rounded border border-zinc-200 hover:bg-red-50 text-red-600 transition-colors" title="Excluir Arquivo"><svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2"><polyline points="3 6 5 6 21 6"/><path d="M19 6v14a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V6m3 0V4a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2"/><line x1="10" y1="11" x2="10" y2="17"/><line x1="14" y1="11" x2="14" y2="17"/></svg></button>
                          </div>
                        </td>
                      </tr>
                    ))
                  )}
                </tbody>
              </table>
            </div>
          </div>
        )}

        {/* ABA: Faltas Justificadas */}
        {abaAtual === "Faltas justificadas" && (
          <div className="flex flex-col gap-6">
            <h3 className="text-lg font-medium text-zinc-800">Faltas por Período Letivo</h3>
            {carregandoAbas ? (
              <div className="bg-white border border-zinc-200 rounded-xl p-10 text-center text-zinc-500">Carregando faltas...</div>
            ) : faltas.length === 0 ? (
              <div className="bg-white border border-zinc-200 rounded-xl p-10 text-center text-zinc-500">O aluno não possui nenhuma falta registrada no sistema.</div>
            ) : (
              livrosComFaltas.map((livro) => {
                const faltasDoLivro = faltas.filter(f => f.book === livro);
                return (
                  <div key={livro} className="bg-white border border-zinc-200 rounded-xl overflow-hidden shadow-sm">
                    <div className="bg-zinc-50 border-b border-zinc-200 px-5 py-3">
                      <h4 className="text-sm font-bold text-[#1F2A35]">{livro}</h4>
                    </div>
                    <div className="overflow-x-auto">
                      <table className="w-full text-sm">
                        <thead>
                          <tr className="border-b border-zinc-200">
                            <th className="text-left px-5 py-3 font-bold text-zinc-500 uppercase tracking-wide text-xs w-32">Data da Falta</th>
                            <th className="text-left px-5 py-3 font-bold text-zinc-500 uppercase tracking-wide text-xs">Aula / Lição</th>
                            <th className="text-left px-5 py-3 font-bold text-zinc-500 uppercase tracking-wide text-xs w-48">Status</th>
                            <th className="text-right px-5 py-3 font-bold text-zinc-500 uppercase tracking-wide text-xs w-44">Ação</th>
                          </tr>
                        </thead>
                        <tbody>
                          {faltasDoLivro.map((falta, index) => (
                            <tr key={falta.id} className={`hover:bg-zinc-50 transition-colors ${index === faltasDoLivro.length - 1 ? "" : "border-b border-zinc-100"}`}>
                              <td className="px-5 py-4 font-medium text-zinc-800">{formatDate(falta.data)}</td>
                              <td className="px-5 py-4 text-zinc-600">{falta.aula}</td>
                              <td className="px-5 py-4">
                                {falta.justificada ? (
                                  <span className="inline-flex items-center gap-1.5 px-2.5 py-1 rounded-md text-[11px] font-bold bg-green-50 text-green-700 border border-green-200"><svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="3"><polyline points="20 6 9 17 4 12"/></svg> JUSTIFICADA</span>
                                ) : (
                                  <span className="inline-flex items-center gap-1.5 px-2.5 py-1 rounded-md text-[11px] font-bold bg-red-50 text-red-600 border border-red-200"><svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="3"><line x1="18" y1="6" x2="6" y2="18"/><line x1="6" y1="6" x2="18" y2="18"/></svg> NÃO JUSTIFICADA</span>
                                )}
                              </td>
                              <td className="px-5 py-4 text-right">
                                <button onClick={() => setFaltaModalAberto(falta)} className={`h-8 px-3 text-xs font-medium rounded-lg transition-colors border ${falta.justificada ? "text-zinc-600 border-zinc-300 hover:bg-zinc-100" : "text-white bg-[#1F2A35] border-[#1F2A35] hover:bg-[#2d3d4d]"}`}>{falta.justificada ? "Ver Justificativa" : "Justificar"}</button>
                              </td>
                            </tr>
                          ))}
                        </tbody>
                      </table>
                    </div>
                  </div>
                );
              })
            )}
          </div>
        )}

      </div>

      {/* Render dos Modais */}
      {matriculaEditando && (
        <ModalEditarHistorico matricula={matriculaEditando} onClose={() => setMatriculaEditando(null)} onSave={salvarEdicaoHistorico} />
      )}

      {ocorrenciaModal.isOpen && (
        <ModalOcorrencia
          ocorrencia={ocorrenciaModal.oco}
          onClose={() => setOcorrenciaModal({ isOpen: false, oco: null })}
          onSave={(dados) => salvarOcorrencia(dados, ocorrenciaModal.oco?.id)}
        />
      )}

      {documentoModalAberto && (
        <ModalAnexarDocumento onClose={() => setDocumentoModalAberto(false)} onSave={salvarDocumento} />
      )}

      {faltaModalAberto && (
        <ModalJustificativaFalta falta={faltaModalAberto} onClose={() => setFaltaModalAberto(null)} onSave={justificarFalta} />
      )}
    </>
  );
}