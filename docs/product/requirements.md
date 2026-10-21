# Requisitos e critérios de aceitação

Documentação consolidada em 21/10/2026 a partir do produto existente e correções
M0/M3/M4. “Implementado” descreve código disponível; evidência de execução está no
[registro de validação desta entrega](../milestone-completion-2026-10-21.md).

| ID | Requisito | Critério de aceitação / evidência de código |
| --- | --- | --- |
| RF01 | Autenticar e isolar usuários | Registro/login/refresh/logout; outro usuário não consulta/movimenta recursos; AuthenticationTests e TransferEndpointTests |
| RF02 | Administrar contas | Criar/listar/consultar/renomear/excluir na API; criar/listar/buscar/editar/excluir na web; exclusão em uso retorna 409; ResourceEndpointTests e E2E de contas |
| RF03 | Categorizar lançamentos | API cria/lista/consulta/atualiza categorias; categoria opcional em lançamento; rejeitar referência alheia; ResourceEndpointTests/ManualTransactionEndpointTests |
| RF04 | Registrar receita/despesa | Valor positivo e até duas casas, conta própria e data não futura; persistência alimenta saldo/dashboard; ManualTransactionEndpointTests |
| RF05 | Transferir entre contas próprias | Origem diferente do destino; um registro atômico; idempotência por usuário/chave; falha não deixa nenhum efeito; TransferTests/TransferEndpointTests |
| RF06 | Consultar saldo por conta | `GET /accounts/balances` calcula receita menos despesa mais entradas menos saídas de transferência até hoje UTC; não altera histórico; soma das contas conserva total registrado |
| RF07 | Usar cartão e parcelamento | CRUD de cartão API, compras idempotentes, soma das parcelas igual à compra; PurchaseEndpointTests/CreditCardEndpointTests |
| RF08 | Consultar faturas | Listagem/detalhe/resumo por calendário e limite; sem presumir pagamento; InvoiceEndpointTests |
| RF09 | Gerar recorrências | Mensal, dia preservado após mês curto, geração única por ocorrência e usuário, desativação; RecurringTransactionEndpointTests/BackgroundJobTests |
| RF10 | Consultar dashboard | Histórico, categorias, faturas e previsão; transferência interna não aparece como receita/despesa; DashboardEndpointTests/TransferEndpointTests |
| RNF01 | Precisão monetária | Decimal no backend/banco, JSON monetário em strings e validação/formatação exata na web; MoneyPrecisionEndpointTests e money.test.mjs |
| RNF02 | Persistência e consistência | Migrations preservam dados anteriores; transferência e revisão de cache na mesma transação; falha injetada reverte ambas |
| RNF03 | Operação local reproduzível | Setup, Compose, encerramento sem perda de volumes, backup/restore e diagnóstico documentados no README/release |
| RNF04 | Verificação proporcional | Testar domínio, API/banco e interface afetados com dados descartáveis; não depender de banco pessoal |
| RNF05 | Integrações opcionais | PostgreSQL é fonte de verdade; falha de Redis não bloqueia leitura; outbox preserva eventos financeiros com broker offline |

## Evoluções ainda pendentes

Pagamento/quitação e estorno de cartão; edição/exclusão de lançamentos; interface
completa de categorias/cartões/recorrências; readiness; retenção/replay de eventos;
fuso configurável por usuário. Não estão declarados implementados por esta entrega.

## Aceite dos milestones corrigidos

- M0: visão, requisitos, regras e casos de uso versionáveis, com limites e evidências.
- M3: receita/despesa existentes, transferências atômicas/idempotentes e saldo por
  conta, com isolamento e regressões de concorrência/rollback.
- M4: fluxos enumerados na fase 10 disponíveis no navegador, incluindo edição e
  exclusão de contas, erros e persistência após recarga. Não exigir aqui funcionalidades
  web que nunca foram enumeradas como critérios da fase 10.
