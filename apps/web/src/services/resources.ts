import { redirect } from "next/navigation";
import { getAccessToken } from "@/services/session";
import { getApiUrl } from "@/config/api";
import { resourceListSchema } from "@/schemas/resource";
import type { ResourceKind } from "@/types/resource";

export async function listResources(kind: ResourceKind) {
  const token = await getAccessToken();
  if (!token) redirect("/login");
  const response = await fetch(new URL(`/${kind}`, getApiUrl()), {
    headers: { Authorization: `Bearer ${token}` },
    cache: "no-store",
    signal: AbortSignal.timeout(5000),
  });
  if (response.status === 401) redirect("/login?expired=1");
  if (!response.ok) {
    throw new Error("Não foi possível carregar os dados.");
  }
  return resourceListSchema.parse(await response.json());
}
