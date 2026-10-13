import { z } from "zod";

const money = z.number().finite();
const nonnegative = money.nonnegative();
export const dashboardDateSchema = z.iso.date().refine(value => value >= "0002-01-01" && value <= "9998-12-01");
export const dashboardSummarySchema = z.object({
  asOf: dashboardDateSchema,
  currentBalance: money,
  incomeThisMonth: nonnegative,
  expensesThisMonth: nonnegative,
  creditCardInvoicesThisMonth: nonnegative,
  futureExpenses: nonnegative,
  expensesByCategory: z.array(z.object({ categoryId: z.uuid().nullable(), name: z.string().min(1), total: nonnegative })),
  monthlyHistory: z.array(z.object({ month: z.iso.date(), income: nonnegative, expenses: nonnegative })).length(12),
  invoices: z.array(z.object({ id: z.uuid(), creditCardId: z.uuid(), cardName: z.string().min(1), closingDate: z.iso.date(), dueDate: z.iso.date(), total: nonnegative })),
  forecast: z.array(z.object({ month: z.iso.date(), recurringExpenses: nonnegative, installments: nonnegative, total: nonnegative })).length(3),
});
export type DashboardSummary = z.infer<typeof dashboardSummarySchema>;
