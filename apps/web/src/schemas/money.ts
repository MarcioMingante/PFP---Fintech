import { z } from "zod";

// Never coerce money through Number: even valid numeric(18,2) values may lose cents.
export function normalizeMoney(value: string): string {
  const negative = value.startsWith("-");
  const [whole, fraction = ""] = value.replace(/^-/, "").split(".");
  const integer = whole.replace(/^0+(?=\d)/, "");
  const cents = fraction.padEnd(2, "0");
  return `${negative && (integer !== "0" || cents !== "00") ? "-" : ""}${integer}.${cents}`;
}
export const moneySchema = z.string().regex(/^-?\d{1,29}(\.\d{1,2})?$/).transform(normalizeMoney);
export function moneyCents(value: string): bigint {
  const normalized = normalizeMoney(value);
  const negative = normalized.startsWith("-");
  const digits = normalized.replace(/[-.]/g, "");
  return BigInt(digits) * BigInt(negative ? -1 : 1);
}
export const nonnegativeMoneySchema = moneySchema.refine(value => moneyCents(value) >= BigInt(0));
export const amountSchema = moneySchema.refine(value => {
  const cents = moneyCents(value);
  return cents > BigInt(0) && cents <= BigInt("999999999999999999");
}, "Use valor positivo de até 9999999999999999.99, com duas casas decimais.");
