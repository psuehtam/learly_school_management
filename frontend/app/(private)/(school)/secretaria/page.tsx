"use client";

import { useCallback, useMemo, useState } from "react";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Card, CardHeader, CardTitle } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Modal } from "@/components/ui/modal";
import {
  getApiErrorMessage,
  criarAlunoComMatricula,
  listarMatriculas,
  cancelarMatriculaById,
  vincularTurmaMatricula,
  type MatriculaListItem,
  type MatriculaStatus,
} from "@/lib/api";

const STATUS_ABAS: Array<{ id: "espera" | "canceladas" | "todas"; label: string; status?: MatriculaStatus }> = [
  { id: "espera", label: "Alunos em Espera", status: "Em Espera" },
  { id: "canceladas", label: "Canceladas", status: "Cancelado" },
  { id: "todas", label: "Todas as Matriculas" },
];

type AbaId = (typeof STATUS_ABAS)[number]["id"];

type NovoAlunoForm = {
  eProprioResponsavel: boolean;
  nome: string;
  sobrenome: string;
  sexo: "Masculino" | "Feminino" | "Outro";
  dataNascimento: string;
  dataIngresso: string;
  cpf: string;
  cep: string;
  tipoLogradouro: "Rua" | "Avenida" | "Travessa" | "Alameda" | "Estrada" | "Rodovia" | "Outro";
  logradouro: string;
  numero: string;
  complemento: string;
  bairro: string;
  municipio: string;
  responsavelNome: string;
  responsavelSobrenome: string;
  responsavelCpf: string;
  responsavelSexo: "Masculino" | "Feminino" | "Outro";
  responsavelCep: string;
  responsavelTipoLogradouro: "Rua" | "Avenida" | "Travessa" | "Alameda" | "Estrada" | "Rodovia" | "Outro";
  responsavelLogradouro: string;
  responsavelNumero: string;
  responsavelComplemento: string;
  responsavelBairro: string;
  responsavelMunicipio: string;
};

const emptyNovoAlunoForm: NovoAlunoForm = {
  eProprioResponsavel: false,
  nome: "",
  sobrenome: "",
  sexo: "Masculino",
  dataNascimento: "",
  dataIngresso: "",
  cpf: "",
  cep: "",
  tipoLogradouro: "Rua",
  logradouro: "",
  numero: "",
  complemento: "",
  bairro: "",
  municipio: "",
  responsavelNome: "",
  responsavelSobrenome: "",
  responsavelCpf: "",
  responsavelSexo: "Feminino",
  responsavelCep: "",
  responsavelTipoLogradouro: "Rua",
  responsavelLogradouro: "",
  responsavelNumero: "",
  responsavelComplemento: "",
  responsavelBairro: "",
  responsavelMunicipio: "",
};

function formatDate(value: string): string {
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return "-";
  return date.toLocaleDateString("pt-BR");
}

function formatDateTime(value: string): string {
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return "-";
  return date.toLocaleString("pt-BR", {
    day: "2-digit",
    month: "2-digit",
    year: "numeric",
    hour: "2-digit",
    minute: "2-digit",
  });
}

function calcularIdade(dataNascimentoIso: string): number | null {
  if (!dataNascimentoIso) return null;
  const hoje = new Date();
  const nascimento = new Date(dataNascimentoIso);
  if (Number.isNaN(nascimento.getTime())) return null;

  let idade = hoje.getFullYear() - nascimento.getFullYear();
  const m = hoje.getMonth() - nascimento.getMonth();
  if (m < 0 || (m === 0 && hoje.getDate() < nascimento.getDate())) {
    idade--;
  }

  return idade;
}

export default function SecretariaPage() {
  const [aba, setAba] = useState<AbaId>("espera");
  const [busca, setBusca] = useState("");
  const [alunoIdFiltro, setAlunoIdFiltro] = useState("");
  const [matriculas, setMatriculas] = useState<MatriculaListItem[]>([]);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const [modalNovoAlunoOpen, setModalNovoAlunoOpen] = useState(false);
  const [novoAlunoForm, setNovoAlunoForm] = useState<NovoAlunoForm>(emptyNovoAlunoForm);
  const [etapaCadastro, setEtapaCadastro] = useState<"aluno" | "responsavel" | "outros">("aluno");
  const [savingAluno, setSavingAluno] = useState(false);

  const [matriculaParaEnturmar, setMatriculaParaEnturmar] = useState<MatriculaListItem | null>(null);
  const [turmaIdEnturmar, setTurmaIdEnturmar] = useState("");
  const [savingEnturmar, setSavingEnturmar] = useState(false);

  const statusSelecionado = STATUS_ABAS.find((s) => s.id === aba)?.status;
  const idadeAluno = calcularIdade(novoAlunoForm.dataNascimento);
  const alunoMenor = idadeAluno !== null && idadeAluno < 18;

  const carregarMatriculas = useCallback(async () => {
    setIsLoading(true);
    setError(null);
    try {
      const filtroAlunoId = alunoIdFiltro.trim() ? Number(alunoIdFiltro) : undefined;
      const data = await listarMatriculas({
        status: statusSelecionado,
        alunoId: Number.isFinite(filtroAlunoId as number) ? filtroAlunoId : undefined,
      });
      setMatriculas(data);
    } catch (e) {
      setError(getApiErrorMessage(e, "Falha ao carregar matriculas."));
    } finally {
      setIsLoading(false);
    }
  }, [alunoIdFiltro, statusSelecionado]);

  const matriculasFiltradas = useMemo(() => {
    const termo = busca.trim().toLowerCase();
    if (!termo) return matriculas;
    return matriculas.filter((m) =>
      `${m.id} ${m.alunoId} ${m.turmaId ?? ""} ${m.status}`.toLowerCase().includes(termo),
    );
  }, [matriculas, busca]);

  const abrirModalNovoAluno = () => {
    setNovoAlunoForm(emptyNovoAlunoForm);
    setEtapaCadastro("aluno");
    setModalNovoAlunoOpen(true);
  };

  const salvarNovoAluno = async () => {
    if (!novoAlunoForm.nome.trim() || !novoAlunoForm.sobrenome.trim()) {
      setError("Nome e sobrenome sao obrigatorios.");
      return;
    }

    if (!novoAlunoForm.dataNascimento || !novoAlunoForm.dataIngresso) {
      setError("Data de nascimento e data de ingresso sao obrigatorias.");
      return;
    }

    if (
      !novoAlunoForm.cep.trim() ||
      !novoAlunoForm.tipoLogradouro.trim() ||
      !novoAlunoForm.logradouro.trim() ||
      !novoAlunoForm.numero.trim() ||
      !novoAlunoForm.bairro.trim() ||
      !novoAlunoForm.municipio.trim()
    ) {
      setError("Preencha o endereco completo do aluno.");
      return;
    }

    if (alunoMenor && novoAlunoForm.eProprioResponsavel) {
      setError("Aluno menor de idade nao pode ser marcado como proprio responsavel.");
      return;
    }

    if (novoAlunoForm.eProprioResponsavel && !novoAlunoForm.cpf.trim()) {
      setError("CPF e obrigatorio quando o aluno e o proprio responsavel.");
      return;
    }

    if (!novoAlunoForm.eProprioResponsavel) {
      if (
        !novoAlunoForm.responsavelNome.trim() ||
        !novoAlunoForm.responsavelSobrenome.trim() ||
        !novoAlunoForm.responsavelCpf.trim() ||
        !novoAlunoForm.responsavelCep.trim() ||
        !novoAlunoForm.responsavelTipoLogradouro.trim() ||
        !novoAlunoForm.responsavelLogradouro.trim() ||
        !novoAlunoForm.responsavelNumero.trim() ||
        !novoAlunoForm.responsavelBairro.trim() ||
        !novoAlunoForm.responsavelMunicipio.trim()
      ) {
        setError("Preencha os dados e endereco completos do responsavel.");
        return;
      }
    }

    setSavingAluno(true);
    setError(null);
    try {
      await criarAlunoComMatricula({
        eProprioResponsavel: novoAlunoForm.eProprioResponsavel,
        nome: novoAlunoForm.nome.trim(),
        sobrenome: novoAlunoForm.sobrenome.trim(),
        sexo: novoAlunoForm.sexo,
        dataNascimento: novoAlunoForm.dataNascimento,
        dataIngresso: novoAlunoForm.dataIngresso,
        cpf: novoAlunoForm.cpf.trim() || undefined,
        cep: novoAlunoForm.cep.trim(),
        tipoLogradouro: novoAlunoForm.tipoLogradouro,
        logradouro: novoAlunoForm.logradouro.trim(),
        numero: novoAlunoForm.numero.trim(),
        complemento: novoAlunoForm.complemento.trim() || undefined,
        bairro: novoAlunoForm.bairro.trim(),
        municipio: novoAlunoForm.municipio.trim(),
        responsavelNome: novoAlunoForm.eProprioResponsavel ? undefined : novoAlunoForm.responsavelNome.trim(),
        responsavelSobrenome: novoAlunoForm.eProprioResponsavel ? undefined : novoAlunoForm.responsavelSobrenome.trim(),
        responsavelCpf: novoAlunoForm.eProprioResponsavel ? undefined : novoAlunoForm.responsavelCpf.trim(),
        responsavelSexo: novoAlunoForm.eProprioResponsavel ? undefined : novoAlunoForm.responsavelSexo,
        responsavelCep: novoAlunoForm.eProprioResponsavel ? undefined : novoAlunoForm.responsavelCep.trim(),
        responsavelTipoLogradouro: novoAlunoForm.eProprioResponsavel ? undefined : novoAlunoForm.responsavelTipoLogradouro,
        responsavelLogradouro: novoAlunoForm.eProprioResponsavel ? undefined : novoAlunoForm.responsavelLogradouro.trim(),
        responsavelNumero: novoAlunoForm.eProprioResponsavel ? undefined : novoAlunoForm.responsavelNumero.trim(),
        responsavelComplemento: novoAlunoForm.eProprioResponsavel ? undefined : novoAlunoForm.responsavelComplemento.trim() || undefined,
        responsavelBairro: novoAlunoForm.eProprioResponsavel ? undefined : novoAlunoForm.responsavelBairro.trim(),
        responsavelMunicipio: novoAlunoForm.eProprioResponsavel ? undefined : novoAlunoForm.responsavelMunicipio.trim(),
      });

      setModalNovoAlunoOpen(false);
      await carregarMatriculas();
    } catch (e) {
      setError(getApiErrorMessage(e, "Falha ao criar aluno."));
    } finally {
      setSavingAluno(false);
    }
  };

  const enturmarMatricula = async () => {
    if (!matriculaParaEnturmar) return;

    const turmaId = Number(turmaIdEnturmar);
    if (!Number.isFinite(turmaId) || turmaId <= 0) {
      setError("Informe um TurmaId valido.");
      return;
    }

    setSavingEnturmar(true);
    setError(null);
    try {
      await vincularTurmaMatricula(matriculaParaEnturmar.id, turmaId);
      setMatriculaParaEnturmar(null);
      setTurmaIdEnturmar("");
      await carregarMatriculas();
    } catch (e) {
      setError(getApiErrorMessage(e, "Falha ao vincular turma."));
    } finally {
      setSavingEnturmar(false);
    }
  };

  const onCancelarMatricula = async (m: MatriculaListItem) => {
    if (!confirm(`Cancelar matricula #${m.id}?`)) return;

    setError(null);
    try {
      await cancelarMatriculaById(m.id);
      await carregarMatriculas();
    } catch (e) {
      setError(getApiErrorMessage(e, "Falha ao cancelar matricula."));
    }
  };

  return (
    <div className="flex flex-col gap-6">
      <div className="flex items-center justify-between gap-4">
        <div>
          <h1 className="text-xl font-semibold text-zinc-900">Secretaria</h1>
          <p className="text-sm text-zinc-500">Cadastro de alunos e gestao de matriculas</p>
        </div>
        <div className="flex items-center gap-2">
          <Button variant="secondary" onClick={carregarMatriculas} isLoading={isLoading}>Atualizar</Button>
          <Button onClick={abrirModalNovoAluno}>Novo Aluno</Button>
        </div>
      </div>

      <Card className="p-4">
        <div className="flex flex-wrap items-center gap-2">
          {STATUS_ABAS.map((item) => (
            <button
              key={item.id}
              onClick={() => setAba(item.id)}
              className={`h-9 rounded-lg px-4 text-sm font-medium transition-colors ${
                aba === item.id
                  ? "bg-[#1F2A35] text-white"
                  : "border border-zinc-200 bg-white text-zinc-700 hover:bg-zinc-50"
              }`}
            >
              {item.label}
            </button>
          ))}
        </div>
      </Card>

      <Card className="p-4">
        <div className="grid grid-cols-1 gap-3 md:grid-cols-3">
          <Input
            label="Busca rápida"
            placeholder="ID, alunoId, turmaId ou status"
            value={busca}
            onChange={(e) => setBusca(e.target.value)}
          />
          <Input
            label="Filtrar por AlunoId"
            placeholder="Ex.: 123"
            value={alunoIdFiltro}
            onChange={(e) => setAlunoIdFiltro(e.target.value)}
          />
          <div className="flex items-end">
            <Button className="w-full" onClick={carregarMatriculas} isLoading={isLoading}>Aplicar filtros</Button>
          </div>
        </div>
      </Card>

      {error && (
        <div className="rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700">
          {error}
        </div>
      )}

      <Card>
        <CardHeader className="mb-3">
          <CardTitle className="text-base">Matriculas ({matriculasFiltradas.length})</CardTitle>
        </CardHeader>

        <div className="overflow-x-auto">
          <table className="w-full min-w-[760px] text-sm">
            <thead>
              <tr className="border-b border-zinc-200 text-left text-xs uppercase tracking-wide text-zinc-500">
                <th className="px-3 py-2">Matricula</th>
                <th className="px-3 py-2">Aluno</th>
                <th className="px-3 py-2">Turma</th>
                <th className="px-3 py-2">Status</th>
                <th className="px-3 py-2">Data</th>
                <th className="px-3 py-2 text-right">Acoes</th>
              </tr>
            </thead>
            <tbody>
              {!isLoading && matriculasFiltradas.length === 0 ? (
                <tr>
                  <td colSpan={6} className="px-3 py-10 text-center text-zinc-400">
                    Nenhuma matricula encontrada.
                  </td>
                </tr>
              ) : (
                matriculasFiltradas.map((m) => (
                  <tr key={m.id} className="border-b border-zinc-100 hover:bg-zinc-50/70">
                    <td className="px-3 py-3 font-medium text-zinc-800">#{m.id}</td>
                    <td className="px-3 py-3 text-zinc-700">Aluno #{m.alunoId}</td>
                    <td className="px-3 py-3 text-zinc-700">{m.turmaId ? `Turma #${m.turmaId}` : "Nao definida"}</td>
                    <td className="px-3 py-3"><Badge>{m.status}</Badge></td>
                    <td className="px-3 py-3 text-zinc-600">
                      <div>{formatDate(m.dataMatricula)}</div>
                      <div className="text-xs text-zinc-400">Atualizado: {formatDateTime(m.dataAtualizacao)}</div>
                    </td>
                    <td className="px-3 py-3">
                      <div className="flex justify-end gap-2">
                        <Button
                          size="sm"
                          variant="secondary"
                          onClick={() => {
                            setMatriculaParaEnturmar(m);
                            setTurmaIdEnturmar(m.turmaId ? String(m.turmaId) : "");
                          }}
                          disabled={m.status !== "Em Espera"}
                        >
                          Enturmar
                        </Button>
                        <Button size="sm" variant="danger" onClick={() => void onCancelarMatricula(m)} disabled={m.status === "Cancelado"}>
                          Cancelar
                        </Button>
                      </div>
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>
      </Card>

      <Modal
        open={modalNovoAlunoOpen}
        onClose={() => setModalNovoAlunoOpen(false)}
        title="Novo aluno"
        footer={
          <>
            <Button variant="secondary" onClick={() => setModalNovoAlunoOpen(false)}>Fechar</Button>
            {etapaCadastro !== "aluno" && (
              <Button
                variant="secondary"
                onClick={() =>
                  setEtapaCadastro((prev) => (prev === "outros" ? (novoAlunoForm.eProprioResponsavel ? "aluno" : "responsavel") : "aluno"))
                }
              >
                Voltar
              </Button>
            )}
            {etapaCadastro === "aluno" && (
              <Button
                onClick={() => {
                  if (
                    !novoAlunoForm.nome.trim() ||
                    !novoAlunoForm.sobrenome.trim() ||
                    !novoAlunoForm.dataNascimento ||
                    !novoAlunoForm.sexo ||
                    !novoAlunoForm.cep.trim() ||
                    !novoAlunoForm.tipoLogradouro.trim() ||
                    !novoAlunoForm.logradouro.trim() ||
                    !novoAlunoForm.numero.trim() ||
                    !novoAlunoForm.bairro.trim() ||
                    !novoAlunoForm.municipio.trim()
                  ) {
                    setError("Preencha dados pessoais e endereco obrigatorios do aluno para continuar.");
                    return;
                  }

                  if (alunoMenor || !novoAlunoForm.eProprioResponsavel) {
                    setEtapaCadastro("responsavel");
                  } else {
                    setEtapaCadastro("outros");
                  }
                }}
              >
                Proximo
              </Button>
            )}
            {etapaCadastro === "responsavel" && (
              <Button
                onClick={() => {
                  if (
                    !novoAlunoForm.responsavelNome.trim() ||
                    !novoAlunoForm.responsavelSobrenome.trim() ||
                    !novoAlunoForm.responsavelCpf.trim() ||
                    !novoAlunoForm.responsavelCep.trim() ||
                    !novoAlunoForm.responsavelLogradouro.trim() ||
                    !novoAlunoForm.responsavelNumero.trim() ||
                    !novoAlunoForm.responsavelBairro.trim() ||
                    !novoAlunoForm.responsavelMunicipio.trim()
                  ) {
                    setError("Preencha os dados obrigatorios do responsavel, incluindo endereco.");
                    return;
                  }

                  setEtapaCadastro("outros");
                }}
              >
                Proximo
              </Button>
            )}
            {etapaCadastro === "outros" && (
              <Button onClick={salvarNovoAluno} isLoading={savingAluno}>Salvar aluno</Button>
            )}
          </>
        }
      >
        <div className="mb-4 flex items-center gap-2 text-xs">
          <Badge variant={etapaCadastro === "aluno" ? "info" : "muted"}>1. Dados do aluno</Badge>
          <span className="text-zinc-400">/</span>
          <Badge variant={etapaCadastro === "responsavel" ? "info" : "muted"}>2. Responsavel</Badge>
          <span className="text-zinc-400">/</span>
          <Badge variant={etapaCadastro === "outros" ? "info" : "muted"}>3. Outros dados</Badge>
        </div>

        {etapaCadastro === "aluno" && (
          <div className="space-y-4">
            <div className="rounded-xl border border-zinc-200 bg-zinc-50/80 p-4">
              <h3 className="mb-3 text-sm font-semibold text-zinc-800">Dados pessoais</h3>
              <div className="grid grid-cols-1 gap-3 md:grid-cols-2">
                <Input label="Nome" value={novoAlunoForm.nome} onChange={(e) => setNovoAlunoForm((p) => ({ ...p, nome: e.target.value }))} required />
                <Input label="Sobrenome" value={novoAlunoForm.sobrenome} onChange={(e) => setNovoAlunoForm((p) => ({ ...p, sobrenome: e.target.value }))} required />
                <div className="flex flex-col gap-1">
                  <label className="text-sm font-medium text-zinc-700">Sexo</label>
                  <select
                    className="h-10 rounded-lg border border-zinc-200 px-3 text-sm outline-none focus:border-[#1F2A35]"
                    value={novoAlunoForm.sexo}
                    onChange={(e) => setNovoAlunoForm((p) => ({ ...p, sexo: e.target.value as NovoAlunoForm["sexo"] }))}
                  >
                    <option value="Masculino">Masculino</option>
                    <option value="Feminino">Feminino</option>
                    <option value="Outro">Outro</option>
                  </select>
                </div>
                <Input
                  label="Data de nascimento"
                  type="date"
                  value={novoAlunoForm.dataNascimento}
                  required
                  onChange={(e) => {
                    const novaData = e.target.value;
                    const idade = calcularIdade(novaData);
                    setNovoAlunoForm((p) => ({
                      ...p,
                      dataNascimento: novaData,
                      eProprioResponsavel: idade !== null && idade < 18 ? false : p.eProprioResponsavel,
                    }));
                  }}
                />
              </div>
            </div>
            <div className="rounded-xl border border-zinc-200 bg-white p-4">
              <h3 className="mb-3 text-sm font-semibold text-zinc-800">Endereco do aluno</h3>
              <div className="grid grid-cols-1 gap-3 md:grid-cols-3">
                <Input label="CEP" value={novoAlunoForm.cep} onChange={(e) => setNovoAlunoForm((p) => ({ ...p, cep: e.target.value }))} required />
                <div className="flex flex-col gap-1">
                  <label className="text-sm font-medium text-zinc-700">Tipo de logradouro</label>
                  <select
                    className="h-10 rounded-lg border border-zinc-200 px-3 text-sm outline-none focus:border-[#1F2A35]"
                    value={novoAlunoForm.tipoLogradouro}
                    onChange={(e) => setNovoAlunoForm((p) => ({ ...p, tipoLogradouro: e.target.value as NovoAlunoForm["tipoLogradouro"] }))}
                  >
                    <option value="Rua">Rua</option>
                    <option value="Avenida">Avenida</option>
                    <option value="Travessa">Travessa</option>
                    <option value="Alameda">Alameda</option>
                    <option value="Estrada">Estrada</option>
                    <option value="Rodovia">Rodovia</option>
                    <option value="Outro">Outro</option>
                  </select>
                </div>
                <Input label="Numero" value={novoAlunoForm.numero} onChange={(e) => setNovoAlunoForm((p) => ({ ...p, numero: e.target.value }))} required />
                <div className="md:col-span-2">
                  <Input label="Logradouro" value={novoAlunoForm.logradouro} onChange={(e) => setNovoAlunoForm((p) => ({ ...p, logradouro: e.target.value }))} required />
                </div>
                <Input label="Complemento" value={novoAlunoForm.complemento} onChange={(e) => setNovoAlunoForm((p) => ({ ...p, complemento: e.target.value }))} />
                <Input label="Bairro" value={novoAlunoForm.bairro} onChange={(e) => setNovoAlunoForm((p) => ({ ...p, bairro: e.target.value }))} required />
                <Input label="Municipio" value={novoAlunoForm.municipio} onChange={(e) => setNovoAlunoForm((p) => ({ ...p, municipio: e.target.value }))} required />
              </div>
            </div>
            <div className="rounded-lg border border-zinc-200 bg-zinc-50 px-3 py-2 text-sm text-zinc-700">
              {alunoMenor
                ? "Aluno menor de idade: o proximo passo sera o cadastro do responsavel."
                : "Aluno maior de idade: voce pode definir se ele sera o proprio responsavel."}
            </div>
            {!alunoMenor && (
              <label className="inline-flex items-center gap-2 text-sm text-zinc-700">
                <input
                  type="checkbox"
                  className="h-4 w-4"
                  checked={novoAlunoForm.eProprioResponsavel}
                  onChange={(e) => setNovoAlunoForm((p) => ({ ...p, eProprioResponsavel: e.target.checked }))}
                />
                Aluno e o proprio responsavel
              </label>
            )}
          </div>
        )}

        {etapaCadastro === "responsavel" && (
          <div className="space-y-4">
            <div className="rounded-xl border border-zinc-200 bg-zinc-50/80 p-4">
              <h3 className="mb-3 text-sm font-semibold text-zinc-800">Dados do responsavel</h3>
              <div className="grid grid-cols-1 gap-3 md:grid-cols-2">
                <Input label="Nome do responsavel" value={novoAlunoForm.responsavelNome} onChange={(e) => setNovoAlunoForm((p) => ({ ...p, responsavelNome: e.target.value }))} required />
                <Input label="Sobrenome do responsavel" value={novoAlunoForm.responsavelSobrenome} onChange={(e) => setNovoAlunoForm((p) => ({ ...p, responsavelSobrenome: e.target.value }))} required />
                <Input label="CPF do responsavel" value={novoAlunoForm.responsavelCpf} onChange={(e) => setNovoAlunoForm((p) => ({ ...p, responsavelCpf: e.target.value }))} required />
                <div className="flex flex-col gap-1">
                  <label className="text-sm font-medium text-zinc-700">Sexo do responsavel</label>
                  <select
                    className="h-10 rounded-lg border border-zinc-200 px-3 text-sm outline-none focus:border-[#1F2A35]"
                    value={novoAlunoForm.responsavelSexo}
                    onChange={(e) => setNovoAlunoForm((p) => ({ ...p, responsavelSexo: e.target.value as NovoAlunoForm["responsavelSexo"] }))}
                  >
                    <option value="Masculino">Masculino</option>
                    <option value="Feminino">Feminino</option>
                    <option value="Outro">Outro</option>
                  </select>
                </div>
              </div>
            </div>
            <div className="rounded-xl border border-zinc-200 bg-white p-4">
              <h3 className="mb-3 text-sm font-semibold text-zinc-800">Endereco do responsavel</h3>
              <div className="grid grid-cols-1 gap-3 md:grid-cols-3">
                <Input label="CEP" value={novoAlunoForm.responsavelCep} onChange={(e) => setNovoAlunoForm((p) => ({ ...p, responsavelCep: e.target.value }))} required />
                <div className="flex flex-col gap-1">
                  <label className="text-sm font-medium text-zinc-700">Tipo de logradouro</label>
                  <select
                    className="h-10 rounded-lg border border-zinc-200 px-3 text-sm outline-none focus:border-[#1F2A35]"
                    value={novoAlunoForm.responsavelTipoLogradouro}
                    onChange={(e) =>
                      setNovoAlunoForm((p) => ({ ...p, responsavelTipoLogradouro: e.target.value as NovoAlunoForm["responsavelTipoLogradouro"] }))
                    }
                  >
                    <option value="Rua">Rua</option>
                    <option value="Avenida">Avenida</option>
                    <option value="Travessa">Travessa</option>
                    <option value="Alameda">Alameda</option>
                    <option value="Estrada">Estrada</option>
                    <option value="Rodovia">Rodovia</option>
                    <option value="Outro">Outro</option>
                  </select>
                </div>
                <Input label="Numero" value={novoAlunoForm.responsavelNumero} onChange={(e) => setNovoAlunoForm((p) => ({ ...p, responsavelNumero: e.target.value }))} required />
                <div className="md:col-span-2">
                  <Input label="Logradouro" value={novoAlunoForm.responsavelLogradouro} onChange={(e) => setNovoAlunoForm((p) => ({ ...p, responsavelLogradouro: e.target.value }))} required />
                </div>
                <Input label="Complemento" value={novoAlunoForm.responsavelComplemento} onChange={(e) => setNovoAlunoForm((p) => ({ ...p, responsavelComplemento: e.target.value }))} />
                <Input label="Bairro" value={novoAlunoForm.responsavelBairro} onChange={(e) => setNovoAlunoForm((p) => ({ ...p, responsavelBairro: e.target.value }))} required />
                <Input label="Municipio" value={novoAlunoForm.responsavelMunicipio} onChange={(e) => setNovoAlunoForm((p) => ({ ...p, responsavelMunicipio: e.target.value }))} required />
              </div>
            </div>
          </div>
        )}

        {etapaCadastro === "outros" && (
          <div className="grid grid-cols-1 gap-3 md:grid-cols-2">
            <Input label="CPF do aluno" value={novoAlunoForm.cpf} onChange={(e) => setNovoAlunoForm((p) => ({ ...p, cpf: e.target.value }))} helperText={novoAlunoForm.eProprioResponsavel ? "Obrigatorio quando o aluno e o proprio responsavel." : undefined} />
            <Input label="Data de ingresso" type="date" value={novoAlunoForm.dataIngresso} onChange={(e) => setNovoAlunoForm((p) => ({ ...p, dataIngresso: e.target.value }))} />
            <div className="md:col-span-2 rounded-lg border border-zinc-200 bg-zinc-50 px-3 py-2 text-sm text-zinc-700">
              Ao salvar: o sistema cria o aluno e abre uma matricula em <strong>Em Espera</strong>.
            </div>
          </div>
        )}
      </Modal>

      <Modal
        open={!!matriculaParaEnturmar}
        onClose={() => setMatriculaParaEnturmar(null)}
        title={matriculaParaEnturmar ? `Enturmar matricula #${matriculaParaEnturmar.id}` : "Enturmar"}
        footer={
          <>
            <Button variant="secondary" onClick={() => setMatriculaParaEnturmar(null)}>Fechar</Button>
            <Button onClick={enturmarMatricula} isLoading={savingEnturmar}>Vincular turma</Button>
          </>
        }
      >
        <div className="space-y-3">
          <p className="text-sm text-zinc-600">
            Como o modulo de turmas ainda esta em implementacao, use o <strong>TurmaId</strong> manual para concluir a enturmacao.
          </p>
          <Input
            label="TurmaId"
            placeholder="Ex.: 15"
            value={turmaIdEnturmar}
            onChange={(e) => setTurmaIdEnturmar(e.target.value)}
          />
        </div>
      </Modal>
    </div>
  );
}
