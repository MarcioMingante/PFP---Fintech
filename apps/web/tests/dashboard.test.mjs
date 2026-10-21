import assert from "node:assert/strict";
import test from "node:test";
import { dashboardSummarySchema, dashboardDateSchema } from "../src/schemas/dashboard.ts";
import { formatDate, formatMoney } from "../src/utils/finance.ts";

const empty = {
  asOf: "2024-03-31", currentBalance: "-10.01", incomeThisMonth: "0.00", expensesThisMonth: "10.01",
  creditCardInvoicesThisMonth: "0.00", futureExpenses: "0.00", expensesByCategory: [], invoices: [],
  monthlyHistory: Array.from({ length: 12 }, () => ({ month: "2024-03-01", income: "0.00", expenses: "0.00" })),
  forecast: Array.from({ length: 3 }, () => ({ month: "2024-04-01", recurringExpenses: "0.00", installments: "0.00", total: "0.00" })),
};
test("dashboard accepts negative balance and empty activity, rejects incomplete or invalid totals", () => {
  assert.deepEqual(dashboardSummarySchema.parse(empty), empty);
  for (const value of [{ ...empty, incomeThisMonth: -1 }, { ...empty, forecast: [] }, { ...empty, monthlyHistory: [] }, { ...empty, currentBalance: Infinity }]) {
    assert.equal(dashboardSummarySchema.safeParse(value).success, false);
  }
});
test("dashboard dates reject rollover and out-of-range calendars", () => {
  assert.equal(dashboardDateSchema.safeParse("2024-02-29").success, true);
  for (const date of ["2025-02-29", "0001-01-01", "9999-12-31", "invalid"]) assert.equal(dashboardDateSchema.safeParse(date).success, false);
  assert.equal(formatDate("2024-02-29"), "29/02/2024");
  assert.match(formatMoney("10.01"), /10,01/);
});
