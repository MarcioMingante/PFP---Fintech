# Architecture Decision Records

Decisões de arquitetura do FinTrack. Este índice consolida fase 32; registros
continuam acompanhando mudanças futuras. Conteúdo descreve escolhas e seus
limites, não comprova conclusão integral de funcionalidades ou deploy externo.

## Catálogo

Situação documental consolidada nesta revisão: decisões listadas estão aceitas
no escopo indicado. Os ADRs 001–004 foram registrados retrospectivamente com base
nos arquivos atuais; não se atribui data histórica de aprovação. ADR 024 aceita
um estudo, não implantação AWS. ADR 026 define exercício opcional separado da stack.

| ADR | Decisão | Escopo |
| --- | --- | --- |
| 001 | [Backend monolítico em camadas](001-layered-monolith.md) | Implementação/fluxo local |
| 002 | [PostgreSQL com EF Core](002-postgresql.md) | Implementação/fluxo local |
| 003 | [Docker Compose como ambiente executável local](003-docker-compose.md) | Implementação/fluxo local |
| 004 | [Frontend Next.js com App Router](004-nextjs.md) | Implementação/fluxo local |
| 009 | [Autenticação e propriedade dos recursos](009-authentication.md) | Implementação/fluxo local |
| 010 | [Cartões de crédito como aggregate](010-credit-cards.md) | Implementação/fluxo local |
| 011 | [Compras parceladas e calendário](011-installment-purchases.md) | Implementação/fluxo local |
| 012 | [Faturas persistidas e limite disponível](012-credit-card-invoices.md) | Implementação/fluxo local |
| 013 | [Recorrências mensais e geração idempotente](013-recurring-transactions.md) | Implementação/fluxo local |
| 014 | [Dashboard por modelos de leitura](014-dashboard-read-model.md) | Implementação/fluxo local |
| 015 | [PostgreSQL efêmero com Testcontainers](015-testcontainers-integration.md) | Implementação/fluxo local |
| 016 | [Fluxos E2E com Playwright](016-playwright-e2e.md) | Implementação/fluxo local |
| 017 | [Redis com revisão transacional do dashboard](017-redis-dashboard-cache.md) | Implementação/fluxo local |
| 018 | [Recorrências em BackgroundService](018-recurring-background-jobs.md) | Implementação/fluxo local |
| 019 | [TransactionCreated com RabbitMQ e outbox](019-transaction-messaging.md) | Implementação/fluxo local |
| 020 | [Observabilidade local](020-local-observability.md) | Implementação/fluxo local |
| 021 | [CI público sem cobrança](021-free-public-ci.md) | Workflow preparado; execução remota não comprovada |
| 022 | [Imagens Docker locais](022-local-container-images.md) | Implementação/fluxo local |
| 023 | [Entrega funcional local](023-local-functional-delivery.md) | Implementação/fluxo local |
| 024 | [Arquitetura AWS conceitual](024-cloud-conceptual-architecture.md) | Estudo AWS exclusivamente documental; não implantado |
| 025 | [Mensageria local e estudo de SQS](025-local-messaging-sqs-comparison.md) | RabbitMQ real; comparação SQS/SNS documental |
| 026 | [Exercício Terraform Docker local](026-local-terraform-lab.md) | Laboratório local opcional |
| 027 | [Release versionada local](027-local-release.md) | Implementação/fluxo local |
| 028 | [Transferências atômicas e saldos derivados](028-transfers-and-balances.md) | Implementação local |
| 029 | [Dinheiro exato na API e frontend](029-exact-money-json.md) | Implementação local |

Os números 005–008 não têm registros neste diretório. Nomes apresentados como
exemplos no roadmap não constituem ADRs existentes: JWT está no 009, Redis no
017, RabbitMQ no 019 e cloud conceitual no 024. Não duplicar decisões nem
renumerar links históricos apenas para preencher lacunas.

## Registrar e revisar decisões

1. Copiar [template](template.md), usando próximo número livre após maior ADR
   existente e nome curto em kebab-case. Após entrega M0/M3/M4, próximo é 030.
2. Preencher contexto, decisão, alternativas e consequências; incluir situação,
   escopo e evidências que permitam revisão. Documentos em português usam esses
   títulos como equivalentes a Context, Decision, Alternatives e Consequences.
3. Explicar impacto concreto e distinguir execução real de proposta. Revisar
   código/configuração pertinentes; não registrar validação não executada.
4. Atualizar catálogo e links relacionados na mesma mudança de arquitetura.
   Durante revisão, manter proposta explícita até decisão ser aceita.
5. Quando decisão mudar, criar novo ADR referenciando anterior, marcar antigo
   como substituído e ligar ambos. Correções e complementos sem mudança de escolha
   podem ser feitos no próprio registro, preservando fatos históricos relevantes.

Os ADRs anteriores mantêm estilo e evidências das fases originais; complementos
da fase 32 explicitam alternativas/limites conforme implementação atual, sem
inventar avaliações históricas. Evidências de testes antigas não significam nova
execução nesta revisão documental.

## Validação da fase 32

Conferidos IDs únicos, cobertura do catálogo, quatro seções essenciais em todos
os ADRs e links locais. Decisões iniciais comparadas com referências dos projetos
.NET, testes de arquitetura, persistência, Compose e frontend. Sem alterações de
runtime; testes da aplicação não executados nesta fase documental.
