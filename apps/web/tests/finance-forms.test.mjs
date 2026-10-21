import assert from "node:assert/strict";
import test from "node:test";
import { cardFormSchema, transactionFormSchema, purchaseFormSchema } from "../src/schemas/finance-forms.ts";
const id = "44444444-4444-4444-8444-444444444444";
test("finance forms normalize references and reject invalid money/calendar fields", () => {
  const transaction = { accountId: id, categoryId: "", description: " Salário ", amount: "100.01", type: "Income", occurredOn: "2024-02-29" };
  assert.equal(transactionFormSchema.parse(transaction).categoryId, null);
  assert.equal(transactionFormSchema.parse(transaction).description, "Salário");
  assert.equal(transactionFormSchema.safeParse({ ...transaction, amount: "1.001" }).success, false);
  assert.equal(transactionFormSchema.safeParse({ ...transaction, type: "Transfer" }).success, false);
  assert.equal(cardFormSchema.safeParse({ name: "Card", limit: "100", closingDay: "32", dueDay: "5" }).success, false);
  assert.equal(purchaseFormSchema.safeParse({ creditCardId: id, description: "Compra", amount: "100", installmentCount: "3", purchaseDate: "2024-01-01" }).success, false);
});
