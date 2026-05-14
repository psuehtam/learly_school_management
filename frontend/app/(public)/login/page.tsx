"use client";

import { Suspense, useState } from "react";
import Image from "next/image";
import { useRouter, useSearchParams } from "next/navigation";
import { saveSessionInfo, setSessionFlagCookie } from "@/lib/auth";
import { getApiErrorMessage } from "@/lib/api/client";
import { login } from "@/lib/api/auth";
import { resolvePostLoginTarget } from "@/lib/post-login-redirect";
import type { User } from "@/lib/api/types";

function IconBuilding({ className }: { className?: string }) {
  return (
    <svg className={className} viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.75" aria-hidden>
      <path d="M4 21V8.5L12 4l8 4.5V21" strokeLinecap="round" strokeLinejoin="round" />
      <path d="M9 21v-4h6v4" strokeLinecap="round" strokeLinejoin="round" />
      <path d="M9 13h.01M12 13h.01M15 13h.01M9 10h.01M12 10h.01M15 10h.01" strokeLinecap="round" />
    </svg>
  );
}

function IconMail({ className }: { className?: string }) {
  return (
    <svg className={className} viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.75" aria-hidden>
      <path d="M4 6h16v12H4V6Z" strokeLinejoin="round" />
      <path d="m4 7 8 6 8-6" strokeLinecap="round" strokeLinejoin="round" />
    </svg>
  );
}

function IconLock({ className }: { className?: string }) {
  return (
    <svg className={className} viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.75" aria-hidden>
      <rect x="5" y="11" width="14" height="10" rx="2" />
      <path d="M8 11V8a4 4 0 0 1 8 0v3" strokeLinecap="round" />
    </svg>
  );
}

function IconEye({ off }: { off?: boolean }) {
  if (off) {
    return (
      <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.75" className="size-4" aria-hidden>
        <path d="M3 3l18 18M10.6 10.6a2 2 0 0 0 2.8 2.8" strokeLinecap="round" />
        <path d="M9.88 5.09A10.4 10.4 0 0 1 12 5c4 0 7.33 2.33 10 7-.37.62-.78 1.18-1.22 1.69M6.12 6.12C3.93 7.94 2.2 10.57 2 12c.67 1.33 2 4 5 4 .93 0 1.81-.18 2.64-.52" strokeLinecap="round" strokeLinejoin="round" />
        <path d="M12 17c-4 0-7.33-2.33-10-7 1.26-2.09 2.65-3.61 4.14-4.73" strokeLinecap="round" strokeLinejoin="round" />
      </svg>
    );
  }
  return (
    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.75" className="size-4" aria-hidden>
      <path d="M2 12s3.5-7 10-7 10 7 10 7-3.5 7-10 7S2 12 2 12Z" strokeLinejoin="round" />
      <circle cx="12" cy="12" r="3" />
    </svg>
  );
}

function LoginForm() {
  const router = useRouter();
  const searchParams = useSearchParams();

  const [codigoEscola, setCodigoEscola] = useState("");
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [showPassword, setShowPassword] = useState(false);
  const [loading, setLoading] = useState(false);
  const [mensagem, setMensagem] = useState("");

  function extractPermissions(raw: unknown): string[] {
    if (!raw || typeof raw !== "object") return [];
    const data = raw as { permissions?: unknown; permissoes?: unknown };
    const p1 = Array.isArray(data.permissions) ? data.permissions : [];
    const p2 = Array.isArray(data.permissoes) ? data.permissoes : [];
    return [...p1, ...p2].filter((x): x is string => typeof x === "string");
  }

  function buildUserFromLogin(raw: unknown): User {
    const data = (raw && typeof raw === "object" ? raw : {}) as {
      userId?: unknown;
      id?: unknown;
      nome?: unknown;
      perfil?: unknown;
      role?: unknown;
      email?: unknown;
      isSuperAdmin?: unknown;
    };

    const rawId = typeof data.userId === "number" ? data.userId : typeof data.id === "number" ? data.id : null;
    const role =
      typeof data.perfil === "string" ? data.perfil :
      typeof data.role === "string" ? data.role :
      "Usuario";

    return {
      id: rawId,
      nome: typeof data.nome === "string" ? data.nome : "Usuario",
      role,
      permissions: extractPermissions(raw),
      email: typeof data.email === "string" ? data.email : undefined,
      isSuperAdmin: data.isSuperAdmin === true || data.isSuperAdmin === "true",
    };
  }

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setLoading(true);
    setMensagem("");

    try {
      const dados = await login({
        codigoEscola: codigoEscola.trim() || null,
        email,
        senha: password,
      });
      const user = buildUserFromLogin(dados?.usuario);
      const isSuperAdmin = Boolean(user.isSuperAdmin);
      saveSessionInfo({
        nome: user.nome,
        perfil: user.role,
        email: user.email,
        isSuperAdmin,
      });
      setSessionFlagCookie(dados.expiraEmUtc);

      const next = searchParams.get("next");
      const target = resolvePostLoginTarget(user, next);

      router.replace(target);
    } catch (erro) {
      console.error(erro);
      setMensagem(getApiErrorMessage(erro, "Não foi possível conectar ao servidor."));
    } finally {
      setLoading(false);
    }
  };

  const inputClass =
    "h-11 w-full rounded-xl border border-zinc-200/90 bg-white/90 pl-10 pr-3 text-sm text-zinc-900 shadow-sm outline-none transition focus:border-[#4a6d8c] focus:ring-2 focus:ring-[rgba(74,109,140,0.22)]";

  return (
    <main className="relative min-h-screen overflow-hidden font-sans">
      <div
        className="absolute inset-0 bg-gradient-to-br from-zinc-50 via-[#eef2f6] to-[#e2e9f0]"
        aria-hidden
      />
      <div
        className="pointer-events-none absolute -right-24 -top-24 h-[28rem] w-[28rem] rounded-full bg-[#4a6d8c]/[0.09] blur-3xl"
        aria-hidden
      />
      <div
        className="pointer-events-none absolute -bottom-32 -left-16 h-[22rem] w-[22rem] rounded-full bg-sky-300/15 blur-3xl"
        aria-hidden
      />
      <div
        className="pointer-events-none absolute left-1/2 top-1/2 h-px w-[120%] -translate-x-1/2 -translate-y-1/2 rotate-[-8deg] bg-gradient-to-r from-transparent via-white/40 to-transparent opacity-60"
        aria-hidden
      />

      <div className="relative z-10 flex min-h-screen flex-col items-center justify-center px-4 py-10 sm:px-6">
        <div className="w-full max-w-[420px]">
          <div className="mb-8 flex flex-col items-center text-center">
            <div className="mb-5 flex justify-center">
              <Image
                src="/images/Logo.svg"
                alt="Learly"
                width={112}
                height={112}
                className="h-24 w-24 sm:h-28 sm:w-28"
                priority
              />
            </div>
            <h1 className="mt-2 text-2xl font-semibold tracking-tight text-zinc-900 sm:text-[1.65rem]">
              Bem-vindo de volta
            </h1>
            <p className="mt-1.5 max-w-sm text-sm leading-relaxed text-zinc-500">
              Entre com sua escola e credenciais para acessar o painel.
            </p>
          </div>

          <div className="rounded-2xl border border-white/70 bg-white/75 p-8 shadow-[0_24px_80px_-16px_rgba(15,23,42,0.12)] shadow-zinc-900/8 backdrop-blur-xl ring-1 ring-zinc-900/[0.04] sm:p-9">
            <form onSubmit={handleSubmit} className="flex flex-col gap-5">
              <div className="flex flex-col gap-2">
                <label htmlFor="codigo-escola" className="text-sm font-medium text-zinc-700">
                  Código da escola
                </label>
                <div className="relative">
                  <IconBuilding className="pointer-events-none absolute left-3 top-1/2 size-[1.125rem] -translate-y-1/2 text-zinc-400" />
                  <input
                    id="codigo-escola"
                    type="text"
                    value={codigoEscola}
                    onChange={(e) => setCodigoEscola(e.target.value.toUpperCase())}
                    autoComplete="organization"
                    className={`${inputClass} uppercase`}
                  />
                </div>
              </div>

              <div className="flex flex-col gap-2">
                <label htmlFor="login-email" className="text-sm font-medium text-zinc-700">
                  E-mail ou usuário
                </label>
                <div className="relative">
                  <IconMail className="pointer-events-none absolute left-3 top-1/2 size-[1.125rem] -translate-y-1/2 text-zinc-400" />
                  <input
                    id="login-email"
                    type="text"
                    value={email}
                    onChange={(e) => setEmail(e.target.value)}
                    required
                    autoComplete="username"
                    className={inputClass}
                  />
                </div>
              </div>

              <div className="flex flex-col gap-2">
                <label htmlFor="senha" className="text-sm font-medium text-zinc-700">
                  Senha
                </label>
                <div className="relative">
                  <IconLock className="pointer-events-none absolute left-3 top-1/2 size-[1.125rem] -translate-y-1/2 text-zinc-400" />
                  <input
                    id="senha"
                    type={showPassword ? "text" : "password"}
                    value={password}
                    onChange={(e) => setPassword(e.target.value)}
                    required
                    autoComplete="current-password"
                    className={`${inputClass} pr-11`}
                  />
                  <button
                    type="button"
                    onClick={() => setShowPassword(!showPassword)}
                    aria-pressed={showPassword}
                    aria-label={showPassword ? "Ocultar senha" : "Mostrar senha"}
                    className="absolute right-1.5 top-1/2 flex size-9 -translate-y-1/2 items-center justify-center rounded-lg text-zinc-500 transition hover:bg-zinc-100 hover:text-zinc-700"
                  >
                    <IconEye off={showPassword} />
                  </button>
                </div>
              </div>

              {mensagem ? (
                <div
                  role="alert"
                  className="rounded-xl border border-red-100 bg-red-50/90 px-3.5 py-3 text-center text-sm font-medium text-red-700"
                >
                  {mensagem}
                </div>
              ) : null}

              <button
                type="submit"
                disabled={loading}
                className="group relative mt-1 flex h-12 w-full items-center justify-center overflow-hidden rounded-xl bg-[#4a6d8c] text-sm font-semibold text-white shadow-lg shadow-[#4a6d8c]/25 transition hover:bg-[#3d5c78] active:scale-[0.98] disabled:cursor-not-allowed disabled:opacity-65 disabled:active:scale-100"
              >
                <span
                  className="absolute inset-0 bg-gradient-to-t from-black/10 to-transparent opacity-0 transition group-hover:opacity-100"
                  aria-hidden
                />
                {loading ? (
                  <span className="relative size-5 border-2 border-white/35 border-t-white rounded-full animate-spin" />
                ) : (
                  <span className="relative">Entrar</span>
                )}
              </button>
            </form>
          </div>

          <p className="mt-8 text-center text-xs text-zinc-400">
            © {new Date().getFullYear()} Learly · Gestão escolar
          </p>
        </div>
      </div>
    </main>
  );
}

function LoginFallback() {
  return (
    <main className="relative min-h-screen overflow-hidden font-sans">
      <div className="absolute inset-0 bg-gradient-to-br from-zinc-50 via-[#eef2f6] to-[#e2e9f0]" aria-hidden />
      <div className="relative z-10 flex min-h-screen flex-col items-center justify-center px-4 py-10">
        <div className="mb-8 h-24 w-24 animate-pulse rounded-2xl bg-zinc-300/40 sm:h-28 sm:w-28" />
        <div className="h-[420px] w-full max-w-[420px] animate-pulse rounded-2xl bg-white/50 shadow-xl ring-1 ring-zinc-900/5 backdrop-blur-sm" />
      </div>
    </main>
  );
}

export default function LoginPage() {
  return (
    <Suspense fallback={<LoginFallback />}>
      <LoginForm />
    </Suspense>
  );
}
