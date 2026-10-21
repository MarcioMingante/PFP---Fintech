# 028 — Transferências atômicas e saldos derivados

Status: aceita. Escopo: implementação local. Registro: 21/10/2026.

## Contexto

Auditoria identificou M3 incompleto: receitas/despesas existentes não ofereciam
transferência entre contas ou consulta de saldo individual. Modelar duas escritas
independentes permitiria débito sem crédito, e tratá-las como renda/despesa
inflaria dashboard, categorias e eventos de analytics.

## Decisão

Adicionar aggregate Transfer com origem, destino, proprietário, descrição, valor,
data e chave de idempotência. Uma linha representa ambas as incidências no livro;
insert e revisão de cache compartilham transação pelo DbContext existente.

POST/GET `/transfers` são autenticados, separados de `/transactions`.
Chave única por usuário e conteúdo imutável: replay retorna 200, conteúdo divergente
409. Repositório trata conflito concorrente sem duplicar movimento. As duas contas
precisam pertencer ao usuário; FK composta `(account_id, user_id)` acrescentada
por SQL na migration reforça propriedade sem tornar obrigatório proprietário dos
registros legados. Essas constraints adicionais não são modeladas como alternate
key EF, pois isso mudaria nulabilidade de contas antigas.

GET `/accounts/balances` agrega receitas - despesas + transferências recebidas -
enviadas numa consulta SQL, até data atual UTC. Saldo não é contador mutável;
lançamentos recorrentes anteriores entram automaticamente, sem backfill financeiro.
Conta vazia tem zero; saldo negativo continua permitido, assim como despesas
já permitiam. API não promete autorização por fundos disponíveis.

Transferência não emite TransactionCreated v1: contrato desse evento continua
restrito a receitas/despesas. Não altera totais globais, histórico de renda,
previsões ou categorias. Exclusão de qualquer conta referenciada é bloqueada.

## Alternativas

- Duas transações Income/Expense: distorceriam agregações e semântica dos eventos.
- Dois registros independentes: exigiriam invariantes adicionais para impedir metade persistida.
- Saldo armazenado/incremental: exigiria backfill, sincronização de todos os caminhos
  de escrita e controle de atualização perdida; agregação evita contador duplicado.
- Proibir saldo negativo: seria nova política de crédito com exigência de locks;
  não é adotada sem requisito de produto.

## Consequências

Migration AddTransfers é obrigatória antes de usar endpoints novos. Registro único
assegura ambos os lados, mas consulta passa a agregar histórico; índices apoiam
filtros por conta/usuário. Crescimento futuro pode justificar read model próprio.
Saldo inicial, transferências externas, estornos e pagamentos ficam fora desta entrega.

## Evidências e validação

[Integrações](../../apps/api/tests/FinTrack.IntegrationTests/TransferEndpointTests.cs)
cobrem conservação de saldos, isolamento, FK de propriedade, concorrência,
idempotência, exclusão em uso e rollback da transferência/revisão por falha injetada.
[Domínio](../../apps/api/tests/FinTrack.UnitTests/Domain/Transfers/TransferTests.cs)
e [E2E](../../apps/web/e2e/finance.spec.ts) cobrem invariantes e fluxo web.
