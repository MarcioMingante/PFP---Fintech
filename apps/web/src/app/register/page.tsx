import type { Metadata } from "next";
import { AuthForm } from "@/features/auth/auth-form";

export const metadata: Metadata = { title: "Criar conta" };

export default function RegisterPage() {
  return <main id="main-content" tabIndex={-1} className="auth-page"><section className="panel auth-panel"><p className="eyebrow">FINTRACK</p><h1>Comece por aqui.</h1><p>Crie seu acesso para organizar suas finanças.</p><AuthForm mode="register" /></section></main>;
}
