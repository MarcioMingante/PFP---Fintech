import { z } from "zod";
import { amountSchema as amount, moneySchema } from "./money.ts";
const name = z.string().trim().min(1).max(100);
const description = z.string().trim().min(1).max(200);
const categoryId = z.preprocess(value => value === "" || value === null ? null : value, z.uuid().nullable());
export const accountFormSchema = z.object({ name });
export const cardFormSchema = z.object({ name, limit: amount, closingDay: z.coerce.number().int().min(1).max(31), dueDay: z.coerce.number().int().min(1).max(31) });
export const transactionFormSchema = z.object({ accountId: z.uuid(), categoryId, description, amount, type: z.enum(["Income", "Expense"]), occurredOn: z.iso.date() });
export const purchaseFormSchema = z.object({ creditCardId: z.uuid(), description, amount, installmentCount: z.coerce.number().int().min(1).max(120), purchaseDate: z.iso.date(), idempotencyKey: z.uuid() });
export const transactionListSchema = z.array(z.object({ id: z.uuid(), accountId: z.uuid(), categoryId: z.uuid().nullable(), recurringTransactionId: z.uuid().nullable(), description, amount: moneySchema, type: z.enum(["Income", "Expense"]), occurredOn: z.iso.date() }));
export const cardListSchema = z.array(z.object({ id: z.uuid(), name, limit: moneySchema, closingDay: z.number().int(), dueDay: z.number().int() }));
export const invoiceListSchema = z.array(z.object({ id: z.uuid(), creditCardId: z.uuid(), closingDate: z.iso.date(), dueDate: z.iso.date(), total: moneySchema }));

export const transferFormSchema = z.object({ sourceAccountId: z.uuid(), destinationAccountId: z.uuid(), description, amount, occurredOn: z.iso.date(), idempotencyKey: z.uuid() }).refine(value => value.sourceAccountId !== value.destinationAccountId);
export const transferListSchema = z.array(z.object({ id: z.uuid(), sourceAccountId: z.uuid(), destinationAccountId: z.uuid(), description, amount: moneySchema, occurredOn: z.iso.date(), idempotencyKey: z.uuid() }));
export const accountBalanceListSchema = z.array(z.object({ id: z.uuid(), name, balance: moneySchema }));
