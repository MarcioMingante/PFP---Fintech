import { defineConfig, devices } from "@playwright/test";
const baseURL = process.env.FINTRACK_E2E_BASE_URL;
if (process.env.FINTRACK_E2E !== "1" || !baseURL || new URL(baseURL).hostname !== "127.0.0.1") {
  throw new Error("Execute npm run e2e para iniciar ambiente local isolado.");
}
export default defineConfig({
  testDir: "./e2e", fullyParallel: true, workers: 2, retries: 0, timeout: 45000,
  expect: { timeout: 10000 }, forbidOnly: !!process.env.CI,
  reporter: process.env.CI ? [["list"]] : [["list"], ["html", { open: "never" }]],
  use: { baseURL, trace: "retain-on-failure", screenshot: "only-on-failure", video: "retain-on-failure" },
  projects: [
    { name: "chromium", use: { ...devices["Desktop Chrome"] } },
    { name: "mobile-chromium", use: { ...devices["Pixel 7"], defaultBrowserType: "chromium" } },
  ],
});
