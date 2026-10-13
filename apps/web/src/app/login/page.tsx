import type { Metadata } from "next";
import { cookies } from "next/headers";
import { AuthForm } from "@/features/auth/auth-form";
import { refreshSession } from "@/features/auth/actions";

export const metadata: Metadata = { title: "Entrar" };

export default async function LoginPage() {
  const hasRefresh = Boolean((await cookies()).get("fintrack_refresh"));
  return <main id="main-content" tabIndex={-1} className="auth-page"><section className="panel auth-panel"><p className="eyebrow">FINTRACK</p><h1>Seu espaço financeiro.</h1><p>Entre para acessar suas contas e categorias.</p><AuthForm mode="login" />{hasRefresh && <form action={refreshSession}><button className="session-button" type="submit">Renovar sessão</button></form>}</section></main>;
}
