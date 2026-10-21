"use server";
import { redirect } from "next/navigation";
import { revalidatePath } from "next/cache";
import { getAccessToken } from "@/services/session";
import { getApiUrl } from "@/config/api";
import { accountFormSchema, cardFormSchema, transactionFormSchema, purchaseFormSchema, transferFormSchema } from "@/schemas/finance-forms";

export type FinanceState = { error: string };
async function save(path: string, payload: unknown, destination: string, method = "POST"): Promise<FinanceState> {
  const token = await getAccessToken();
  if (!token) redirect("/login");
  let response: Response;
  try {
    response = await fetch(new URL(path, getApiUrl()), { method, headers: { Authorization: `Bearer ${token}`, "Content-Type": "application/json" }, body: JSON.stringify(payload), cache: "no-store", signal: AbortSignal.timeout(10000) });
  } catch { return { error: "Serviço indisponível. Tente novamente." }; }
  if (response.status === 401) redirect("/login?expired=1");
  if (!response.ok) {
    if (response.status === 400) {
      const body = await response.json().catch(() => null);
      const messages = body?.errors && Object.values(body.errors).flat().filter(value => typeof value === "string");
      return { error: messages?.length ? messages.join(" ") : "Confira os campos informados." };
    }
    return { error: response.status === 409 && method === "DELETE" ? "Conta em uso por lançamentos, recorrências ou transferências; exclusão bloqueada." : response.status === 404 ? "Conta não encontrada ou sem acesso. Atualize a página." : response.status === 409 ? "Solicitação em conflito. Confira os dados antes de reenviar." : "Não foi possível salvar. Confira sua conta ou cartão." };
  }
  revalidatePath(destination); revalidatePath("/dashboard"); revalidatePath("/accounts"); revalidatePath("/transactions");
  redirect(destination);
}
export async function createAccount(_state: FinanceState, data: FormData) {
  const parsed = accountFormSchema.safeParse(Object.fromEntries(data));
  return parsed.success ? save("/accounts", parsed.data, "/accounts") : { error: "Informe nome com até 100 caracteres." };
}
export async function createTransaction(_state: FinanceState, data: FormData) {
  const parsed = transactionFormSchema.safeParse(Object.fromEntries(data));
  return parsed.success ? save("/transactions", parsed.data, "/transactions") : { error: "Confira conta, descrição, valor e data." };
}
export async function createCreditCard(_state: FinanceState, data: FormData) {
  const parsed = cardFormSchema.safeParse(Object.fromEntries(data));
  return parsed.success ? save("/credit-cards", parsed.data, "/credit-cards") : { error: "Confira nome, limite e dias de 1 a 31." };
}
export async function createPurchase(_state: FinanceState, data: FormData) {
  const parsed = purchaseFormSchema.safeParse(Object.fromEntries(data));
  if (!parsed.success) return { error: "Confira cartão, valor, quantidade de parcelas e data." };
  const { creditCardId, ...payload } = parsed.data;
  return save(`/credit-cards/${creditCardId}/purchases`, payload, "/credit-cards");
}

export async function updateAccount(_state: FinanceState, data: FormData) {
  const id = data.get("id");
  const parsed = accountFormSchema.safeParse(Object.fromEntries(data));
  if (typeof id !== "string" || !/^[0-9a-f-]{36}$/i.test(id) || !parsed.success) return { error: "Confira conta e nome." };
  return save(`/accounts/${id}`, parsed.data, "/accounts", "PUT");
}
export async function deleteAccount(_state: FinanceState, data: FormData) {
  const id = data.get("id");
  if (typeof id !== "string" || !/^[0-9a-f-]{36}$/i.test(id)) return { error: "Conta inválida." };
  return save(`/accounts/${id}`, undefined, "/accounts", "DELETE");
}
export async function createTransfer(_state: FinanceState, data: FormData) {
  const parsed = transferFormSchema.safeParse(Object.fromEntries(data));
  return parsed.success ? save("/transfers", parsed.data, "/transactions") : { error: "Confira contas diferentes, valor, descrição e data." };
}
