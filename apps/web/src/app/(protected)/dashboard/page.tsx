import type { Metadata } from "next";
import Link from "next/link";
import { PageHeader } from "@/components/page-header";
import { DashboardCharts } from "@/features/dashboard/dashboard-charts";
import { getDashboardSummary } from "@/services/dashboard";
import { dashboardDateSchema } from "@/schemas/dashboard";
import { formatDate, formatMoney, formatMonth } from "@/utils/finance";

export const metadata: Metadata = { title: "Visão geral" };

export default async function DashboardPage({ searchParams }: { searchParams: Promise<{ asOf?: string | string[] }> }) {
  const { asOf } = await searchParams;
  if (asOf !== undefined && (typeof asOf !== "string" || !dashboardDateSchema.safeParse(asOf).success)) {
    return <section className="panel error-state"><h1>Data inválida</h1><p>Escolha uma data entre 01/01/0002 e 01/12/9998.</p><Link className="button" href="/dashboard">Voltar ao resumo</Link></section>;
  }
  const data = await getDashboardSummary(asOf);
  const metrics = [
    ["Saldo registrado", data.currentBalance, "Receitas menos despesas até a data"],
    ["Receitas do mês", data.incomeThisMonth, "Entradas registradas"],
    ["Despesas do mês", data.expensesThisMonth, "Saídas das contas"],
    ["Faturas do mês", data.creditCardInvoicesThisMonth, "Parcelas com vencimento no mês"],
  ] as const;
  return <>
    <PageHeader title="Seu dinheiro, com clareza." description="Acompanhe o que entrou, o que saiu e os compromissos que vêm pela frente." />
    <form className="dashboard-filter" action="/dashboard"><label htmlFor="dashboard-date">Dados até</label><input id="dashboard-date" name="asOf" type="date" min="0002-01-01" max="9998-12-01" defaultValue={data.asOf} required /><button className="button" type="submit">Atualizar visão</button></form>
    <section className="metric-grid" aria-label="Resumo financeiro">{metrics.map(([label, value, note]) => <article className="panel metric-card" key={label}><h2>{label}</h2><strong>{formatMoney(value)}</strong><p>{note}</p></article>)}</section>
    <DashboardCharts data={data} />
    <section className="dashboard-charts">
      <article className="panel dashboard-panel"><h2>Compromissos futuros</h2><p>Próximos três meses · {formatMoney(data.futureExpenses)}</p><div className="table-scroll"><table><caption className="sr-only">Previsão de despesas</caption><thead><tr><th>Mês</th><th>Recorrências</th><th>Cartões</th><th>Total</th></tr></thead><tbody>{data.forecast.map(item => <tr key={item.month}><th scope="row">{formatMonth(item.month)}</th><td>{formatMoney(item.recurringExpenses)}</td><td>{formatMoney(item.installments)}</td><td>{formatMoney(item.total)}</td></tr>)}</tbody></table></div><p className="dashboard-note">Previsão de recorrências ativas e parcelas. Valores previstos não entram no saldo registrado.</p></article>
      <article className="panel dashboard-panel"><h2>Faturas com vencimento no mês</h2><p>Compromissos dos seus cartões</p>{data.invoices.length === 0 ? <p>Nenhuma fatura com vencimento neste mês.</p> : <ul className="invoice-totals">{data.invoices.map(item => <li key={item.id}><div><strong>{item.cardName}</strong><p>Vence em {formatDate(item.dueDate)}</p></div><strong>{formatMoney(item.total)}</strong></li>)}</ul>}<p className="dashboard-note">Faturas aparecem separadas das despesas das contas. Pagamentos ainda não são registrados.</p></article>
    </section>
    <p className="dashboard-note">Visão até {formatDate(data.asOf)}. Saldo considera apenas lançamentos registrados, sem saldo inicial das contas. Histórico não inclui parcelas dos cartões.</p>
  </>;
}
