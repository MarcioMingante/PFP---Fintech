# 013 — Recorrências mensais e geração idempotente

## Contexto

Fase 17 introduz despesas e receitas recorrentes. Backend não possuía módulo
Transactions da fase 6. Esta entrega cria base mínima de lançamentos gerados,
sem antecipar transferências, CRUD manual ou jobs da fase 22.

## Decisão

`RecurringTransaction` armazena usuário, conta, categoria opcional, descrição,
valor decimal positivo, tipo (`Income`/`Expense`), início, término opcional,
próxima ocorrência e estado ativo. Frequência inicial é sempre `Monthly`.
Conta e categoria são consultadas com filtro do usuário antes da criação.

Template é imutável após criado. Desativação é idempotente e impede novas
ocorrências, inclusive atrasadas ainda não geradas. Não apaga lançamentos
existentes nem oferece reativação. Para alterar contrato, desative template
antigo e crie outro com data inicial explícita.

### Calendário

Primeira ocorrência é data inicial. Cada próxima ocorrência usa dia original
do início, limitado ao último dia do mês. Início em 31/01 gera 28/29 de fevereiro,
31/03 e 30/04; fevereiro não desloca permanentemente calendário.

Término é inclusivo. Após última ocorrência, próxima data é nula e estado
inativo. Datas aceitam 0001-01-02 até 9998-12-01; calendário interrompe no limite
superior. Valores usam `decimal`/`numeric(18,2)` e não arredondam entrada.

### Processamento inicial

`POST /recurring-transactions/generate` processa somente usuário autenticado,
até `throughDate` inclusiva. Padrão é data UTC do `TimeProvider` injetado.
Não permite gerar lançamentos futuros. Template com início futuro é permitido
e aguarda data inicial. Geração recupera todos os meses pendentes até limite
por lote de **120 ocorrências por recorrência**. `hasMore: true` pede nova chamada.

Transação de banco envolve lançamentos e atualização dos checkpoints. Bloqueio
PostgreSQL `pg_advisory_xact_lock` por usuário serializa geração e desativação,
inclusive entre processos. Índice único `(recurring_transaction_id, occurred_on)`
protege cada ocorrência contra duplicação. Checkpoint não avança sem lançamentos
persistidos; falha reverte lote. Usuários diferentes processam independentemente.

Agendamento automático, worker e mensageria permanecem nas fases 22/23.
Endpoint permite primeiro workflow manual periódico, sem efeitos em GET.

### Lançamentos

`Transaction` persistida referencia recorrência, usuário, conta e categoria,
com descrição, valor positivo, tipo e data da ocorrência. `GET /transactions`
lista lançamentos gerados do usuário. Tipo expressa receita/despesa; não há
transferência nem campo de saldo incremental nesta entrega. Dashboard poderá
agregar receitas menos despesas a partir desses registros. Integração web,
CRUD manual e implementação completa da fase 6 permanecem pendentes.

Filtros globais protegem templates e lançamentos. FKs preservam referências;
conta com recorrência ou lançamento não pode ser excluída, retornando 409.
Recorrências finalizadas/desativadas também preservam vínculo histórico.

## Alternativas

- Gerar em GET: rejeitado por leitura com efeitos colaterais.
- Avançar data anterior com `AddMonths`: rejeitado por perder dia original.
- Confiar somente em verificação de existência: rejeitado por corrida concorrente.
- Worker nesta fase: adiado para fase 22, conforme roadmap.
- Todos os meses em lote ilimitado: rejeitado para limitar recuperação por chamada.

## Consequências

Backend oferece cadastro, leitura, desativação e processamento manual mensal.
Testes cobrem precisão, calendário, término, lotes, checkpoints, concorrência,
reenvios, autorização e persistência de receitas/despesas.
