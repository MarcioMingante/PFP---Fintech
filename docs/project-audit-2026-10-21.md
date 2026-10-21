# Auditoria de milestones — 21/10/2026

## Atualização após correções

Auditoria abaixo preserva estado inicial. M0, M3, M4 e precisão monetária foram
tratados posteriormente na mesma data; consulte [entrega e validações](milestone-completion-2026-10-21.md).
Pendências de pagamentos, outras interfaces e readiness continuam separadas.

## Resultado

**Projeto tem base funcional validada, mas não está integralmente concluído.**
Fases de infraestrutura, estudos e apresentação avançaram apesar de lacunas nas
fases 0, 6 e 10. Chegar à fase 33 não comprova conclusão sequencial das anteriores.
Há também defeito monetário reproduzido no frontend e escolha de data UTC que
precisa ser confirmada como regra do produto.

Revisão compara [roadmap](personal-finance-platform-roadmap.md), código,
configuração, ADRs, artefatos de release e testes selecionados. Não certifica
ausência de bugs, segurança completa ou funcionamento de todos os cenários.
Não foram alterados código da aplicação, schema ou banco pessoal.

## Matriz dos milestones

Roadmap lista objetivos amplos, sem critérios formais para cada milestone.
Mapeamento abaixo usa fases relacionadas e distingue entrega técnica de produto
completo. “Entregue no escopo” significa evidência presente, não execução de
cada ferramenta ou cenário novamente nesta auditoria.

| Milestone | Fases relacionadas | Situação | Evidência ou pendência |
| --- | --- | --- | --- |
| M0 — Planning | 0 | Parcial | Roadmap e ADRs existem; documentos separados de visão, requisitos, regras e casos de uso previstos na fase 0 não existem |
| M1 — Docker Foundation | 1–2 | Entregue no escopo | Monorepo, Compose, PostgreSQL/volume, `.editorconfig`, `.env.example`, setup e Makefile |
| M2 — Core Backend | 3–5 | Entregue no escopo | Camadas, EF/migrations e CRUD de contas; dependências verificadas por testes |
| M3 — Transactions | 6 | Parcial | Receita/despesa e listagem implementadas; falta transferência atômica e modelo/fluxo de saldo por conta previsto na fase 6 |
| M4 — Frontend | 9–12 | Parcial | Navegação, leitura e criação integradas; editar/excluir contas exigidos na fase 10 não estão na web |
| M5 — Authentication | 13 | Entregue no escopo | Registro/login/refresh/logout e propriedade; integrações e E2E selecionados aprovados. MFA/recuperação de senha são evolução, não exigência original desta fase |
| M6 — Credit Cards | 14 | Entregue no escopo backend | CRUD API; frontend cria/lista. Editar/excluir cartão na web ainda não existe |
| M7 — Financial Engine | 15–17 | Entregue no escopo backend | Parcelamento, calendário, faturas e recorrências; pagamentos/quitação e gestão web de recorrências seguem fora da entrega atual |
| M8 — Dashboard | 18 | Entregue com limitações | Agregações, gráficos e projeções; E2E validou saldo de receitas/despesas. Sem saldo inicial, transferências ou pagamentos |
| M9 — Testing | 7, 19–20 | Infraestrutura entregue; cobertura parcial | xUnit/Testcontainers/Playwright presentes e seleção aprovada; transferências ausentes também não possuem testes; não houve suíte completa nesta revisão |
| M10 — Redis & Background Jobs | 21–22 | Entregue no escopo | Cache por revisão e worker de recorrências; integrações selecionadas aprovadas |
| M11 — Messaging | 23, 29 | Entregue no escopo local | Outbox, dois consumidores, retries e DLQs; integrações selecionadas aprovadas; notificações são registros locais |
| M12 — Observability | 24 | Entregue no escopo local | Logs JSON, tracing e métricas; integrações selecionadas aprovadas; coleta ponta a ponta das UIs não foi repetida nesta revisão |
| M13 — CI e releases locais | 25–27, 31 | Entregue no escopo local | Workflow preparado, imagens/tag `v0.1.0`, release e backup; não comprova execução do Actions remoto; GHCR excluído por decisão do proprietário |
| M14 — Cloud conceitual | 28–29 | Entregue documentalmente | Estudo/diagrama AWS e comparação SQS; implantação não faz parte do caminho escolhido |
| M15 — IaC local | 30 | Laboratório entregue | Arquivos Terraform, lockfile, módulo, Make e documentação presentes; apply/destroy não repetidos nesta auditoria |
| M16 — Portfolio Release | 31–33 | Entregue como apresentação local | Release, imagens, screenshots, vídeo, 23 ADRs e README; não significa produto funcional completo |

## Achados priorizados

### 1. Perda de precisão monetária — média

[Schema de dinheiro](../apps/web/src/schemas/finance-forms.ts) converte entrada
para `number` antes de validar e aceita faixa além da precisão decimal segura.
Reprodução executada diretamente com `cardFormSchema`:

| Entrada textual | Aceita | Valor após conversão |
| --- | --- | --- |
| `9999999999999999.99` | Sim | `10000000000000000` |
| `90071992547409.91` | Sim | `90071992547409.9` |

Mesmo com testes aprovados, schema altera valor informado; segundo exemplo
perde um centavo. DTOs de leitura também usam `number`. Corrigir contrato de
dinheiro ponta a ponta com representação exata, ou definir limite seguro e
validar entrada antes da conversão. Incluir regressão de fronteiras e leituras.
Achado da auditoria de 13/10 permanece aberto.

### 2. M3 e M4 incompletos — lacunas de escopo

[TransactionType](../apps/api/src/FinTrack.Domain/Transactions/Transaction.cs)
só define Income/Expense. [API](../apps/api/src/FinTrack.Api/Program.cs) não mapeia
transferências nem edição/exclusão de lançamentos. [Account](../apps/api/src/FinTrack.Domain/Accounts/Account.cs)
contém nome/proprietário e não modela saldo individual.

Fase 6 exige transferência com débito/crédito atômicos; ainda falta implementar
fluxo, invariantes e testes. Saldo agregado de dashboard não substitui esse caso.
Fase 10 exige edição/exclusão de contas na web; [formulários atuais](../apps/web/src/features/finance/forms.tsx)
e [ações](../apps/web/src/features/finance/actions.ts) implementam criação.
Edição/exclusão de lançamentos e pagamentos são pendências conhecidas do produto,
mas não devem ser confundidas com requisitos explicitamente enumerados na fase 10.

### 3. Dia financeiro em UTC — decisão de produto pendente

Frontend usa `new Date().toISOString().slice(0, 10)`; serviços/jobs usam data UTC.
Reprodução: instante `2026-10-22T00:30:00Z` vira `2026-10-22` no código, mas
corresponde a `2026-10-21` em São Paulo. Existe divergência da data local entre
21h e meia-noite; não existe requisito formal de fuso que permita declarar UTC
incorreto em todos os contextos. Definir fuso financeiro e testar virada do dia
antes de usar regra como expectativa para usuário brasileiro.

### 4. Faturas e limite têm escopo limitado

[InvoiceService](../apps/api/src/FinTrack.Application/Invoices/InvoiceService.cs)
soma todas as parcelas históricas para limite utilizado. Sem pagamento, tempo
não libera limite; compras acima do limite são permitidas. Comportamento consta
no [ADR 012](decisions/012-credit-card-invoices.md), portanto é limitação deliberada,
não regressão comprovada. Se objetivo for cartão completo, falta ciclo de quitação,
liberação de limite e integração financeira com contas.

### 5. Readiness e operação ainda limitadas

`GET /health` retorna resposta fixa; não consulta banco, migrations, Redis ou
RabbitMQ. Healthcheck verde não comprova ambiente completo. Adicionar readiness
e identificação de versão seria evolução útil. Retenção da outbox, replay e
alertas operacionais também permanecem fora da implementação atual.

Não foi atualizado nem iniciado Compose pessoal. Imagens locais da release
existem e IDs coincidem com [registro v0.1.0](releases/v0.1.0.md); isso não comprova
que ambiente pessoal esteja executando essa versão.

### 6. Planejamento e status histórico incompletos

Arquivos `docs/product/vision.md`, `requirements.md`, `business-rules.md` e
`use-cases.md` previstos na fase 0 não existem. ADRs/README cobrem parte do
conteúdo, sem substituir critérios de aceitação formais por funcionalidade.

Roadmap mantém observações históricas, como TransactionTests ausentes na fase 7
e interface de faturas pendente na fase 16, apesar de lançamentos e tabela de
faturas introduzidos posteriormente. Checklists históricos não devem ser usados
como inventário atual. Matriz consolidada foi adicionada ao roadmap nesta revisão.
Não houve mudança automática dos critérios nem marcação de funcionalidades ausentes
como concluídas.

## Validações executadas agora

| Verificação | Resultado |
| --- | --- |
| Testes de arquitetura .NET | 4 aprovados, zero ignorados |
| Integrações selecionadas | 49 aprovadas, zero falhas/ignorados |
| Frontend `npm test` | 3 testes aprovados |
| ESLint e typecheck | Aprovados |
| Build API e Next.js pelo runner E2E | Aprovados; API sem warnings/erros |
| E2E Chromium desktop principal | 1 aprovado, sem retries |
| Compose config com valores fictícios | Aprovado sem exibir configuração resolvida |
| Sintaxe shell de setup/backup | Aprovada |
| Precisão monetária e data UTC | Reproduções acima confirmadas |

Seleção de integrações: AuthenticationTests, ManualTransactionEndpointTests,
PurchaseEndpointTests, InvoiceEndpointTests, DashboardCacheTests,
BackgroundJobTests, MessagingTests e ObservabilityTests. Testcontainers usa bancos,
cache e broker descartáveis. E2E cobre cadastro, login inválido/válido, logout,
conta persistida, receita/despesa, saldo esperado e gráficos.

Build E2E emite aviso de `next start` com `output: standalone`; cenário passou.
Dockerfile usa servidor standalone. Aviso do runner não foi tratado como falha
funcional do container de produção.

Não executados: suíte completa, E2E mobile/parcelamento nesta revisão, Terraform
apply/destroy, nova rodada de backup/restore, build Docker, publicação ou CI remoto.
Validações anteriores de release/laboratório permanecem evidências históricas,
sem atribuir execução nova a esta auditoria. Arquivo gerado `next-env.d.ts` foi
restaurado ao conteúdo inicial após testes.

## Próximos passos recomendados

1. Corrigir precisão monetária e definir regra de fuso financeiro.
2. Completar M3: transferência atômica e saldo por conta, com testes de domínio e integração.
3. Completar M4: edição/exclusão de contas na interface; depois priorizar demais fluxos web.
4. Consolidar requisitos/critérios da fase 0 e atualizar pendências por funcionalidade.
5. Planejar pagamentos e readiness conforme objetivo de uso, sem exigir cloud/GHCR.

Esta auditoria registra achados e status; não implementa essas correções.
