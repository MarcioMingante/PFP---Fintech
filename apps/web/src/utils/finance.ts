// Format integer and fractional digits separately, without IEEE-754 conversion.
export const formatMoney = (value: string) => {
  const negative = value.startsWith("-");
  const [whole, fraction = "00"] = value.replace(/^-/, "").split(".");
  const grouped = BigInt(whole).toLocaleString("pt-BR");
  return `${negative ? "-" : ""}R$\u00a0${grouped},${fraction.padEnd(2, "0")}`;
};
export const formatMonth = (value: string) => new Intl.DateTimeFormat("pt-BR", { month: "short", year: "2-digit", timeZone: "UTC" }).format(new Date(`${value}T00:00:00Z`));
export const formatDate = (value: string) => new Intl.DateTimeFormat("pt-BR", { timeZone: "UTC" }).format(new Date(`${value}T00:00:00Z`));
