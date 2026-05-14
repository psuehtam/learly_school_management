export type BrazilMaskKind = "cpf" | "cnpj" | "phone" | "cep";

/** Apenas dígitos, opcionalmente limitado (útil para enviar à API). */
export function digitsOnly(value: string, maxLength?: number): string {
  const d = value.replace(/\D/g, "");
  return maxLength !== undefined ? d.slice(0, maxLength) : d;
}

/** CPF com máscara enquanto digita (até 11 dígitos). */
export function formatCPF(value: string): string {
  const d = digitsOnly(value, 11);
  if (d.length <= 3) return d;
  if (d.length <= 6) return `${d.slice(0, 3)}.${d.slice(3)}`;
  if (d.length <= 9) return `${d.slice(0, 3)}.${d.slice(3, 6)}.${d.slice(6)}`;
  return `${d.slice(0, 3)}.${d.slice(3, 6)}.${d.slice(6, 9)}-${d.slice(9)}`;
}

/** CNPJ com máscara enquanto digita (até 14 dígitos). */
export function formatCNPJ(value: string): string {
  const d = digitsOnly(value, 14);
  if (d.length <= 2) return d;
  if (d.length <= 5) return `${d.slice(0, 2)}.${d.slice(2)}`;
  if (d.length <= 8) return `${d.slice(0, 2)}.${d.slice(2, 5)}.${d.slice(5)}`;
  if (d.length <= 12) {
    return `${d.slice(0, 2)}.${d.slice(2, 5)}.${d.slice(5, 8)}/${d.slice(8)}`;
  }
  return `${d.slice(0, 2)}.${d.slice(2, 5)}.${d.slice(5, 8)}/${d.slice(8, 12)}-${d.slice(12)}`;
}

/**
 * Telefone BR: (DD) NNNN-NNNN ou (DD) NNNNN-NNNN.
 * Se o primeiro dígito da parte local for 9, assume celular (5+4) durante a digitação.
 */
/** CEP: 00000-000 */
export function formatCEP(value: string): string {
  const d = digitsOnly(value, 8);
  if (d.length <= 5) return d;
  return `${d.slice(0, 5)}-${d.slice(5)}`;
}

export function formatPhone(value: string): string {
  const d = digitsOnly(value, 11);
  if (d.length === 0) return "";
  if (d.length <= 2) return `(${d}`;
  const ddd = d.slice(0, 2);
  const rest = d.slice(2);
  if (rest.length === 0) return `(${ddd}) `;
  const mobile = rest[0] === "9";
  if (mobile) {
    if (rest.length <= 5) return `(${ddd}) ${rest}`;
    return `(${ddd}) ${rest.slice(0, 5)}-${rest.slice(5)}`;
  }
  if (rest.length <= 4) return `(${ddd}) ${rest}`;
  if (d.length <= 10) return `(${ddd}) ${rest.slice(0, 4)}-${rest.slice(4)}`;
  return `(${ddd}) ${rest.slice(0, 5)}-${rest.slice(5)}`;
}

/** Aplica máscara conforme o tipo — use no `onChange` e grave o valor já formatado no state. */
export function applyBrazilMask(kind: BrazilMaskKind, rawInput: string): string {
  switch (kind) {
    case "cpf":
      return formatCPF(rawInput);
    case "cnpj":
      return formatCNPJ(rawInput);
    case "phone":
      return formatPhone(rawInput);
    case "cep":
      return formatCEP(rawInput);
  }
}

function cpfCheckDigit(base: string, factor: number): number {
  let sum = 0;
  for (let i = 0; i < base.length; i++) {
    sum += parseInt(base[i]!, 10) * (factor - i);
  }
  const mod = sum % 11;
  return mod < 2 ? 0 : 11 - mod;
}

/** true se tiver 11 dígitos e dígitos verificadores válidos. */
export function isValidCPF(maskedOrDigits: string): boolean {
  const d = digitsOnly(maskedOrDigits, 11);
  if (d.length !== 11 || /^(\d)\1{10}$/.test(d)) return false;
  const d1 = cpfCheckDigit(d.slice(0, 9), 10);
  if (d1 !== parseInt(d[9]!, 10)) return false;
  const d2 = cpfCheckDigit(d.slice(0, 10), 11);
  return d2 === parseInt(d[10]!, 10);
}

function cnpjCheckDigit(digits: string, weights: number[]): number {
  let sum = 0;
  for (let i = 0; i < digits.length; i++) {
    sum += parseInt(digits[i]!, 10) * weights[i]!;
  }
  const mod = sum % 11;
  return mod < 2 ? 0 : 11 - mod;
}

/** true se tiver 14 dígitos e dígitos verificadores válidos. */
export function isValidCNPJ(maskedOrDigits: string): boolean {
  const d = digitsOnly(maskedOrDigits, 14);
  if (d.length !== 14 || /^(\d)\1{13}$/.test(d)) return false;
  const w1 = [5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];
  const w2 = [6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];
  const i1 = cnpjCheckDigit(d.slice(0, 12), w1);
  if (i1 !== parseInt(d[12]!, 10)) return false;
  const i2 = cnpjCheckDigit(d.slice(0, 13), w2);
  return i2 === parseInt(d[13]!, 10);
}

/** true se tiver 10 ou 11 dígitos (DDD + número). */
export function isValidBrazilPhone(maskedOrDigits: string): boolean {
  const d = digitsOnly(maskedOrDigits);
  if (d.length !== 10 && d.length !== 11) return false;
  if (d.length === 11 && d[2] !== "9") return false;
  if (d.length === 10 && d[2] === "9") return false;
  return true;
}

/**
 * Mensagem de erro quando o campo já está "completo" em tamanho e inválido; senão `undefined`.
 * Use em `error` do Input ou antes de submit.
 */
export function brazilFieldError(kind: BrazilMaskKind, maskedValue: string): string | undefined {
  const len = digitsOnly(maskedValue).length;
  switch (kind) {
    case "cpf":
      if (len === 0) return undefined;
      if (len < 11) return undefined;
      return isValidCPF(maskedValue) ? undefined : "CPF inválido.";
    case "cnpj":
      if (len === 0) return undefined;
      if (len < 14) return undefined;
      return isValidCNPJ(maskedValue) ? undefined : "CNPJ inválido.";
    case "phone":
      if (len === 0) return undefined;
      if (len < 10) return undefined;
      return isValidBrazilPhone(maskedValue) ? undefined : "Telefone inválido.";
    case "cep":
      if (len === 0) return undefined;
      if (len < 8) return undefined;
      return len === 8 ? undefined : "CEP inválido.";
  }
}
