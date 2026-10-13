import { test, expect, type Page } from "@playwright/test";
import { randomUUID } from "node:crypto";

async function register(page: Page) {
  const credentials = { email: `e2e-${randomUUID()}@example.com`, password: "TestPassword!123" };
  await page.goto("/register");
  await page.getByLabel("E-mail", { exact: true }).fill(credentials.email);
  await page.getByLabel("Senha", { exact: true }).fill(credentials.password);
  await page.getByRole("button", { name: "Criar conta", exact: true }).click();
  await expect(page).toHaveURL(/\/dashboard$/);
  return credentials;
}

test("login, conta, receitas/despesas e dashboard com gráficos", async ({ page }) => {
  const errors: string[] = [];
  page.on("pageerror", error => errors.push(error.message));
  const credentials = await register(page);
  await page.getByRole("button", { name: "Sair", exact: true }).click();
  await expect(page).toHaveURL(/\/login$/);
  await page.getByLabel("E-mail", { exact: true }).fill(credentials.email);
  await page.getByLabel("Senha", { exact: true }).fill("WrongPassword!123");
  await page.getByRole("button", { name: "Entrar", exact: true }).click();
  await expect(page.locator(".auth-form").getByRole("alert")).toHaveText("E-mail ou senha incorretos.");
  await expect(page.getByLabel("E-mail", { exact: true })).toHaveValue(credentials.email);
  await page.getByLabel("Senha", { exact: true }).fill(credentials.password);
  await page.getByRole("button", { name: "Entrar", exact: true }).click();
  await expect(page).toHaveURL(/\/dashboard$/);
  const cookies = await page.context().cookies();
  expect(cookies.filter(cookie => cookie.name.startsWith("fintrack_")).length).toBe(2);
  expect(cookies.filter(cookie => cookie.name.startsWith("fintrack_")).every(cookie => cookie.httpOnly)).toBe(true);
  await page.getByRole("link", { name: "Contas", exact: true }).click();
  await page.getByLabel("Nome da conta", { exact: true }).fill("Conta E2E");
  await page.getByRole("button", { name: "Salvar conta", exact: true }).click();
  await expect(page.getByRole("listitem").filter({ hasText: "Conta E2E" })).toBeVisible();
  await page.reload();
  await expect(page.getByRole("listitem").filter({ hasText: "Conta E2E" })).toBeVisible();
  await page.getByRole("link", { name: "Transações", exact: true }).click();
  for (const [type, description, amount] of [["Income", "Salário E2E", "2000.00"], ["Expense", "Despesa E2E", "59.90"]]) {
    await page.getByLabel("Tipo", { exact: true }).selectOption(type);
    await page.getByLabel("Descrição", { exact: true }).fill(description);
    await page.getByLabel("Valor", { exact: true }).fill(amount);
    await page.getByLabel("Data do lançamento", { exact: true }).fill("2024-03-05");
    await page.getByRole("button", { name: "Salvar lançamento", exact: true }).click();
    await expect(page.getByRole("listitem").filter({ hasText: description })).toBeVisible();
  }
  await page.goto("/dashboard?asOf=2024-03-31");
  await expect(page.locator(".metric-card").filter({ hasText: "Saldo registrado" })).toContainText("1.940,10");
  await expect(page.locator(".metric-card").filter({ hasText: "Receitas do mês" })).toContainText("2.000,00");
  await expect(page.locator(".metric-card").filter({ hasText: "Despesas do mês" })).toContainText("59,90");
  await expect(page.locator(".finance-chart svg")).toHaveCount(2);
  await page.getByText("Ver histórico em tabela", { exact: true }).click();
  await expect(page.getByRole("table").first()).toBeVisible();
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= window.innerWidth)).toBe(true);
  expect(errors).toEqual([]);
});

test("cartão, compra parcelada, centavos e calendário da fatura", async ({ page }) => {
  await register(page);
  await page.getByRole("link", { name: "Cartões", exact: true }).click();
  await page.getByLabel("Nome do cartão", { exact: true }).fill("Cartão E2E");
  await page.getByLabel("Limite", { exact: true }).fill("5000.00");
  await page.getByLabel("Dia de fechamento", { exact: true }).fill("25");
  await page.getByLabel("Dia de vencimento", { exact: true }).fill("5");
  await page.getByRole("button", { name: "Salvar cartão", exact: true }).click();
  await expect(page.getByRole("listitem").filter({ hasText: "Cartão E2E" })).toBeVisible();
  await page.getByLabel("Descrição da compra", { exact: true }).fill("Notebook E2E");
  await page.getByLabel("Valor total", { exact: true }).fill("100.00");
  await page.getByLabel("Parcelas", { exact: true }).fill("3");
  await page.getByLabel("Data da compra", { exact: true }).fill("2024-01-25");
  await page.getByRole("button", { name: "Salvar compra", exact: true }).click();
  const rows = page.getByRole("table").getByRole("row");
  await expect(rows).toHaveCount(4);
  await expect(rows.nth(1)).toContainText("05/03/2024");
  await expect(rows.nth(1)).toContainText("33,34");
  await expect(rows.nth(2)).toContainText("05/04/2024");
  await expect(rows.nth(2)).toContainText("33,33");
  await expect(rows.nth(3)).toContainText("05/05/2024");
  await page.reload();
  await expect(page.getByRole("table").getByRole("row")).toHaveCount(4);
  await page.goto("/dashboard?asOf=2024-03-31");
  await expect(page.locator(".metric-card").filter({ hasText: "Faturas do mês" })).toContainText("33,34");
  await expect(page.locator(".metric-card").filter({ hasText: "Saldo registrado" })).toContainText("0,00");
});

test("sessão encerrada e segundo usuário não acessam dados anteriores", async ({ page }) => {
  await page.goto("/accounts");
  await expect(page).toHaveURL(/\/login$/);
  await register(page);
  await page.goto("/accounts");
  await page.getByLabel("Nome da conta", { exact: true }).fill("Conta privada E2E");
  await page.getByRole("button", { name: "Salvar conta", exact: true }).click();
  await expect(page.getByText("Conta privada E2E", { exact: true })).toBeVisible();
  await page.getByRole("button", { name: "Sair", exact: true }).click();
  await expect(page).toHaveURL(/\/login$/);
  await page.goto("/accounts");
  await expect(page).toHaveURL(/\/login$/);
  await register(page);
  await page.goto("/accounts");
  await expect(page.getByText("Conta privada E2E", { exact: true })).toHaveCount(0);
  await expect(page.getByRole("heading", { name: "Nenhuma conta cadastrada", exact: true })).toBeVisible();
  await page.goto("/dashboard?asOf=invalid");
  await expect(page.getByRole("heading", { name: "Data inválida", exact: true })).toBeVisible();
});
