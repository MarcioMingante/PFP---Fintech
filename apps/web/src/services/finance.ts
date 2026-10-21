import { redirect } from "next/navigation";
import { z } from "zod";
import { getAccessToken } from "@/services/session";
import { getApiUrl } from "@/config/api";
import { cardListSchema, invoiceListSchema, transactionListSchema, transferListSchema } from "@/schemas/finance-forms";
async function read<T>(path: string, schema: z.ZodType<T>): Promise<T> {
  const token = await getAccessToken();
  if (!token) redirect("/login");
  const response = await fetch(new URL(path, getApiUrl()), { headers: { Authorization: `Bearer ${token}` }, cache: "no-store", signal: AbortSignal.timeout(10000) });
  if (response.status === 401) redirect("/login?expired=1");
  if (!response.ok) throw new Error("Não foi possível carregar dados financeiros.");
  return schema.parse(await response.json());
}
export const listTransactions = () => read("/transactions", transactionListSchema);
export const listCreditCards = () => read("/credit-cards", cardListSchema);
export const listInvoices = (cardId: string) => read(`/credit-cards/${cardId}/invoices`, invoiceListSchema);

export const listTransfers = () => read("/transfers", transferListSchema);
