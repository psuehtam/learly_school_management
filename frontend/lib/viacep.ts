import { digitsOnly } from "@/utils";

/** Valores aceitos no formulario de endereco (secretaria / alunos). */
export type TipoLogradouroForm =
  | "Rua"
  | "Avenida"
  | "Travessa"
  | "Alameda"
  | "Estrada"
  | "Rodovia"
  | "Outro";

export type EnderecoViaCep = {
  cepFormatado: string;
  tipoLogradouro: TipoLogradouroForm;
  logradouro: string;
  complemento: string;
  bairro: string;
  municipio: string;
  /** Sigla UF (2 letras). */
  uf: string;
};

type ViaCepJson = {
  cep?: string;
  logradouro?: string;
  complemento?: string;
  bairro?: string;
  localidade?: string;
  uf?: string;
  erro?: boolean;
};

const PREFIXOS_TIPO: Record<string, TipoLogradouroForm> = {
  Rua: "Rua",
  Avenida: "Avenida",
  Travessa: "Travessa",
  Alameda: "Alameda",
  Estrada: "Estrada",
  Rodovia: "Rodovia",
  Praça: "Outro",
  Praca: "Outro",
};

function inferTipoENomeLogradouro(logradouroApi: string): { tipo: TipoLogradouroForm; nome: string } {
  const trimmed = logradouroApi.trim();
  if (!trimmed) {
    return { tipo: "Rua", nome: "" };
  }
  const partes = trimmed.split(/\s+/);
  const primeiro = partes[0];
  if (!primeiro) {
    return { tipo: "Rua", nome: trimmed };
  }
  const tipo = PREFIXOS_TIPO[primeiro];
  if (tipo) {
    const resto = partes.slice(1).join(" ").trim();
    return { tipo, nome: resto };
  }
  return { tipo: "Rua", nome: trimmed };
}

/** Busca endereco na API publica ViaCEP (apenas navegador). */
export async function buscarEnderecoPorCep(cepMaskedOrDigits: string): Promise<EnderecoViaCep> {
  const d = digitsOnly(cepMaskedOrDigits, 8);
  if (d.length !== 8) {
    throw new Error("Informe o CEP com 8 digitos.");
  }

  const res = await fetch(`https://viacep.com.br/ws/${d}/json/`, { method: "GET" });
  if (!res.ok) {
    throw new Error("Nao foi possivel consultar o CEP. Tente novamente.");
  }

  const data = (await res.json()) as ViaCepJson;
  if (data.erro === true || !data.localidade) {
    throw new Error("CEP nao encontrado.");
  }

  const { tipo, nome } = inferTipoENomeLogradouro(data.logradouro ?? "");

  const cepFmt = d.length === 8 ? `${d.slice(0, 5)}-${d.slice(5)}` : d;

  return {
    cepFormatado: cepFmt,
    tipoLogradouro: tipo,
    logradouro: nome,
    complemento: (data.complemento ?? "").trim(),
    bairro: (data.bairro ?? "").trim(),
    municipio: (data.localidade ?? "").trim(),
    uf: (data.uf ?? "").trim().toUpperCase().slice(0, 2),
  };
}
