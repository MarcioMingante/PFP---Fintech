import { AccountForm } from "@/features/finance/forms";
import type { Metadata } from "next";
import { PageHeader } from "@/components/page-header";
import { ResourceList } from "@/features/resources/resource-list";
import { listResources } from "@/services/resources";

export const metadata: Metadata = { title: "Contas" };
export const dynamic = "force-dynamic";

export default async function AccountsPage() {
  const accounts = await listResources("accounts");
  return <><PageHeader title="Suas contas." description="Todas as suas contas, organizadas em um único lugar." /><AccountForm /><ResourceList resources={accounts} kind="accounts" /></>;
}
