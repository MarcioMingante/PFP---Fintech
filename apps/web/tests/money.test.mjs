import assert from "node:assert/strict";
import test from "node:test";
import { amountSchema, moneySchema, moneyCents } from "../src/schemas/money.ts";
import { cardFormSchema, transactionListSchema } from "../src/schemas/finance-forms.ts";
import { formatMoney } from "../src/utils/finance.ts";

test("money preserves every cent through input, JSON transport and formatting", () => {
  for (const value of ["0.01", "0.10", "100.01", "90071992547409.91", "9999999999999999.99"]) {
    const parsed = amountSchema.parse(value);
    assert.equal(parsed, value);
    assert.equal(JSON.parse(JSON.stringify({ amount: parsed })).amount, value);
  }
  assert.equal(amountSchema.parse("00001.1"), "1.10");
  assert.equal(formatMoney("90071992547409.91"), "R$\u00a090.071.992.547.409,91");
  assert.equal(formatMoney("9999999999999999.99"), "R$\u00a09.999.999.999.999.999,99");
  assert.equal(formatMoney("-0.01"), "-R$\u00a00,01");
  assert.equal(moneyCents("9999999999999999.99"), BigInt("999999999999999999"));
  assert.equal(moneySchema.parse("-0.00"), "0.00");
  // Aggregate balances are not restricted to the individual numeric(18,2) limit.
  assert.equal(moneySchema.parse("19999999999999999.98"), "19999999999999999.98");
});
test("money rejects over-limit values, fractions, exponents and already-rounded numbers", () => {
  for (const value of ["0", "-1", "1.001", "10000000000000000.00", "1e2", "1,10", "NaN", "Infinity", "", 100.01, 9999999999999999.99]) {
    assert.equal(amountSchema.safeParse(value).success, false, String(value));
  }
  const card = cardFormSchema.parse({ name: "Exato", limit: "9999999999999999.99", closingDay: "25", dueDay: "5" });
  assert.equal(card.limit, "9999999999999999.99");
  const item = { id: "44444444-4444-4444-8444-444444444444", accountId: "44444444-4444-4444-8444-444444444444", categoryId: null, recurringTransactionId: null, description: "Exato", amount: "90071992547409.91", type: "Income", occurredOn: "2024-03-05" };
  assert.equal(transactionListSchema.parse([item])[0].amount, item.amount);
  assert.equal(transactionListSchema.safeParse([{ ...item, amount: 90071992547409.91 }]).success, false);
});
