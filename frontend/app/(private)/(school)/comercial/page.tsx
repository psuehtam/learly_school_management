"use client";

import { useCallback, useEffect, useMemo, useState } from "react";
import { getCurrentUser } from "@/lib/api/auth";
import {
  criarPreAluno,
  getApiErrorMessage,
  cancelarPreAluno,
  listarContratoTemplates,
  listarLivrosInteressePreAluno,
  listarPreAlunos,
  submeterPreAlunoParaAprovacao,
  aprovarMatricula,
} from "@/lib/api";
import { hasPermission } from "@/lib/permissions";
import type { User } from "@/lib/api/types";
import type {
  ContratoTemplate,
  CriarPreAlunoPayload,
  LivroInteresseOpcao,
  PreAlunoListItem,
  PreAlunoStatus,
} from "@/types/comercial";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Card, CardHeader, CardTitle } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Modal, useModalRequestClose } from "@/components/ui/modal";
import { buscarEnderecoPorCep } from "@/lib/viacep";
import { applyBrazilMask, digitsOnly } from "@/utils";

const SELECT_FIELD =
  "h-10 w-full rounded-lg border border-zinc-300 bg-white px-3 text-sm text-zinc-900 shadow-sm outline-none focus:border-[#1F2A35] focus:ring-2 focus:ring-[#1F2A35]/20";

function formatMoney(n: number): string {
  return n.toLocaleString("pt-BR", { style: "currency", currency: "BRL" });
}

function formatDate(iso: string): string {
  const d = new Date(iso);
  if (Number.isNaN(d.getTime())) return "-";
  return d.toLocaleDateString("pt-BR");
}

const STATUS_META: Record<PreAlunoStatus, { label: string }> = {
  "Em negociacao": { label: "Em negociação" },
  "Aguardando aprovacao": { label: "Aguardando aprovação" },
  Aprovado: { label: "Aprovado" },
  Matriculado: { label: "Matriculado" },
  Cancelado: { label: "Cancelado" },
};

const FILTROS_STATUS: Array<{ value: string; label: string }> = [
  { value: "", label: "Todos" },
  { value: "Em negociacao", label: STATUS_META["Em negociacao"].label },
  { value: "Aguardando aprovacao", label: STATUS_META["Aguardando aprovacao"].label },
  { value: "Aprovado", label: STATUS_META.Aprovado.label },
  { value: "Matriculado", label: STATUS_META.Matriculado.label },
  { value: "Cancelado", label: STATUS_META.Cancelado.label },
];

type PassoCadastroPreAluno = "aluno" | "responsavel" | "comercial";
type OpcaoRespAdulto = "proprio" | "outro" | null;

/** Idade em anos completos em relação ao dia atual (calendário local do navegador). */
function calcularIdadeAnosDoDia(dataIso: string): number | null {
  const m = /^(\d{4})-(\d{2})-(\d{2})$/.exec(dataIso.trim());
  if (!m) return null;
  const y = Number(m[1]);
  const mo = Number(m[2]);
  const d = Number(m[3]);
  const now = new Date();
  const ty = now.getFullYear();
  const tm = now.getMonth() + 1;
  const td = now.getDate();
  let idade = ty - y;
  if (tm < mo || (tm === mo && td < d)) idade--;
  return idade;
}

const ORIGEM_CAPTACAO_OPCOES = [
  "Indicação",
  "Redes sociais",
  "Site ou lead digital",
  "Ativo / Ligação outbound",
  "Evento ou feira",
  "Passou na frente / outdoor",
  "Parceria",
  "Google / busca",
  "Outro",
] as const;

const FORMAS_PAGAMENTO_OPCOES = ["PIX", "Boleto", "Cartão", "Dinheiro", "Outro"] as const;

const emptyPayload: CriarPreAlunoPayload = {
  eProprioResponsavel: false,
  alunoCpf: "",
  responsavelTipoPessoa: "Fisica",
  responsavelCpfCnpj: "",
  responsavelNome: "",
  responsavelSobrenome: "",
  responsavelTelefone: "",
  nome: "",
  sobrenome: "",
  dataNascimento: "",
  telefoneAluno: "",
  livroInteresseId: 0,
  tipoContrato: "",
  valorMensalidade: 0,
  formaPagamento: "",
  valorMatricula: 0,
  formaPagamentoMatricula: "",
  valorMaterial: 0,
  origemCaptacao: "",
  usaTransporteVan: false,
  transporteCep: "",
  transporteLogradouro: "",
  transporteNumero: "",
  transporteComplemento: "",
  transporteBairro: "",
  transporteCidade: "",
  transporteUf: "",
  observacoesComerciais: "",
};

function escolherTemplateContratoPadrao(templates: ContratoTemplate[]): number | "" {
  if (templates.length === 0) return "";
  const ativo = templates.find((t) => t.ativo);
  return ativo?.id ?? templates[0].id;
}

function serializarModalPreAlunoEstado(p: {
  form: CriarPreAlunoPayload;
  passo: PassoCadastroPreAluno;
  tipo: OpcaoRespAdulto;
  templateContratoId: number | "";
}) {
  return JSON.stringify(p);
}

function PreAlunoCancelarBtn({ disabled }: { disabled?: boolean }) {
  const requestClose = useModalRequestClose();
  return (
    <Button type="button" variant="secondary" onClick={requestClose} disabled={disabled}>
      Cancelar
    </Button>
  );
}

export default function ComercialPage() {
  const [user, setUser] = useState<User | null>(null);
  const [lista, setLista] = useState<PreAlunoListItem[]>([]);
  const [livros, setLivros] = useState<LivroInteresseOpcao[]>([]);
  const [contratoTemplates, setContratoTemplates] = useState<ContratoTemplate[]>([]);
  const [filtroStatus, setFiltroStatus] = useState("");
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const [modalNovo, setModalNovo] = useState(false);
  const [preAlunoModalBaseline, setPreAlunoModalBaseline] = useState("");
  const [formNovo, setFormNovo] = useState<CriarPreAlunoPayload>(() => ({ ...emptyPayload }));
  const [passoCadastro, setPassoCadastro] = useState<PassoCadastroPreAluno>("aluno");
  const [tipoResponsavelAdulto, setTipoResponsavelAdulto] = useState<OpcaoRespAdulto>(null);
  const [salvando, setSalvando] = useState(false);
  const [templateContratoId, setTemplateContratoId] = useState<number | "">("");
  const [cepVanBuscando, setCepVanBuscando] = useState(false);

  const idadePreAluno = useMemo(() => calcularIdadeAnosDoDia(formNovo.dataNascimento), [formNovo.dataNascimento]);
  const menorDeIdade = idadePreAluno !== null && idadePreAluno < 18;
  const maiorOu18 = idadePreAluno !== null && idadePreAluno >= 18;

  useEffect(() => {
    if (!modalNovo) setPreAlunoModalBaseline("");
  }, [modalNovo]);

  const preAlunoModalEstadoAtual = useMemo(
    () =>
      serializarModalPreAlunoEstado({
        form: formNovo,
        passo: passoCadastro,
        tipo: tipoResponsavelAdulto,
        templateContratoId,
      }),
    [formNovo, passoCadastro, tipoResponsavelAdulto, templateContratoId],
  );

  const preAlunoModalTemAlteracao =
    modalNovo &&
    !salvando &&
    preAlunoModalBaseline !== "" &&
    preAlunoModalEstadoAtual !== preAlunoModalBaseline;

  const podeCriar = user ? hasPermission(user, "CRIAR_PRE_ALUNO") : false;
  const podeEditarOuCriarFicha = user
    ? hasPermission(user, "CRIAR_PRE_ALUNO") || hasPermission(user, "EDITAR_PRE_ALUNO")
    : false;
  const podeCancelar = user ? hasPermission(user, "CANCELAR_PRE_ALUNO") : false;
  const podeAprovar = user ? hasPermission(user, "APROVAR_MATRICULA") : false;

  useEffect(() => {
    void getCurrentUser().then(setUser).catch(() => setUser(null));
  }, []);

  const carregar = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const [pa, liv, templates] = await Promise.all([
        listarPreAlunos(filtroStatus.trim() ? { status: filtroStatus.trim() } : undefined),
        listarLivrosInteressePreAluno(),
        listarContratoTemplates().catch(() => [] as ContratoTemplate[]),
      ]);
      setLista(pa);
      setLivros(liv);
      setContratoTemplates(templates);
    } catch (e) {
      setError(getApiErrorMessage(e, "Falha ao carregar dados do comercial."));
    } finally {
      setLoading(false);
    }
  }, [filtroStatus]);

  useEffect(() => {
    void carregar();
  }, [carregar]);

  function resolverTipoContrato(): string {
    if (templateContratoId === "") return "";
    const template = contratoTemplates.find((t) => t.id === templateContratoId);
    return template?.nome.trim() ?? "";
  }

  const abrirNovo = () => {
    const templatePadrao = escolherTemplateContratoPadrao(contratoTemplates);
    setFormNovo({ ...emptyPayload });
    setTemplateContratoId(templatePadrao);
    setPassoCadastro("aluno");
    setTipoResponsavelAdulto(null);
    setPreAlunoModalBaseline(
      serializarModalPreAlunoEstado({
        form: { ...emptyPayload },
        passo: "aluno",
        tipo: null,
        templateContratoId: templatePadrao,
      }),
    );
    setModalNovo(true);
  };

  const fecharModalNovo = () => {
    setModalNovo(false);
    setPassoCadastro("aluno");
    setTipoResponsavelAdulto(null);
  };

  const buscarCepTransporteVan = async () => {
    setCepVanBuscando(true);
    try {
      const r = await buscarEnderecoPorCep(formNovo.transporteCep ?? "");
      const logApi = r.logradouro.trim();
      const montado =
        logApi !== ""
          ? `${r.tipoLogradouro} ${logApi}`
              .replace(/\s+/g, " ")
              .trim()
              .replace(/^Outro\s+/, "")
              .trim()
          : "";

      setFormNovo((p) => ({
        ...p,
        transporteCep: digitsOnly(r.cepFormatado, 8),
        transporteLogradouro: montado !== "" ? montado : p.transporteLogradouro,
        transporteBairro: r.bairro !== "" ? r.bairro : p.transporteBairro,
        transporteCidade: r.municipio !== "" ? r.municipio : p.transporteCidade,
        transporteUf: r.uf !== "" ? r.uf : (p.transporteUf ?? ""),
        transporteComplemento:
          r.complemento.trim() !== "" ? r.complemento : (p.transporteComplemento ?? ""),
      }));
    } catch (e) {
      alert(e instanceof Error ? e.message : "Não foi possível buscar o CEP.");
    } finally {
      setCepVanBuscando(false);
    }
  };

  const avancarEtapaAluno = () => {
    if (
      !formNovo.nome.trim()
      || !formNovo.sobrenome.trim()
      || !formNovo.dataNascimento
      || idadePreAluno === null
      || !formNovo.livroInteresseId
    ) {
      return;
    }

    setPassoCadastro("responsavel");
    setTipoResponsavelAdulto(null);

    setFormNovo((p) => ({
      ...p,
      eProprioResponsavel: false,
      alunoCpf: "",
      responsavelNome: "",
      responsavelSobrenome: "",
      responsavelCpfCnpj: "",
      responsavelTelefone: "",
      responsavelTipoPessoa: "Fisica",
    }));
  };

  const escolherProprioResponsavel = () => {
    setTipoResponsavelAdulto("proprio");
    setFormNovo((p) => ({
      ...p,
      eProprioResponsavel: true,
      alunoCpf: "",
      responsavelTipoPessoa: "Fisica",
      responsavelCpfCnpj: "",
      responsavelNome: "",
      responsavelSobrenome: "",
      responsavelTelefone: "",
    }));
  };

  const escolherOutroResponsavel = () => {
    setTipoResponsavelAdulto("outro");
    setFormNovo((p) => ({
      ...p,
      eProprioResponsavel: false,
      alunoCpf: "",
    }));
  };

  const salvarNovo = async () => {
    const telA = digitsOnly(formNovo.telefoneAluno ?? "");
    const usarProprio = maiorOu18 && formNovo.eProprioResponsavel;
    const docResp = digitsOnly(formNovo.responsavelCpfCnpj);
    const telR = digitsOnly(formNovo.responsavelTelefone);
    const cpfAlunoDigits = digitsOnly(formNovo.alunoCpf ?? "");

    const valorMat = formNovo.valorMaterial ?? 0;
    const transp = formNovo.usaTransporteVan
      ? {
          transporteCep: digitsOnly(formNovo.transporteCep ?? "", 8),
          transporteLogradouro: formNovo.transporteLogradouro?.trim() ?? "",
          transporteNumero: formNovo.transporteNumero?.trim() ?? "",
          transporteComplemento:
            formNovo.transporteComplemento?.trim() !== "" ? formNovo.transporteComplemento?.trim() : null,
          transporteBairro: formNovo.transporteBairro?.trim() ?? "",
          transporteCidade: formNovo.transporteCidade?.trim() ?? "",
          transporteUf: (formNovo.transporteUf ?? "").trim().toUpperCase(),
        }
      : {
          transporteCep: null,
          transporteLogradouro: null,
          transporteNumero: null,
          transporteComplemento: null,
          transporteBairro: null,
          transporteCidade: null,
          transporteUf: null,
        };

    const baseComercial = {
      ...formNovo,
      tipoContrato: resolverTipoContrato(),
      valorMaterial: valorMat,
      formaPagamento: formNovo.formaPagamento?.trim() !== "" ? formNovo.formaPagamento!.trim() : null,
      formaPagamentoMatricula:
        formNovo.valorMatricula > 0 ? formNovo.formaPagamentoMatricula?.trim() || null : null,
      observacoesComerciais:
        formNovo.observacoesComerciais?.trim() !== "" ? formNovo.observacoesComerciais?.trim() : null,
      origemCaptacao: formNovo.origemCaptacao.trim(),
      ...transp,
    };

    const payload: CriarPreAlunoPayload = usarProprio
      ? {
          ...baseComercial,
          eProprioResponsavel: true,
          alunoCpf: cpfAlunoDigits,
          responsavelTipoPessoa: "Fisica",
          responsavelCpfCnpj: "",
          responsavelNome: "",
          responsavelSobrenome: "",
          responsavelTelefone: "",
          telefoneAluno: telA.length >= 10 ? telA : null,
        }
      : {
          ...baseComercial,
          eProprioResponsavel: false,
          alunoCpf: null,
          responsavelCpfCnpj: docResp,
          responsavelTelefone: telR,
          telefoneAluno: telA.length >= 10 ? telA : null,
        };

    setSalvando(true);
    setError(null);
    try {
      await criarPreAluno(payload);
      fecharModalNovo();
      await carregar();
    } catch (e) {
      setError(getApiErrorMessage(e, "Nao foi possivel criar o pre-aluno."));
    } finally {
      setSalvando(false);
    }
  };

  const onSubmeter = async (row: PreAlunoListItem) => {
    if (!confirm(`Enviar "${row.nomeCompletoAluno}" para aprovacao da secretaria?`)) return;
    setError(null);
    try {
      await submeterPreAlunoParaAprovacao(row.id);
      await carregar();
    } catch (e) {
      setError(getApiErrorMessage(e, "Falha ao enviar para aprovacao."));
    }
  };

  const onAprovar = async (row: PreAlunoListItem) => {
    if (!confirm(`Aprovar pre-aluno "${row.nomeCompletoAluno}"?`)) return;
    setError(null);
    try {
      await aprovarMatricula(row.id);
      await carregar();
    } catch (e) {
      setError(getApiErrorMessage(e, "Falha ao aprovar."));
    }
  };

  const onCancelar = async (row: PreAlunoListItem) => {
    if (!confirm(`Cancelar pre-aluno "${row.nomeCompletoAluno}"? Esta acao nao remove o registro do historico.`)) return;
    setError(null);
    try {
      await cancelarPreAluno(row.id);
      await carregar();
    } catch (e) {
      setError(getApiErrorMessage(e, "Falha ao cancelar."));
    }
  };

  const etapaAlunoValida = Boolean(
    formNovo.nome.trim()
      && formNovo.sobrenome.trim()
      && formNovo.dataNascimento
      && idadePreAluno !== null
      && formNovo.livroInteresseId > 0,
  );

  const etapaResponsavelValida = useMemo(() => {
    const telAlunoDig = digitsOnly(formNovo.telefoneAluno ?? "");
    const cpfProprioDig = digitsOnly(formNovo.alunoCpf ?? "");
    const docOutroDig = digitsOnly(formNovo.responsavelCpfCnpj);
    const telOutroDig = digitsOnly(formNovo.responsavelTelefone);

    if (menorDeIdade) {
      const okDoc =
        formNovo.responsavelTipoPessoa === "Fisica" ? docOutroDig.length === 11 : docOutroDig.length === 14;
      return (
        okDoc
        && telOutroDig.length >= 10
        && formNovo.responsavelNome.trim().length > 0
        && formNovo.responsavelSobrenome.trim().length > 0
      );
    }

    if (tipoResponsavelAdulto === "proprio") {
      return cpfProprioDig.length === 11 && telAlunoDig.length >= 10;
    }

    if (tipoResponsavelAdulto === "outro") {
      const okDoc =
        formNovo.responsavelTipoPessoa === "Fisica" ? docOutroDig.length === 11 : docOutroDig.length === 14;
      return (
        okDoc
        && telOutroDig.length >= 10
        && formNovo.responsavelNome.trim().length > 0
        && formNovo.responsavelSobrenome.trim().length > 0
      );
    }

    return false;
  }, [
    menorDeIdade,
    tipoResponsavelAdulto,
    formNovo.telefoneAluno,
    formNovo.alunoCpf,
    formNovo.responsavelCpfCnpj,
    formNovo.responsavelTelefone,
    formNovo.responsavelNome,
    formNovo.responsavelSobrenome,
    formNovo.responsavelTipoPessoa,
  ]);

  const tipoContratoResolvido = useMemo(() => {
    if (templateContratoId === "") return "";
    const template = contratoTemplates.find((t) => t.id === templateContratoId);
    return template?.nome.trim() ?? "";
  }, [templateContratoId, contratoTemplates]);

  const etapaComercialValida = useMemo(() => {
    const cepOk = digitsOnly(formNovo.transporteCep ?? "").length === 8;
    const endBasico =
      Boolean(formNovo.transporteLogradouro?.trim())
      && Boolean(formNovo.transporteNumero?.trim())
      && Boolean(formNovo.transporteBairro?.trim())
      && Boolean(formNovo.transporteCidade?.trim())
      && (formNovo.transporteUf ?? "").trim().length === 2;
    const vanOk = !formNovo.usaTransporteVan ? true : cepOk && endBasico;

    const matriculaPgtoOk = formNovo.valorMatricula <= 0 || Boolean(formNovo.formaPagamentoMatricula?.trim());

    return Boolean(
      formNovo.valorMensalidade > 0
        && tipoContratoResolvido.length > 0
        && tipoContratoResolvido.length <= 120
        && formNovo.origemCaptacao.trim().length > 0
        && matriculaPgtoOk
        && vanOk,
    );
  }, [
    formNovo.transporteCep,
    formNovo.transporteLogradouro,
    formNovo.transporteNumero,
    formNovo.transporteBairro,
    formNovo.transporteCidade,
    formNovo.transporteUf,
    formNovo.usaTransporteVan,
    formNovo.valorMensalidade,
    formNovo.origemCaptacao,
    formNovo.valorMatricula,
    formNovo.formaPagamentoMatricula,
    tipoContratoResolvido,
  ]);

  const podeSalvarTudo =
    etapaAlunoValida && etapaResponsavelValida && etapaComercialValida;

  return (
    <div className="flex flex-col gap-6">
      <div className="flex flex-wrap items-center justify-between gap-4">
        <div>
          <h1 className="text-xl font-semibold text-zinc-900">Comercial · Pré-alunos</h1>
          <p className="text-sm text-zinc-500">
            Cadastre fichas conforme o manual: após o contrato, envie para a secretaria aprovar.
          </p>
        </div>
        <div className="flex flex-wrap gap-2">
          <Button variant="secondary" onClick={() => void carregar()} isLoading={loading}>
            Atualizar
          </Button>
          {podeCriar && (
            <Button onClick={abrirNovo}>Novo pré-aluno</Button>
          )}
        </div>
      </div>

      <Card className="p-4">
        <label className="text-sm font-medium text-zinc-700">Filtrar por status</label>
        <select
          value={filtroStatus}
          onChange={(e) => setFiltroStatus(e.target.value)}
          className={`mt-1.5 max-w-xs ${SELECT_FIELD}`}
        >
          {FILTROS_STATUS.map((f) => (
            <option key={f.value || "all"} value={f.value}>
              {f.label}
            </option>
          ))}
        </select>
      </Card>

      {error && (
        <div className="rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700">
          {error}
        </div>
      )}

      <Card>
        <CardHeader className="mb-2">
          <CardTitle className="text-base">Lista ({lista.length})</CardTitle>
        </CardHeader>
        <div className="overflow-x-auto rounded-lg border border-zinc-100">
          <table className="w-full min-w-[960px] text-sm">
            <thead>
              <tr className="border-b border-zinc-200 bg-zinc-50/80 text-left text-xs font-semibold uppercase tracking-wide text-zinc-500">
                <th className="px-4 py-3">Pré-aluno</th>
                <th className="px-4 py-3">Responsável</th>
                <th className="px-4 py-3">Livro</th>
                <th className="px-4 py-3">Contrato / valor</th>
                <th className="px-4 py-3">Cadastro</th>
                <th className="px-4 py-3">Status</th>
                <th className="px-4 py-3 text-right">Ações</th>
              </tr>
            </thead>
            <tbody>
              {!loading && lista.length === 0 ? (
                <tr>
                  <td colSpan={7} className="px-4 py-12 text-center text-zinc-400">
                    Nenhum pré-aluno encontrado.
                  </td>
                </tr>
              ) : (
                lista.map((row) => {
                  const st = STATUS_META[row.status] ?? STATUS_META["Em negociacao"];
                  const badgeVariant =
                    row.status === "Cancelado"
                      ? "danger"
                      : row.status === "Aprovado" || row.status === "Matriculado"
                        ? "success"
                        : row.status === "Em negociacao"
                          ? "warning"
                          : "info";
                  return (
                    <tr key={row.id} className="border-b border-zinc-100 last:border-b-0 hover:bg-zinc-50/80">
                      <td className="px-4 py-3 font-medium text-zinc-900">{row.nomeCompletoAluno}</td>
                      <td className="px-4 py-3 text-zinc-700">{row.nomeCompletoResponsavel}</td>
                      <td className="px-4 py-3 text-zinc-600">{row.nomeLivroInteresse}</td>
                      <td className="px-4 py-3 text-zinc-600">
                        <div>{row.tipoContrato}</div>
                        <div className="text-xs text-zinc-500 space-y-0.5">
                          <div>
                            {formatMoney(row.valorMensalidade)} / mensalidade
                            {row.formaPagamento ? <> · pagamento mensalidade: {row.formaPagamento}</> : <> · mensalidade: pgto não informado</>}
                          </div>
                          <div>
                            Matrícula: {formatMoney(row.valorMatricula)} ·                             Livro/material:{" "}
                            {row.valorMaterial == null ? "—" : formatMoney(Number(row.valorMaterial))} · Origem:{" "}
                            {row.origemCaptacao}
                          </div>
                        </div>
                      </td>
                      <td className="whitespace-nowrap px-4 py-3 text-xs text-zinc-500">{formatDate(row.dataCadastro)}</td>
                      <td className="px-4 py-3">
                        <Badge variant={badgeVariant}>{st.label}</Badge>
                      </td>
                      <td className="px-4 py-3 text-right">
                        <div className="flex flex-wrap justify-end gap-2">
                          {row.status === "Em negociacao" && podeEditarOuCriarFicha && (
                            <Button
                              variant="secondary"
                              size="sm"
                              className="h-8 px-3 text-xs"
                              onClick={() => void onSubmeter(row)}
                            >
                              Enviar p/ secretaria
                            </Button>
                          )}
                          {row.status === "Aguardando aprovacao" && podeAprovar && (
                            <Button size="sm" className="h-8 px-3 text-xs" onClick={() => void onAprovar(row)}>
                              Aprovar
                            </Button>
                          )}
                          {row.status !== "Matriculado" && row.status !== "Cancelado" && podeCancelar && (
                            <Button variant="danger" size="sm" className="h-8 px-3 text-xs" onClick={() => void onCancelar(row)}>
                              Cancelar
                            </Button>
                          )}
                        </div>
                      </td>
                    </tr>
                  );
                })
              )}
            </tbody>
          </table>
        </div>
      </Card>

      <Modal
        open={modalNovo}
        title="Novo pré-aluno"
        className="max-w-2xl"
        onClose={fecharModalNovo}
        hasUnsavedChanges={preAlunoModalTemAlteracao}
        closeDisabled={salvando}
      >
        <div className="flex max-h-[min(72vh,calc(100vh-8rem))] flex-col gap-4 overflow-y-auto px-6 py-4">
          <div className="flex items-center gap-2 text-xs text-zinc-500">
            <span className={`rounded-full px-2 py-0.5 ${passoCadastro === "aluno" ? "bg-[#1F2A35] text-white" : "bg-zinc-200"}`}>
              1 · Pré-aluno
            </span>
            <span aria-hidden className="text-zinc-300">›</span>
            <span className={`rounded-full px-2 py-0.5 ${passoCadastro === "responsavel" ? "bg-[#1F2A35] text-white" : "bg-zinc-200"}`}>
              2 · Responsável
            </span>
            <span aria-hidden className="text-zinc-300">›</span>
            <span className={`rounded-full px-2 py-0.5 ${passoCadastro === "comercial" ? "bg-[#1F2A35] text-white" : "bg-zinc-200"}`}>
              3 · Contrato / valores
            </span>
          </div>

          {passoCadastro === "aluno" && (
            <>
              <p className="text-sm text-zinc-500">
                Comece pelos dados do interessado. Em seguida definimos quem é o responsável financeiro.
              </p>
              <h3 className="text-sm font-semibold text-zinc-800 border-b pb-2">Dados do pré-aluno</h3>
              <div className="grid gap-4 sm:grid-cols-2">
                <Input label="Nome" value={formNovo.nome} onChange={(e) => setFormNovo((p) => ({ ...p, nome: e.target.value }))} />
                <Input
                  label="Sobrenome"
                  value={formNovo.sobrenome}
                  onChange={(e) => setFormNovo((p) => ({ ...p, sobrenome: e.target.value }))}
                />
                <Input
                  label="Data de nascimento"
                  type="date"
                  value={formNovo.dataNascimento}
                  onChange={(e) => setFormNovo((p) => ({ ...p, dataNascimento: e.target.value }))}
                />
                <div className="text-sm">
                  <p className="font-medium text-zinc-700">Idade estimada</p>
                  <p className="mt-1 text-xs text-zinc-500">
                    Calculada com base na data de hoje do seu navegador.
                  </p>
                  <p className="mt-1 text-zinc-600">
                    {idadePreAluno === null ? "Informe a data de nascimento." : `${idadePreAluno} anos`}
                  </p>
                  {idadePreAluno !== null && idadePreAluno < 18 && (
                    <p className="mt-2 rounded-lg border border-amber-200 bg-amber-50 px-3 py-2 text-xs text-amber-900">
                      Menores de 18 anos exigem na próxima etapa o cadastro de um responsável legal (pai, mãe ou
                      tutor).
                    </p>
                  )}
                </div>
                <Input
                  label={maiorOu18 ? "Celular (obrigatório se for o próprio responsável financeiro)" : "Celular (opcional)"}
                  inputMode="numeric"
                  value={applyBrazilMask("phone", formNovo.telefoneAluno ?? "")}
                  onChange={(e) =>
                    setFormNovo((p) => ({
                      ...p,
                      telefoneAluno: digitsOnly(e.target.value, 11),
                    }))
                  }
                />
                <div className="sm:col-span-2">
                  <label className="mb-1 block text-sm font-medium text-zinc-700">Livro de interesse</label>
                  <select
                    value={formNovo.livroInteresseId || ""}
                    onChange={(e) =>
                      setFormNovo((p) => ({
                        ...p,
                        livroInteresseId: Number(e.target.value) || 0,
                      }))
                    }
                    className={SELECT_FIELD}
                  >
                    <option value="">Selecione...</option>
                    {livros.map((l) => (
                      <option key={l.id} value={l.id}>
                        {l.nome}
                      </option>
                    ))}
                  </select>
                </div>
              </div>

              <div className="mt-4 flex justify-end gap-2 border-t border-zinc-200 pt-4">
                <PreAlunoCancelarBtn />
                <Button onClick={avancarEtapaAluno} disabled={!etapaAlunoValida}>
                  Continuar
                </Button>
              </div>
            </>
          )}

          {passoCadastro === "responsavel" && (
            <>
              <p className="text-sm text-zinc-500">
                {menorDeIdade
                  ? "Cadastro obrigatório do responsável financeiro pelo pré-aluno menor de idade."
                  : `O pré-aluno tem ${idadePreAluno} anos. Informe quem será o responsável financeiro pelo contrato.`}
              </p>

              {!menorDeIdade && tipoResponsavelAdulto === null && (
                <div className="grid gap-3 sm:grid-cols-2">
                  <button
                    type="button"
                    onClick={escolherProprioResponsavel}
                    className="flex flex-col items-start rounded-xl border border-zinc-200 bg-white p-4 text-left shadow-sm transition hover:border-[#1F2A35] hover:shadow"
                  >
                    <span className="font-semibold text-zinc-900">Sou o próprio responsável financeiro</span>
                    <span className="mt-1 text-xs text-zinc-500">
                      Usa nome e celular já informados. Será solicitado o CPF do pré-aluno.
                    </span>
                  </button>
                  <button
                    type="button"
                    onClick={escolherOutroResponsavel}
                    className="flex flex-col items-start rounded-xl border border-zinc-200 bg-white p-4 text-left shadow-sm transition hover:border-[#1F2A35] hover:shadow"
                  >
                    <span className="font-semibold text-zinc-900">Cadastrar outro responsável</span>
                    <span className="mt-1 text-xs text-zinc-500">Pessoa física ou jurídica que assina/financia.</span>
                  </button>
                </div>
              )}

              {menorDeIdade && (
                <h3 className="text-sm font-semibold text-zinc-800 border-b pb-2">Responsável legal obrigatório</h3>
              )}

              {!menorDeIdade && tipoResponsavelAdulto === "proprio" && (
                <div className="rounded-lg border border-blue-100 bg-blue-50/80 px-4 py-3 text-sm text-blue-900">
                  <strong>Próprio responsável:</strong> o cadastro usará nome e sobrenome do pré-aluno. Informe abaixo
                  o <strong>CPF</strong> e confirme o <strong>celular já preenchido</strong> (mínimo 10 dígitos).
                </div>
              )}

              {!menorDeIdade && tipoResponsavelAdulto === "outro" && (
                <h3 className="text-sm font-semibold text-zinc-800 border-b pb-2">Dados do responsável financeiro</h3>
              )}

              {(menorDeIdade || tipoResponsavelAdulto === "outro") && (
                <div className="grid gap-4 sm:grid-cols-2">
                  <div>
                    <label className="mb-1 block text-sm font-medium text-zinc-700">Tipo</label>
                    <select
                      value={formNovo.responsavelTipoPessoa}
                      onChange={(e) => setFormNovo((p) => ({ ...p, responsavelTipoPessoa: e.target.value }))}
                      className={SELECT_FIELD}
                    >
                      <option value="Fisica">Pessoa física</option>
                      <option value="Juridica">Pessoa jurídica</option>
                    </select>
                  </div>
                  <Input
                    label={formNovo.responsavelTipoPessoa === "Fisica" ? "CPF" : "CNPJ"}
                    inputMode="numeric"
                    value={
                      formNovo.responsavelTipoPessoa === "Fisica"
                        ? applyBrazilMask("cpf", formNovo.responsavelCpfCnpj)
                        : applyBrazilMask("cnpj", formNovo.responsavelCpfCnpj)
                    }
                    onChange={(e) =>
                      setFormNovo((p) => ({
                        ...p,
                        responsavelCpfCnpj: digitsOnly(
                          e.target.value,
                          formNovo.responsavelTipoPessoa === "Fisica" ? 11 : 14,
                        ),
                      }))}
                  />
                  <Input
                    label="Nome"
                    value={formNovo.responsavelNome}
                    onChange={(e) => setFormNovo((p) => ({ ...p, responsavelNome: e.target.value }))}
                  />
                  <Input
                    label="Sobrenome"
                    value={formNovo.responsavelSobrenome}
                    onChange={(e) => setFormNovo((p) => ({ ...p, responsavelSobrenome: e.target.value }))}
                  />
                  <Input
                    label="Telefone / WhatsApp do responsável"
                    inputMode="numeric"
                    value={applyBrazilMask("phone", formNovo.responsavelTelefone)}
                    onChange={(e) =>
                      setFormNovo((p) => ({
                        ...p,
                        responsavelTelefone: digitsOnly(e.target.value, 11),
                      }))}
                  />
                </div>
              )}

              {!menorDeIdade && tipoResponsavelAdulto === "proprio" && (
                <div className="grid gap-4 sm:grid-cols-2">
                  <Input
                    label="CPF do pré-aluno (responsável financeiro)"
                    inputMode="numeric"
                    value={applyBrazilMask("cpf", formNovo.alunoCpf ?? "")}
                    onChange={(e) =>
                      setFormNovo((p) => ({
                        ...p,
                        alunoCpf: digitsOnly(e.target.value, 11),
                      }))}
                  />
                  <div className="text-sm">
                    <p className="font-medium text-zinc-700">Celular de contato</p>
                    <p className="mt-2 text-xs text-zinc-500">
                      Usamos o campo &quot;Celular&quot; da primeira etapa. Ajuste ali se precisar.
                    </p>
                  </div>
                </div>
              )}

              <div className="mt-4 flex flex-wrap justify-between gap-2 border-t border-zinc-200 pt-4">
                <Button variant="secondary" onClick={() => setPassoCadastro("aluno")}>
                  Voltar
                </Button>
                <div className="flex gap-2">
                  {!menorDeIdade && tipoResponsavelAdulto !== null && (
                    <Button
                      variant="ghost"
                      size="sm"
                      onClick={() => {
                        setTipoResponsavelAdulto(null);
                        setFormNovo((p) => ({
                          ...p,
                          eProprioResponsavel: false,
                          alunoCpf: "",
                        }));
                      }}
                    >
                      Trocar opção
                    </Button>
                  )}
                  <Button
                    onClick={() => setPassoCadastro("comercial")}
                    disabled={!etapaResponsavelValida || (!menorDeIdade && tipoResponsavelAdulto === null)}
                  >
                    Continuar para contrato / valores
                  </Button>
                </div>
              </div>
            </>
          )}

          {passoCadastro === "comercial" && (
            <>
              <h3 className="border-b pb-2 pt-2 text-sm font-semibold text-zinc-800">
                Contrato e valores financeiros
              </h3>
              <div className="grid gap-4 sm:grid-cols-2">
                <div className="sm:col-span-2 space-y-2">
                  <label htmlFor="template-contrato-pre" className="block text-sm font-medium text-zinc-700">
                    Modelo de contrato
                  </label>
                  {contratoTemplates.length === 0 ? (
                    <p className="text-sm text-amber-700 rounded-lg border border-amber-200 bg-amber-50 px-3 py-2">
                      Nenhum modelo cadastrado. Crie modelos em{" "}
                      <a href="/contratos" className="font-medium underline">
                        Contratos
                      </a>{" "}
                      antes de registrar o pré-aluno.
                    </p>
                  ) : (
                    <>
                      <select
                        id="template-contrato-pre"
                        value={templateContratoId === "" ? "" : String(templateContratoId)}
                        onChange={(e) =>
                          setTemplateContratoId(e.target.value ? Number(e.target.value) : "")}
                        className={SELECT_FIELD}
                      >
                        <option value="">Selecione o modelo…</option>
                        {contratoTemplates.map((t) => (
                          <option key={t.id} value={t.id}>
                            {t.nome}
                            {t.ativo ? " (ativo)" : ""}
                            {` · v${t.versao}`}
                          </option>
                        ))}
                      </select>
                      <p className="text-xs text-zinc-500">
                        O nome do modelo será salvo na ficha do pré-aluno e usado na geração do contrato.
                      </p>
                    </>
                  )}
                </div>
                <Input
                  label="Valor mensalidade (R$)"
                  type="number"
                  min={0}
                  step="0.01"
                  value={formNovo.valorMensalidade || ""}
                  onChange={(e) =>
                    setFormNovo((p) => ({
                      ...p,
                      valorMensalidade: Number(e.target.value.replace(",", ".")) || 0,
                    }))
                  }
                />
                <div>
                  <label className="mb-1 block text-sm font-medium text-zinc-700">
                    Forma de pagamento da mensalidade{" "}
                    <span className="font-normal text-zinc-500">(opcional)</span>
                  </label>
                  <select
                    value={formNovo.formaPagamento ?? ""}
                    onChange={(e) =>
                      setFormNovo((p) => ({
                        ...p,
                        formaPagamento: e.target.value,
                      }))}
                    className={SELECT_FIELD}
                  >
                    <option value="">— Não informar —</option>
                    {FORMAS_PAGAMENTO_OPCOES.map((f) => (
                      <option key={f} value={f}>
                        {f}
                      </option>
                    ))}
                  </select>
                </div>
                <Input
                  label="Valor total do livro/material didático cobrado (R$)"
                  helperText='Use 0 (zero) se for gratuito ou não aplicável.'
                  type="number"
                  min={0}
                  step="0.01"
                  value={formNovo.valorMaterial ?? ""}
                  onChange={(e) =>
                    setFormNovo((p) => ({
                      ...p,
                      valorMaterial:
                        e.target.value === ""
                          ? 0
                          : Math.max(0, Number(e.target.value.replace(",", "."))),
                    }))}
                />
                <Input
                  label="Valor da matrícula (R$)"
                  type="number"
                  min={0}
                  step="0.01"
                  value={formNovo.valorMatricula ?? ""}
                  onChange={(e) =>
                    setFormNovo((p) => ({
                      ...p,
                      valorMatricula:
                        e.target.value === ""
                          ? 0
                          : Math.max(0, Number(e.target.value.replace(",", "."))),
                    }))}
                />
                <div>
                  <label className="mb-1 block text-sm font-medium text-zinc-700">
                    Forma de pagamento da matrícula
                    {Number(formNovo.valorMatricula) > 0 ? (
                      <span className="text-red-600"> *</span>
                    ) : null}
                  </label>
                  <select
                    value={formNovo.formaPagamentoMatricula ?? ""}
                    onChange={(e) =>
                      setFormNovo((p) => ({
                        ...p,
                        formaPagamentoMatricula: e.target.value,
                      }))}
                    disabled={!(Number(formNovo.valorMatricula) > 0)}
                    className={SELECT_FIELD}
                  >
                    <option value="">— Escolha se houver valor de matrícula —</option>
                    {FORMAS_PAGAMENTO_OPCOES.map((f) => (
                      <option key={f} value={f}>
                        {f}
                      </option>
                    ))}
                  </select>
                </div>
              </div>

              <h3 className="mt-2 border-b pb-2 pt-4 text-sm font-semibold text-zinc-800">Prospecção</h3>
              <div className="grid gap-4 sm:grid-cols-2">
                <div className="sm:col-span-2">
                  <label className="mb-1 block text-sm font-medium text-zinc-700">
                    De onde esse aluno/prospect veio?<span className="text-red-600">*</span>
                  </label>
                  <select
                    value={formNovo.origemCaptacao}
                    onChange={(e) =>
                      setFormNovo((p) => ({
                        ...p,
                        origemCaptacao: e.target.value,
                      }))}
                    className={SELECT_FIELD}
                  >
                    <option value="">Selecione…</option>
                    {ORIGEM_CAPTACAO_OPCOES.map((o) => (
                      <option key={o} value={o}>
                        {o}
                      </option>
                    ))}
                  </select>
                </div>
              </div>

              <h3 className="mt-2 border-b pb-2 pt-4 text-sm font-semibold text-zinc-800">Transporte</h3>
              <div className="grid gap-4 sm:grid-cols-2">
                <div className="sm:col-span-2">
                  <label className="mb-1 block text-sm font-medium text-zinc-700">Transporte por van escolar?</label>
                  <select
                    value={formNovo.usaTransporteVan ? "sim" : "nao"}
                    onChange={(e) => {
                      const sim = e.target.value === "sim";
                      setFormNovo((p) => ({
                        ...p,
                        usaTransporteVan: sim,
                        ...(sim
                          ? {}
                          : {
                              transporteCep: "",
                              transporteLogradouro: "",
                              transporteNumero: "",
                              transporteComplemento: "",
                              transporteBairro: "",
                              transporteCidade: "",
                              transporteUf: "",
                            }),
                      }));
                    }}
                    className={SELECT_FIELD}
                  >
                    <option value="nao">Não</option>
                    <option value="sim">Sim — preciso de van</option>
                  </select>
                  {formNovo.usaTransporteVan ? (
                    <p className="mt-2 text-xs text-zinc-500">
                      Informe endereço completo para logística da van. Você pode preencher o CEP e buscar dados.
                    </p>
                  ) : null}
                </div>

                {formNovo.usaTransporteVan ? (
                  <>
                    <div className="flex flex-col gap-2 sm:col-span-2">
                      <label className="text-sm font-medium text-zinc-700">CEP</label>
                      <div className="flex flex-wrap gap-2">
                        <input
                          className={SELECT_FIELD.replace(" w-full ", " flex-1 min-w-[140px] ")}
                          inputMode="numeric"
                          placeholder="00000-000"
                          value={applyBrazilMask("cep", formNovo.transporteCep ?? "")}
                          onChange={(e) =>
                            setFormNovo((p) => ({
                              ...p,
                              transporteCep: digitsOnly(e.target.value, 8),
                            }))}
                        />
                        <Button
                          type="button"
                          variant="secondary"
                          onClick={() => void buscarCepTransporteVan()}
                          disabled={digitsOnly(formNovo.transporteCep ?? "").length !== 8 || cepVanBuscando}
                          isLoading={cepVanBuscando}
                        >
                          Buscar CEP
                        </Button>
                      </div>
                    </div>
                    <Input
                      label="Logradouro *"
                      value={formNovo.transporteLogradouro ?? ""}
                      onChange={(e) =>
                        setFormNovo((p) => ({ ...p, transporteLogradouro: e.target.value }))}
                    />
                    <Input
                      label="Número *"
                      value={formNovo.transporteNumero ?? ""}
                      onChange={(e) =>
                        setFormNovo((p) => ({ ...p, transporteNumero: e.target.value }))}
                    />
                    <Input
                      label="Complemento"
                      value={formNovo.transporteComplemento ?? ""}
                      onChange={(e) =>
                        setFormNovo((p) => ({ ...p, transporteComplemento: e.target.value }))}
                    />
                    <Input
                      label="Bairro *"
                      value={formNovo.transporteBairro ?? ""}
                      onChange={(e) =>
                        setFormNovo((p) => ({ ...p, transporteBairro: e.target.value }))}
                    />
                    <Input
                      label="Cidade *"
                      value={formNovo.transporteCidade ?? ""}
                      onChange={(e) =>
                        setFormNovo((p) => ({ ...p, transporteCidade: e.target.value }))}
                    />
                    <Input
                      label="UF * (2 letras)"
                      inputMode="text"
                      maxLength={2}
                      value={(formNovo.transporteUf ?? "").slice(0, 2)}
                      onChange={(e) =>
                        setFormNovo((p) => ({
                          ...p,
                          transporteUf: e.target.value.toUpperCase(),
                        }))}
                    />
                  </>
                ) : null}
              </div>

              <div className="mt-4 sm:col-span-2">
                <label className="mb-1 block text-sm font-medium text-zinc-700">Observações comerciais</label>
                <textarea
                  value={formNovo.observacoesComerciais ?? ""}
                  onChange={(e) =>
                    setFormNovo((p) => ({
                      ...p,
                      observacoesComerciais: e.target.value,
                    }))}
                  rows={3}
                  className="w-full resize-none rounded-lg border border-zinc-300 bg-white p-3 text-sm text-zinc-900 outline-none focus:border-[#1F2A35] focus:ring-2 focus:ring-[#1F2A35]/15"
                  placeholder='Detalhes da negociação, combinações com "Origem Outro", etc.'
                />
              </div>

              <div className="mt-4 flex flex-wrap justify-between gap-2 border-t border-zinc-200 pt-4">
                <Button variant="secondary" onClick={() => setPassoCadastro("responsavel")} disabled={salvando}>
                  Voltar
                </Button>
                <div className="flex gap-2">
                  <PreAlunoCancelarBtn disabled={salvando} />
                  <Button
                    onClick={() => void salvarNovo()}
                    disabled={salvando || !podeSalvarTudo}
                    isLoading={salvando}
                  >
                    Salvar pré-aluno
                  </Button>
                </div>
              </div>
            </>
          )}
        </div>
      </Modal>
    </div>
  );
}
