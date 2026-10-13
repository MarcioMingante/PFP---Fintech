import Link from "next/link";
import { Navigation } from "@/components/navigation";
import { requireSession } from "@/services/session";
import { logout } from "@/features/auth/actions";

export default async function ProtectedLayout({ children }: { children: React.ReactNode }) {
  const session = await requireSession();
  return (
        <div className="app-shell">
          <aside className="sidebar">
            <Link className="brand" href="/dashboard"><span className="brand-mark" aria-hidden="true">f.</span>FinTrack</Link>
            <p className="sidebar-label">ESPAÇO FINANCEIRO</p>
            <Navigation />
            <div className="sidebar-note"><strong>Um passo de cada vez.</strong><p>Mais organização hoje.<br />Mais tranquilidade amanhã.</p></div>
          </aside>
          <div className="workspace">
            <div className="topbar"><span>{session.email}</span><form action={logout}><button className="session-button" type="submit">Sair</button></form></div>
            <main id="main-content" tabIndex={-1}>{children}</main>
            <footer>FinTrack · Seu dinheiro, uma visão mais clara.</footer>
          </div>
        </div>
  );
}
