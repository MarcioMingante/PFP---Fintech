import type { Metadata } from "next";
import Link from "next/link";
import { PageHeader } from "@/components/page-header";

export const metadata: Metadata = { title: "Visão geral" };

export default function DashboardPage() {
  return <><PageHeader title="Tudo começa com organização." description="Um espaço para acompanhar suas finanças e dar o próximo passo com clareza." />
    <section className="welcome-panel"><div><p className="eyebrow">SEU PONTO DE PARTIDA</p><h2>Conheça seu dinheiro.<br />Planeje suas escolhas.</h2><p>Comece pelas suas contas e categorias. Uma boa visão financeira nasce de informações bem organizadas.</p><Link className="button" href="/accounts">Explorar minhas contas <span aria-hidden="true">↗</span></Link></div><div className="welcome-art" aria-hidden="true"><div className="art-ring"><span>f.</span></div><div className="art-caption">CLAREZA PARA SEGUIR</div></div></section>
    <section className="module-grid" aria-label="Explore suas finanças">
      <Link href="/accounts" className="module-card"><span className="card-number">01</span><h2>Suas contas</h2><p>Encontre suas contas em um único lugar.</p><span className="card-link">Ver contas ↗</span></Link>
      <Link href="/categories" className="module-card"><span className="card-number">02</span><h2>Suas categorias</h2><p>Dê contexto às suas receitas e despesas.</p><span className="card-link">Ver categorias ↗</span></Link>
      <Link href="/transactions" className="module-card"><span className="card-number">03</span><h2>Suas movimentações</h2><p>Prepare o caminho para acompanhar entradas e saídas.</p><span className="card-link">Ver transações ↗</span></Link>
    </section></>;
}
