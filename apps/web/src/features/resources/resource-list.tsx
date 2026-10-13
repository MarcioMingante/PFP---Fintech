"use client";

import { EmptyState } from "@/components/empty-state";
import { useResourceSearch } from "@/hooks/use-resource-search";
import type { Resource, ResourceKind } from "@/types/resource";

export function ResourceList({ resources, kind }: { resources: Resource[]; kind: ResourceKind }) {
  const { query, setQuery, filtered } = useResourceSearch(resources);
  const label = kind === "accounts" ? "contas" : "categorias";
  return (
    <section className="panel" aria-label={`Lista de ${label}`}>
      <div className="panel-toolbar">
        <div><h2>Suas {label}</h2><p>{resources.length} {resources.length === 1 ? "registro" : "registros"}</p></div>
        <form role="search" onSubmit={(event) => event.preventDefault()}>
          <label className="sr-only" htmlFor={`search-${kind}`}>Buscar {label}</label>
          <input id={`search-${kind}`} type="search" placeholder={`Buscar ${label}…`} value={query} onChange={(event) => setQuery(event.target.value)} />
        </form>
      </div>
      <p className="sr-only" role="status">{filtered.length} resultados</p>
      {resources.length === 0 ? <EmptyState title={`Nenhuma ${kind === "accounts" ? "conta cadastrada" : "categoria cadastrada"}`} description={kind === "accounts" ? "Suas contas aparecerão aqui quando forem cadastradas." : "Organize receitas e despesas com categorias. Elas aparecerão aqui quando forem cadastradas."} />
        : filtered.length === 0 ? <EmptyState title="Nenhum resultado" description="Tente buscar por outro nome." />
        : <ul className="resource-list">{filtered.map((resource) => <li key={resource.id}><span className="resource-icon" aria-hidden="true">{resource.name.charAt(0).toLocaleUpperCase("pt-BR")}</span><span>{resource.name}</span><span className="resource-tag">{kind === "accounts" ? "Conta" : "Categoria"}</span></li>)}</ul>}
    </section>
  );
}
