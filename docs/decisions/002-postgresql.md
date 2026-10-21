# 002 — PostgreSQL com EF Core

## Contexto

Contas, usuários, compras, parcelas e faturas precisam de persistência relacional,
valores monetários exatos e gravações atômicas. PostgreSQL já é banco da aplicação;
este registro documenta escolha existente retrospectivamente.

## Decisão

Usar PostgreSQL como fonte de verdade, acessado por EF Core/Npgsql na camada
Infrastructure. Versionar schema com migrations e aplicá-las antes de iniciar API.
Usar decimal no domínio e mapeamentos de precisão no banco, constraints, índices
e chaves estrangeiras conforme cada módulo. SQL específico continua permitido
para locks, outbox e projeções; não exigir abstração genérica sobre todos os acessos.

Redis guarda cache descartável, não substitui persistência financeira (ADR 017).

## Alternativas

- SQLite: facilitaria setup, mas não representaria locks e semântica PostgreSQL
  usados na implementação; não substitui integrações reais.
- Banco documental: exigiria outro desenho de relacionamentos e consistência.
- Somente SQL manual: daria controle explícito, mas ampliaria mapeamento e
  manutenção; SQL pontual e EF Core convivem no desenho adotado.

## Consequências

Banco e migrations são dependências da execução funcional. Integridade exige
transações e constraints além da validação da aplicação. SQL PostgreSQL reduz
portabilidade. Atualização de schema requer backup e análise de compatibilidade;
trocar imagem não desfaz migrations.

## Evidências

- [Persistência e migrations](../../apps/api/src/FinTrack.Infrastructure/Persistence/).
- [Integração real — ADR 015](015-testcontainers-integration.md).
- [Backup e recuperação](../releases/local-release.md).
