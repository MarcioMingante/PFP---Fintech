import Link from "next/link";

export default function NotFound() {
  return <section className="panel error-state"><p className="eyebrow">404</p><h1>Página não encontrada.</h1><p>Volte para sua visão geral e continue por lá.</p><Link className="button" href="/dashboard">Ir para visão geral</Link></section>;
}
