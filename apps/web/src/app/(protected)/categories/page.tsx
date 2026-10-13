import type { Metadata } from "next";
import { PageHeader } from "@/components/page-header";
import { ResourceList } from "@/features/resources/resource-list";
import { listResources } from "@/services/resources";

export const metadata: Metadata = { title: "Categorias" };
export const dynamic = "force-dynamic";

export default async function CategoriesPage() {
  const categories = await listResources("categories");
  return <><PageHeader title="Cada movimento, um contexto." description="Categorias ajudam a entender de onde seu dinheiro vem e para onde ele vai." /><ResourceList resources={categories} kind="categories" /></>;
}
