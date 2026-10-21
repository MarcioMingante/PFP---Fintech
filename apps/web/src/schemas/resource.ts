import { z } from "zod";

import { moneySchema } from "./money.ts";

export const resourceSchema = z.object({
  id: z.uuid(),
  name: z.string().min(1),
  balance: moneySchema.optional(),
});
export const resourceListSchema = z.array(resourceSchema);
