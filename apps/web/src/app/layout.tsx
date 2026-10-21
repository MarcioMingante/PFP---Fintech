import type { Metadata } from "next";
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
        {children}
      </body>
    </html>
  );
}
