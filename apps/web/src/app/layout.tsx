import type { Metadata } from "next";
import Link from "next/link";
import { Navigation } from "@/components/navigation";
import "./globals.css";

export const metadata: Metadata = {
  title: { default: "FinTrack | Finanças pessoais", template: "%s | FinTrack" },
  description: "Organize suas contas, categorias e movimentações financeiras em um só lugar.",
};

export default function RootLayout({ children }: { children: React.ReactNode }) {
  return (
    <html lang="pt-BR">
      <body>
        <a className="skip-link" href="#main-content">Ir para conteúdo</a>
        <div className="app-shell">
          <aside className="sidebar">
            <Link className="brand" href="/dashboard"><span className="brand-mark" aria-hidden="true">f.</span>FinTrack</Link>
            <p className="sidebar-label">ESPAÇO FINANCEIRO</p>
            <Navigation />
            <div className="sidebar-note"><strong>Um passo de cada vez.</strong><p>Mais organização hoje.<br />Mais tranquilidade amanhã.</p></div>
          </aside>
          <div className="workspace">
            <div className="topbar"><span>Finanças pessoais</span><span className="workspace-label">Meu espaço</span></div>
            <main id="main-content" tabIndex={-1}>{children}</main>
            <footer>FinTrack · Seu dinheiro, uma visão mais clara.</footer>
          </div>
        </div>
      </body>
    </html>
  );
}
