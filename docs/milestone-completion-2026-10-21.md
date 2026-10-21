# Entrega M0/M3/M4 e precisão monetária — 21/10/2026

## Resultado

Concluídos critérios dos três milestones auditados, sem declarar encerradas todas
as evoluções do produto. Corrigida perda de centavos na entrada, transporte HTTP,
leitura e exibição da web. [Auditoria inicial](project-audit-2026-10-21.md) permanece
como histórico anterior às correções.

## Entregas

| Escopo | Resultado | Evidências |
| --- | --- | --- |
| M0 — Planejamento | Visão, requisitos com critérios, regras e casos de uso | [Documentos de produto](product/vision.md) |
| M3 — Transações | Receita/despesa existentes mais transferência atômica/idempotente e saldo por conta | [ADR 028](decisions/028-transfers-and-balances.md), `/transfers`, `/accounts/balances` |
| M4 — Frontend da fase 10 | Contas com criação/listagem/busca/edição/exclusão, confirmação/cancelamento e erros; lançamentos com criação/listagem | [Formulários](../apps/web/src/features/finance/forms.tsx), [controles de conta](../apps/web/src/features/finance/account-controls.tsx), E2E |
| Precisão | Decimal no backend/banco, strings monetárias HTTP e validação/formatação por string/BigInt na web | [ADR 029](decisions/029-exact-money-json.md), [regressões](../apps/web/tests/money.test.mjs) |

Transferência é um registro com as duas contas próprias, sem Income/Expense
artificial. Mesma chave/conteúdo não duplica; chave conflitante retorna 409.
Saldo é agregado do histórico até hoje UTC e pode ser negativo. Não existe
saldo inicial, autorização por fundos disponíveis ou pagamento de cartão.

Formulários usam texto com teclado decimal para preservar dígitos antes da
validação. Limite individual mantém numeric(18,2); totais podem excedê-lo dentro
do alcance decimal do backend. Gráficos convertem apenas coordenadas para Number;
valores de tooltips e tabelas permanecem exatos.

## Verificações realizadas

- 53 casos de integração pertinentes aprovados: 49 dos demais cenários selecionados
  e 4 de transferência, cuja classe foi reexecutada após ajustar teste de falha
  ao comportamento de exceções do TestServer. Zero falhas pendentes/ignorados.
- Transferências: concorrência na mesma chave e em chaves distintas, conservação
  dos saldos, isolamento, chave por usuário, FK de propriedade, exclusão em uso e
  rollback da transferência/revisão de cache após falha injetada.
- Precisão: valores `90071992547409.91` e `9999999999999999.99` preservados em
  requests/responses e PostgreSQL; agregados acima do teto individual exatos;
  parcelas mantêm soma; entradas inválidas rejeitadas; request numérico legado aceito.
- 2 testes de domínio de transferências e 4 testes de arquitetura aprovados.
- 4 arquivos de testes frontend aprovados, incluindo regressões de dinheiro;
  lint, typecheck e builds de API/Next.js aprovados.
- 10 E2E afetados aprovados, sem retries: desktop/mobile em autenticação, contas,
  lançamentos, cartões/parcelas, dashboard, transferências e precisão.
- Após ajuste final de entrada textual e ampliação de cancelamento/validação de
  edição e limite máximo de cartão, 4 E2E novos reexecutados e aprovados.
- Migration AddTransfers aplicada pelos testes a bancos vazios e upgrades das
  fixtures com registros legados. EF confirmou ausência de mudanças pendentes
  entre modelo e snapshot. Nenhuma migration executada no banco pessoal.

Integrações selecionadas: TransferEndpointTests, MoneyPrecisionEndpointTests,
ManualTransactionEndpointTests, ResourceEndpointTests, CreditCardEndpointTests,
PurchaseEndpointTests, InvoiceEndpointTests, RecurringTransactionEndpointTests,
DashboardEndpointTests, DashboardCacheTests, BackgroundJobTests e MessagingTests.
Foram usados containers/processos descartáveis, encerrados pelos runners.

Não foi executada suíte completa de funcionalidades não afetadas. Não houve
publicação, push, nova tag, rebuild de `v0.1.0` ou atualização do ambiente pessoal.

## Aplicar em ambiente existente

Migration `20261021115150_AddTransfers` cria ledger/índices/FKs; não reescreve valores
financeiros anteriores nem atribui contas legadas a usuários. Faça backup conforme
[procedimento local](releases/local-release.md) e atualize API/frontend juntos.
`make full` constrói código atual e executa migrador antes da API. Para preservar
executáveis anteriores, use nova tag de imagem conforme procedimento de release.

Resposta monetária da API agora usa strings decimais: consumidores próprios
precisam adaptar leitura. Entradas JSON numéricas antigas continuam aceitas, mas
não recuperam precisão já perdida antes de chegar à API. Registros eventualmente
corrompidos no passado não são alterados automaticamente.

## Pendências fora desta entrega

Pagamentos/quitação, edição/exclusão de lançamentos, gestão web completa de cartões,
categorias e recorrências, readiness e operação avançada da outbox continuam
pendentes. Data financeira UTC permanece regra atual explícita; fuso por usuário
não foi implementado nesta tarefa.
