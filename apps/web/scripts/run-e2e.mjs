import { spawn, execFile } from "node:child_process";
import { promisify } from "node:util";
import { createServer } from "node:net";
import { randomUUID } from "node:crypto";
import { fileURLToPath } from "node:url";
import { resolve } from "node:path";
const execute = promisify(execFile);
const web = fileURLToPath(new URL("../", import.meta.url));
const root = resolve(web, "../..");
const children = new Set();
let container;
let cacheContainer;
let interrupted = false;
const env = { ...process.env, FINTRACK_E2E: "1", AUTH_COOKIE_SECURE: "false" };
async function port() {
  const server = createServer();
  await new Promise((ready, reject) => { server.once("error", reject); server.listen(0, "127.0.0.1", ready); });
  const result = server.address().port;
  await new Promise(done => server.close(done));
  return result;
}
function launch(command, args, cwd = root) {
  const child = spawn(command, args, { cwd, env, stdio: "inherit" });
  children.add(child);
  child.once("exit", () => children.delete(child));
  return child;
}
async function run(command, args, cwd = root) {
  if (interrupted) throw new Error("E2E interrompido.");
  const child = launch(command, args, cwd);
  await new Promise((done, reject) => {
    child.once("error", reject);
    child.once("exit", code => code === 0 ? done() : reject(new Error(`${command} encerrou com código ${code}.`)));
  });
}
async function ready(url, child) {
  for (let attempt = 0; attempt < 120; attempt++) {
    if (interrupted || child.exitCode !== null) throw new Error("Servidor E2E encerrou antes de ficar pronto.");
    try { if ((await fetch(url, { signal: AbortSignal.timeout(1000) })).ok) return; } catch { /* Startup polling only. */ }
    await new Promise(done => setTimeout(done, 500));
  }
  throw new Error("Servidor E2E não ficou pronto.");
}
for (const signal of ["SIGINT", "SIGTERM"]) process.on(signal, () => {
  interrupted = true;
  for (const child of children) child.kill("SIGTERM");
});
try {
  await execute("docker", ["info", "--format", "{{.ServerVersion}}"]);
  const password = `test-${randomUUID()}`;
  const name = `fintrack-e2e-${randomUUID()}`;
  container = name; // Allows cleanup even if Docker returns an error after creating it.
  await execute("docker", ["run", "-d", "--rm", "--name", name, "--label", "fintrack.e2e=true", "-e", "POSTGRES_DB=fintrack_e2e", "-e", "POSTGRES_USER=fintrack_test", "-e", `POSTGRES_PASSWORD=${password}`, "-p", "127.0.0.1::5432", "postgres:17-alpine"]);
  let databaseReady = false;
  for (let attempt = 0; attempt < 60; attempt++) {
    if (interrupted) throw new Error("E2E interrompido.");
    try { await execute("docker", ["exec", name, "pg_isready", "-U", "fintrack_test", "-d", "fintrack_e2e"]); databaseReady = true; break; } catch { await new Promise(done => setTimeout(done, 500)); }
  }
  if (!databaseReady) throw new Error("PostgreSQL E2E não ficou pronto.");
  const { stdout } = await execute("docker", ["port", name, "5432/tcp"]);
  const databasePort = Number(stdout.trim().split(":").at(-1));
  cacheContainer = `fintrack-e2e-redis-${randomUUID()}`;
  await execute("docker", ["run", "-d", "--rm", "--name", cacheContainer, "--label", "fintrack.e2e=true", "-p", "127.0.0.1::6379", "redis:7-alpine"]);
  let cacheReady = false;
  for (let attempt = 0; attempt < 60; attempt++) {
    if (interrupted) throw new Error("E2E interrompido.");
    try { const ping = await execute("docker", ["exec", cacheContainer, "redis-cli", "ping"]); if (ping.stdout.trim() === "PONG") { cacheReady = true; break; } } catch { /* Startup polling. */ }
    await new Promise(done => setTimeout(done, 500));
  }
  if (!cacheReady) throw new Error("Redis E2E não ficou pronto.");
  const redisPort = Number((await execute("docker", ["port", cacheContainer, "6379/tcp"])).stdout.trim().split(":").at(-1));
  env.ConnectionStrings__Redis = `127.0.0.1:${redisPort}`;
  env.DashboardCache__Enabled = "true";
  env.DashboardCache__TtlSeconds = "60";
  const apiPort = await port(); const webPort = await port();
  env.ConnectionStrings__Database = `Host=127.0.0.1;Port=${databasePort};Database=fintrack_e2e;Username=fintrack_test;Password=${password}`;
  env.Jwt__SigningKey = `e2e-${randomUUID()}-${randomUUID()}`;
  env.Auth__RequestsPerMinute = "1000";
  env.BackgroundJobs__Enabled = "false";
  env.Messaging__Enabled = "false";
  env.Logging__LogLevel__Default = "Warning";
  env.ASPNETCORE_ENVIRONMENT = "Testing";
  env.ASPNETCORE_URLS = `http://127.0.0.1:${apiPort}`;
  env.API_URL = env.ASPNETCORE_URLS;
  env.FINTRACK_E2E_BASE_URL = `http://127.0.0.1:${webPort}`;
  await run("dotnet", ["build", "apps/api/src/FinTrack.Api", "-m:1"]);
  const apiDll = resolve(root, "apps/api/src/FinTrack.Api/bin/Debug/net10.0/FinTrack.Api.dll");
  await run("dotnet", [apiDll, "--migrate"]);
  await run(process.execPath, ["node_modules/next/dist/bin/next", "build", "--webpack"], web);
  const api = launch("dotnet", [apiDll]);
  await ready(`${env.API_URL}/health`, api);
  const frontend = launch(process.execPath, ["node_modules/next/dist/bin/next", "start", "--hostname", "127.0.0.1", "--port", String(webPort)], web);
  await ready(`${env.FINTRACK_E2E_BASE_URL}/login`, frontend);
  await run(process.execPath, ["node_modules/@playwright/test/cli.js", "test", ...process.argv.slice(2)], web);
} catch (error) {
  console.error(error.message);
  process.exitCode = 1;
} finally {
  for (const child of [...children]) {
    const exited = new Promise(done => child.once("exit", done));
    child.kill("SIGTERM");
    await Promise.race([exited, new Promise(done => setTimeout(done, 5000))]);
    if (child.exitCode === null && child.signalCode === null) child.kill("SIGKILL");
  }
  for (const ownedContainer of [cacheContainer, container].filter(Boolean)) {
    try { await execute("docker", ["rm", "-f", ownedContainer]); }
    catch { console.error("Confira limpeza do container E2E:", ownedContainer); process.exitCode = 1; }
  }
}
