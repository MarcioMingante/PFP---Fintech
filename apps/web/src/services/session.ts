import { cookies } from "next/headers";
import { redirect } from "next/navigation";
import { getApiUrl } from "@/config/api";
import { sessionSchema } from "@/schemas/auth";

export async function getAccessToken() {
  return (await cookies()).get("fintrack_access")?.value;
}

export async function requireSession() {
  const token = await getAccessToken();
  if (!token) redirect("/login");
  const response = await fetch(new URL("/auth/me", getApiUrl()), {
    headers: { Authorization: `Bearer ${token}` }, cache: "no-store", signal: AbortSignal.timeout(5000),
  });
  if (response.status === 401) redirect("/login?expired=1");
  if (!response.ok) throw new Error("Não foi possível verificar sessão.");
  return sessionSchema.parse(await response.json());
}
