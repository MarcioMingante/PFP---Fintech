import type { Metadata } from "next";
import { randomUUID } from "node:crypto";
import { PageHeader } from "@/components/page-header";
import { CreditCardForm, PurchaseForm } from "@/features/finance/forms";
import { listCreditCards, listInvoices } from "@/services/finance";
import { formatDate, formatMoney } from "@/utils/finance";
export const metadata: Metadata = { title: "Cartões" };
export default async function CreditCardsPage() {
  const cards = await listCreditCards();
  const invoices = (await Promise.all(cards.map(async card => (await listInvoices(card.id)).map(invoice => ({ ...invoice, cardName: card.name }))))).flat();
  return <><PageHeader title="Seus cartões." description="Organize compras parceladas e acompanhe faturas." /><section className="dashboard-charts"><CreditCardForm /><PurchaseForm cards={cards} today={new Date().toISOString().slice(0, 10)} idempotencyKey={randomUUID()} /></section><section className="panel dashboard-panel"><h2>Cartões cadastrados</h2>{cards.length === 0 ? <p>Nenhum cartão cadastrado.</p> : <ul className="invoice-totals">{cards.map(card => <li key={card.id}><strong>{card.name}</strong><span>Limite {formatMoney(card.limit)}</span></li>)}</ul>}</section><section className="panel dashboard-panel"><h2>Faturas</h2>{invoices.length === 0 ? <p>Nenhuma fatura registrada.</p> : <div className="table-scroll"><table><thead><tr><th>Cartão</th><th>Fechamento</th><th>Vencimento</th><th>Total</th></tr></thead><tbody>{invoices.sort((a, b) => a.dueDate.localeCompare(b.dueDate)).map(item => <tr key={item.id}><th scope="row">{item.cardName}</th><td>{formatDate(item.closingDate)}</td><td>{formatDate(item.dueDate)}</td><td>{formatMoney(item.total)}</td></tr>)}</tbody></table></div>}<p className="dashboard-note">Pagamentos ainda não são registrados.</p></section></>;
}
