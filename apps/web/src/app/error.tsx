"use client";

import { useTransition } from "react";
import { useRouter } from "next/navigation";

export default function ErrorPage({ reset }: { error: Error & { digest?: string }; reset: () => void }) {
  const router = useRouter();
  const [pending, startTransition] = useTransition();
  function retry() {
    startTransition(() => { router.refresh(); reset(); });
  }
  return <section className="panel error-state" role="alert"><p className="eyebrow">ALGO NÃO SAIU COMO ESPERADO</p><h1>Não conseguimos carregar suas informações.</h1><p>O serviço pode estar indisponível. Tente novamente em instantes.</p><button className="button" onClick={retry} disabled={pending}>{pending ? "Tentando novamente…" : "Tentar novamente"}</button></section>;
}
