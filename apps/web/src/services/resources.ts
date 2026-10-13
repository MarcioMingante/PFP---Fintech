import { getApiUrl } from "@/config/api";
import { resourceListSchema } from "@/schemas/resource";
import type { ResourceKind } from "@/types/resource";

export async function listResources(kind: ResourceKind) {
  const response = await fetch(new URL(`/${kind}`, getApiUrl()), {
    cache: "no-store",
    signal: AbortSignal.timeout(5000),
  });
  if (!response.ok) {
    throw new Error("Não foi possível carregar os dados.");
  }
  return resourceListSchema.parse(await response.json());
}
