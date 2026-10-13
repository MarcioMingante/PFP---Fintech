import type { Metadata } from "next";
import { PageHeader } from "@/components/page-header";
import { EmptyState } from "@/components/empty-state";

export const metadata: Metadata = { title: "Transações" };

export default function TransactionsPage() {
  return <><PageHeader title="Suas movimentações." description="Receitas, despesas e transferências em uma visão organizada." /><section className="panel"><EmptyState title="Transações em breve" description="Estamos preparando este espaço para acompanhar suas movimentações financeiras." /></section></>;
}
