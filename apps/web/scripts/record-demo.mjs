import { chromium, expect } from '@playwright/test';
import { mkdir, rename } from 'node:fs/promises';
import { resolve } from 'node:path';
import { randomUUID } from 'node:crypto';

const baseURL = process.env.FINTRACK_DEMO_URL;
if (!baseURL || new URL(baseURL).hostname !== '127.0.0.1' || process.env.FINTRACK_DEMO_DISPOSABLE !== '1') {
  throw new Error('Informe FINTRACK_DEMO_URL loopback e FINTRACK_DEMO_DISPOSABLE=1; use somente stack descartável.');
}
const output = resolve('docs/demo/v0.1.0');
await mkdir(output, { recursive: true });
const browser = await chromium.launch();
let context;
try {
  context = await browser.newContext({ baseURL, viewport: { width: 1440, height: 1000 }, recordVideo: { dir: output, size: { width: 1440, height: 1000 } }, reducedMotion: 'reduce' });
  const page = await context.newPage();
  await page.goto('/register');
  await page.getByLabel('E-mail', { exact: true }).fill(`portfolio-${randomUUID()}@example.com`);
  await page.getByLabel('Senha', { exact: true }).fill('DemoFicticia!123');
  await page.getByRole('button', { name: 'Criar conta', exact: true }).click();
  await expect(page).toHaveURL(/\/dashboard$/);
  await page.goto('/accounts');
  await page.getByLabel('Nome da conta', { exact: true }).fill('Conta demonstração');
  await page.getByRole('button', { name: 'Salvar conta', exact: true }).click();
  await expect(page.getByText('Conta demonstração', { exact: true })).toBeVisible();
  await page.goto('/transactions');
  for (const [type, description, amount] of [['Income', 'Salário fictício', '5000.00'], ['Expense', 'Mercado fictício', '350.50']]) {
    await page.getByLabel('Tipo', { exact: true }).selectOption(type);
    await page.getByLabel('Descrição', { exact: true }).fill(description);
    await page.getByLabel('Valor', { exact: true }).fill(amount);
    await page.getByLabel('Data do lançamento', { exact: true }).fill('2024-03-05');
    await page.getByRole('button', { name: 'Salvar lançamento', exact: true }).click();
    await expect(page.getByText(description, { exact: true })).toBeVisible();
  }
  await page.screenshot({ path: resolve(output, 'transactions.png'), fullPage: true });
  await page.goto('/credit-cards');
  for (const [label, value] of [['Nome do cartão', 'Cartão demonstração'], ['Limite', '8000'], ['Dia de fechamento', '25'], ['Dia de vencimento', '5']]) await page.getByLabel(label, { exact: true }).fill(value);
  await page.getByRole('button', { name: 'Salvar cartão', exact: true }).click();
  await expect(page.getByRole('listitem').filter({ hasText: 'Cartão demonstração' })).toBeVisible();
  for (const [label, value] of [['Descrição da compra', 'Notebook fictício'], ['Valor total', '1200'], ['Parcelas', '4'], ['Data da compra', '2024-01-25']]) await page.getByLabel(label, { exact: true }).fill(value);
  await page.getByRole('button', { name: 'Salvar compra', exact: true }).click();
  await expect(page.getByRole('table').getByRole('row')).toHaveCount(5);
  await page.screenshot({ path: resolve(output, 'credit-cards.png'), fullPage: true });
  await page.goto('/dashboard?asOf=2024-03-31');
  await expect(page.locator('.metric-card').filter({ hasText: 'Saldo registrado' })).toContainText('4.649,50');
  await expect(page.locator('.finance-chart svg')).toHaveCount(2);
  await page.screenshot({ path: resolve(output, 'dashboard.png'), fullPage: true });
  const video = page.video();
  await context.close(); context = undefined;
  await rename(await video.path(), resolve(output, 'demo.webm'));
  console.log('Demonstração fictícia gravada em docs/demo/v0.1.0.');
} finally {
  if (context) await context.close();
  await browser.close();
}
