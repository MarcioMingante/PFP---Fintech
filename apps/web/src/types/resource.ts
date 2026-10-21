import type { z } from "zod";
import type { resourceSchema } from "@/schemas/resource";

export type Resource = z.infer<typeof resourceSchema>;
export type ResourceKind = "accounts" | "categories";
