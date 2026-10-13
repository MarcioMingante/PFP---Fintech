"use client";

import { useActionState } from "react";
import Link from "next/link";
import { login, register } from "@/features/auth/actions";

export function AuthForm({ mode }: { mode: "login" | "register" }) {
  const [state, action, pending] = useActionState(mode === "login" ? login : register, { error: "" });
  return <form action={action} className="auth-form">
    <label htmlFor="email">E-mail</label>
    <input id="email" name="email" type="email" maxLength={254} autoComplete="email" required />
    <label htmlFor="password">Senha</label>
    <input id="password" name="password" type="password" minLength={12} maxLength={128} autoComplete={mode === "login" ? "current-password" : "new-password"} required aria-describedby="password-help" />
    <p id="password-help">Use entre 12 e 128 caracteres.</p>
    {state.error && <p role="alert" className="auth-error">{state.error}</p>}
    <button className="button" type="submit" disabled={pending}>{pending ? "Aguarde…" : mode === "login" ? "Entrar" : "Criar conta"}</button>
    <Link href={mode === "login" ? "/register" : "/login"}>{mode === "login" ? "Ainda não tem conta? Cadastre-se" : "Já tem conta? Entrar"}</Link>
  </form>;
}
