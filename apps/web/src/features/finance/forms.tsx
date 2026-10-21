"use client";
import { useActionState } from "react";
import { createAccount, createTransaction, createCreditCard, createPurchase, createTransfer } from "./actions";
import type { Resource } from "@/types/resource";
function ErrorMessage({ message }: { message: string }) { return message ? <p role="alert" className="auth-error">{message}</p> : null; }
export function AccountForm() {
  const [state, action, pending] = useActionState(createAccount, { error: "" });
  return <section className="panel finance-form-panel"><h2>Criar conta</h2><form action={action} className="finance-form"><label htmlFor="account-name">Nome da conta</label><input id="account-name" name="name" required maxLength={100} /><ErrorMessage message={state.error} /><button className="button" disabled={pending}>{pending ? "Salvando…" : "Salvar conta"}</button></form></section>;
}
export function TransactionForm({ accounts, categories, today }: { accounts: Resource[]; categories: Resource[]; today: string }) {
  const [state, action, pending] = useActionState(createTransaction, { error: "" });
  return <section className="panel finance-form-panel"><h2>Novo lançamento</h2>{accounts.length === 0 ? <p>Cadastre uma conta para registrar lançamentos.</p> : <form action={action} className="finance-form">
    <label htmlFor="transaction-account">Conta</label><select id="transaction-account" name="accountId" required>{accounts.map(item => <option key={item.id} value={item.id}>{item.name}</option>)}</select>
    <label htmlFor="transaction-category">Categoria</label><select id="transaction-category" name="categoryId"><option value="">Sem categoria</option>{categories.map(item => <option key={item.id} value={item.id}>{item.name}</option>)}</select>
    <label htmlFor="transaction-type">Tipo</label><select id="transaction-type" name="type"><option value="Income">Receita</option><option value="Expense">Despesa</option></select>
    <label htmlFor="transaction-description">Descrição</label><input id="transaction-description" name="description" required maxLength={200} />
    <label htmlFor="transaction-amount">Valor</label><input id="transaction-amount" name="amount" type="text" inputMode="decimal" pattern="[0-9]+([.][0-9]{1,2})?" placeholder="0.00" required />
    <label htmlFor="transaction-date">Data do lançamento</label><input id="transaction-date" name="occurredOn" type="date" defaultValue={today} max={today} required />
    <ErrorMessage message={state.error} /><button className="button" disabled={pending}>{pending ? "Salvando…" : "Salvar lançamento"}</button>
  </form>}</section>;
}
export function CreditCardForm() {
  const [state, action, pending] = useActionState(createCreditCard, { error: "" });
  return <section className="panel finance-form-panel"><h2>Novo cartão</h2><form action={action} className="finance-form">
    <label htmlFor="card-name">Nome do cartão</label><input id="card-name" name="name" required maxLength={100} />
    <label htmlFor="card-limit">Limite</label><input id="card-limit" name="limit" type="text" inputMode="decimal" pattern="[0-9]+([.][0-9]{1,2})?" placeholder="0.00" required />
    <label htmlFor="card-closing">Dia de fechamento</label><input id="card-closing" name="closingDay" type="number" min="1" max="31" required />
    <label htmlFor="card-due">Dia de vencimento</label><input id="card-due" name="dueDay" type="number" min="1" max="31" required />
    <ErrorMessage message={state.error} /><button className="button" disabled={pending}>{pending ? "Salvando…" : "Salvar cartão"}</button>
  </form></section>;
}
export function PurchaseForm({ cards, today, idempotencyKey }: { cards: Resource[]; today: string; idempotencyKey: string }) {
  const [state, action, pending] = useActionState(createPurchase, { error: "" });
  return <section className="panel finance-form-panel"><h2>Compra parcelada</h2>{cards.length === 0 ? <p>Cadastre um cartão para registrar compras.</p> : <form action={action} className="finance-form">
    <input type="hidden" name="idempotencyKey" value={idempotencyKey} />
    <label htmlFor="purchase-card">Cartão</label><select id="purchase-card" name="creditCardId">{cards.map(item => <option value={item.id} key={item.id}>{item.name}</option>)}</select>
    <label htmlFor="purchase-description">Descrição da compra</label><input id="purchase-description" name="description" required maxLength={200} />
    <label htmlFor="purchase-amount">Valor total</label><input id="purchase-amount" name="amount" type="text" inputMode="decimal" pattern="[0-9]+([.][0-9]{1,2})?" placeholder="0.00" required />
    <label htmlFor="purchase-count">Parcelas</label><input id="purchase-count" name="installmentCount" type="number" min="1" max="120" required />
    <label htmlFor="purchase-date">Data da compra</label><input id="purchase-date" name="purchaseDate" type="date" defaultValue={today} required />
    <ErrorMessage message={state.error} /><button className="button" disabled={pending}>{pending ? "Salvando…" : "Salvar compra"}</button>
  </form>}</section>;
}

export function TransferForm({ accounts, today, idempotencyKey }: { accounts: Resource[]; today: string; idempotencyKey: string }) {
  const [state, action, pending] = useActionState(createTransfer, { error: "" });
  return <section className="panel finance-form-panel"><h2>Transferir entre contas</h2>{accounts.length < 2 ? <p>Cadastre duas contas para transferir.</p> : <form action={action} className="finance-form">
    <input type="hidden" name="idempotencyKey" value={idempotencyKey} />
    <label htmlFor="transfer-source">Conta de origem</label><select id="transfer-source" name="sourceAccountId">{accounts.map(item => <option key={item.id} value={item.id}>{item.name}</option>)}</select>
    <label htmlFor="transfer-destination">Conta de destino</label><select id="transfer-destination" name="destinationAccountId" defaultValue={accounts[1].id}>{accounts.map(item => <option key={item.id} value={item.id}>{item.name}</option>)}</select>
    <label htmlFor="transfer-description">Descrição da transferência</label><input id="transfer-description" name="description" required maxLength={200} />
    <label htmlFor="transfer-amount">Valor da transferência</label><input id="transfer-amount" name="amount" type="text" inputMode="decimal" pattern="[0-9]+([.][0-9]{1,2})?" placeholder="0.00" required />
    <label htmlFor="transfer-date">Data da transferência</label><input id="transfer-date" name="occurredOn" type="date" defaultValue={today} max={today} required />
    <ErrorMessage message={state.error} /><button className="button" disabled={pending}>Salvar transferência</button>
  </form>}</section>;
}
