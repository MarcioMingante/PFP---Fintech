import assert from "node:assert/strict";
import test from "node:test";
import { resourceListSchema } from "../src/schemas/resource.ts";
import { normalizeSearch } from "../src/utils/text.ts";

test("API lists accept UUIDs and preserve Portuguese names", () => {
  const resources = [{ id: "44444444-4444-4444-8444-444444444444", name: "Alimentação" }];
  assert.deepEqual(resourceListSchema.parse(resources), resources);
  assert.deepEqual(resourceListSchema.parse([]), []);
});

test("API lists reject invalid IDs, empty names and missing fields", () => {
  for (const resources of [
    [{ id: "invalid", name: "Conta" }],
    [{ id: "44444444-4444-4444-8444-444444444444", name: "" }],
    [{ name: "Conta" }],
    { accounts: [] },
  ]) {
    assert.equal(resourceListSchema.safeParse(resources).success, false);
  }
});

test("name search ignores accents, case and surrounding spaces", () => {
  assert.equal(normalizeSearch("  ALIMENTAÇÃO  "), "alimentacao");
  assert.ok(normalizeSearch("Poupança familiar").includes(normalizeSearch("POUPANCA")));
  assert.equal(normalizeSearch("   "), "");
});
