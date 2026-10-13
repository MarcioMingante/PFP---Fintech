"use server";

import { cookies } from "next/headers";
import { redirect } from "next/navigation";
import { getApiUrl } from "@/config/api";
import { credentialsSchema, tokensSchema } from "@/schemas/auth";

export type AuthState = { error: string };

async function storeTokens(payload: unknown) {
  const tokens = tokensSchema.parse(payload);
  const jar = await cookies();
  const options = { httpOnly: true, sameSite: "lax" as const, secure: process.env.AUTH_COOKIE_SECURE === "true", path: "/" };
  jar.set("fintrack_access", tokens.accessToken, { ...options, expires: new Date(tokens.expiresAt) });
  jar.set("fintrack_refresh", tokens.refreshToken, { ...options, expires: new Date(tokens.refreshExpiresAt) });
}

async function authenticate(mode: "login" | "register", data: FormData): Promise<AuthState> {
  const parsed = credentialsSchema.safeParse({ email: data.get("email"), password: data.get("password") });
  if (!parsed.success) return { error: "Informe e-mail válido e senha entre 12 e 128 caracteres." };
  try {
    const response = await fetch(new URL(`/auth/${mode}`, getApiUrl()), {
      method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify(parsed.data),
      cache: "no-store", signal: AbortSignal.timeout(5000),
    });
    if (!response.ok) return { error: response.status === 409 ? "E-mail já cadastrado." : response.status === 429 ? "Muitas tentativas. Aguarde um minuto." : response.status === 401 ? "E-mail ou senha incorretos." : "Não foi possível entrar. Tente novamente." };
    await storeTokens(await response.json());
  } catch { return { error: "Serviço indisponível. Tente novamente em instantes." }; }
  redirect("/dashboard");
}

export async function login(_state: AuthState, data: FormData) { return authenticate("login", data); }
export async function register(_state: AuthState, data: FormData) { return authenticate("register", data); }

export async function refreshSession() {
  const token = (await cookies()).get("fintrack_refresh")?.value;
  let ok = false;
  if (token) {
    try {
      const response = await fetch(new URL("/auth/refresh", getApiUrl()), {
        method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify({ refreshToken: token }),
        cache: "no-store", signal: AbortSignal.timeout(5000),
      });
      if (response.ok) { await storeTokens(await response.json()); ok = true; }
    } catch { /* Keep credentials out of error messages. */ }
  }
  if (ok) redirect("/dashboard");
  const jar = await cookies();
  jar.delete("fintrack_access"); jar.delete("fintrack_refresh");
  redirect("/login?expired=1");
}

export async function logout() {
  const jar = await cookies();
  let token = jar.get("fintrack_access")?.value;
  const refresh = jar.get("fintrack_refresh")?.value;
  try {
    // Renew an expired access token before revoking its session.
    if (refresh) {
      const response = await fetch(new URL("/auth/refresh", getApiUrl()), {
        method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify({ refreshToken: refresh }),
        cache: "no-store", signal: AbortSignal.timeout(5000),
      });
      if (response.ok) token = tokensSchema.parse(await response.json()).accessToken;
    }
    if (token) await fetch(new URL("/auth/logout", getApiUrl()), {
      method: "POST", headers: { Authorization: `Bearer ${token}` }, cache: "no-store", signal: AbortSignal.timeout(5000),
    });
  } catch { /* Local session is cleared even when the API is unavailable. */ }
  jar.delete("fintrack_access"); jar.delete("fintrack_refresh");
  redirect("/login");
}
