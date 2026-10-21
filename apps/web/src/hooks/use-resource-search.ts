"use client";

import { useState } from "react";
import type { Resource } from "@/types/resource";
import { normalizeSearch } from "@/utils/text";

export function useResourceSearch(resources: Resource[]) {
  const [query, setQuery] = useState("");
  const search = normalizeSearch(query);
  const filtered = resources.filter((resource) => normalizeSearch(resource.name).includes(search));
  return { query, setQuery, filtered };
}
