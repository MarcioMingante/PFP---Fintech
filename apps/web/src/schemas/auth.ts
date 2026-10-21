import { z } from "zod";

export const credentialsSchema = z.object({
  email: z.email().max(254),
  password: z.string().min(12, "Senha deve conter pelo menos 12 caracteres.").max(128),
});
export const tokensSchema = z.object({
  accessToken: z.string().min(1), expiresAt: z.iso.datetime({ offset: true }),
  refreshToken: z.string().min(1), refreshExpiresAt: z.iso.datetime({ offset: true }),
});
export const sessionSchema = z.object({ id: z.uuid(), email: z.email() });
