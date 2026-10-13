import { z } from "zod";

export const resourceSchema = z.object({
  id: z.uuid(),
  name: z.string().min(1),
});
export const resourceListSchema = z.array(resourceSchema);
