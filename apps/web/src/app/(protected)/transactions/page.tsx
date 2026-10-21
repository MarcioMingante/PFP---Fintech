import { randomUUID } from "node:crypto";
import type { Metadata } from "next";
import { PageHeader } from "@/components/page-header";
import { TransactionForm, TransferForm } from "@/features/finance/forms";
import { listResources } from "@/services/resources";
import { listTransactions, listTransfers } from "@/services/finance";
import { formatDate, formatMoney } from "@/utils/finance";
export const metadata: Metadata = { title: "Transações" };
export default async function TransactionsPage() {
  const [accounts, categories, transactions, transfers] = await Promise.all([listResources("accounts"), listResources("categories"), listTransactions(), listTransfers()]);
  return <><PageHeader title="Suas movimentações." description="Registre receitas e despesas e acompanhe seu histórico." /><TransactionForm accounts={accounts} categories={categories} today={new Date().toISOString().slice(0, 10)} /><TransferForm accounts={accounts} today={new Date().toISOString().slice(0, 10)} idempotencyKey={randomUUID()} /><section className="panel dashboard-panel"><h2>Lançamentos</h2>{transactions.length === 0 ? <p>Nenhum lançamento registrado.</p> : <ul className="invoice-totals">{transactions.map(item => <li key={item.id}><div><strong>{item.description}</strong><p>{item.type === "Income" ? "Receita" : "Despesa"} · {formatDate(item.occurredOn)}</p></div><strong>{formatMoney(item.amount)}</strong></li>)}</ul>}</section><section className="panel dashboard-panel"><h2>Transferências</h2>{transfers.length === 0 ? <p>Nenhuma transferência registrada.</p> : <ul className="invoice-totals">{transfers.map(item => <li key={item.id}><div><strong>{item.description}</strong><p>{accounts.find(account => account.id === item.sourceAccountId)?.name} para {accounts.find(account => account.id === item.destinationAccountId)?.name} · {formatDate(item.occurredOn)}</p></div><strong>{formatMoney(item.amount)}</strong></li>)}</ul>}</section></>;
}
