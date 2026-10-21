import { redirect } from "next/navigation";
import { getAccessToken } from "@/services/session";
import { getApiUrl } from "@/config/api";
import { dashboardSummarySchema } from "@/schemas/dashboard";

export async function getDashboardSummary(asOf?: string) {
  const token = await getAccessToken();
  if (!token) redirect("/login");
  const url = new URL("/dashboard/summary", getApiUrl());
  if (asOf) url.searchParams.set("asOf", asOf);
  const response = await fetch(url, {
    headers: { Authorization: `Bearer ${token}` }, cache: "no-store", signal: AbortSignal.timeout(10000),
  });
  if (response.status === 401) redirect("/login?expired=1");
  if (!response.ok) throw new Error("Não foi possível carregar o resumo financeiro.");
  return dashboardSummarySchema.parse(await response.json());
}
