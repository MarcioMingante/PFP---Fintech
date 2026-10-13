# 018 — Recorrências em BackgroundService

## Contexto

Fase 17 exigia geração manual periódica. Fase 22 automatiza processamento
sem duplicar lançamentos ou enfraquecer isolamento. Faturas já nascem com
compras/parcelas; dashboard usa leitura/cache com revisão transacional.

## Decisão

`RecurringTransactionsWorker` executa dentro da API como BackgroundService.
Um ciclo roda ao iniciar, seguido de intervalo configurável após conclusão.
Ciclos de uma instância não se sobrepõem. Registro ocorre somente quando
habilitado e nunca no modo `--migrate`.

Processor descobre proprietários com recorrências ativas vencidas, usando
consulta administrativa restrita de leitura. Prioriza próxima ocorrência mais
antiga e limita proprietários por ciclo. Para cada proprietário, abre escopo
novo e chama mesma geração usada pelo endpoint manual, com predicado explícito
de proprietário. Não usa JWT falso, HttpContext ou usuário global compartilhado.

Cada tentativa tem DbContext próprio. Lock advisory por usuário, índice único
por recorrência/data, transação e checkpoint continuam compartilhados com
geração manual/desativação. Instâncias de API podem selecionar mesmo usuário;
lock serializa escrita e checkpoint impede duplicação. Início futuro continua
pendente; geração usa data UTC do TimeProvider, sem lançamentos futuros.

### Limites e recuperação

Até 100 proprietários por ciclo por padrão; geração preserva limite existente
de 120 ocorrências por recorrência/lote. Backlog remanescente aparece no próximo
ciclo. Checkpoint em PostgreSQL permite retomar após reinício, sem estado
financeiro dependente da memória do worker.

Timeout e NpgsqlException transitória recebem até três tentativas por padrão,
com atraso exponencial de 500 ms e 1000 ms. Erro permanente não é repetido no
mesmo ciclo. Proprietário com falha não impede demais proprietários selecionados;
pendência será reavaliada nos próximos ciclos. Erro na descoberta/ciclo é
registrado e não encerra API. Não há fila ou dead-letter nesta fase.

Token de encerramento acompanha consultas, lock, gravação e delays. Cancelar
não avança checkpoint sem commit. StopAsync encerra processamento cooperativo.
Logs estruturados registram contagem de usuários, lançamentos, falhas e backlog,
sem payloads financeiros, tokens ou connection strings.

### Integração com outros módulos

SaveChanges já incrementa revisão do cache pela propriedade das entidades,
inclusive sem usuário HTTP. Geração automática invalida todas as datas do
usuário na mesma transação. Redis não é requisito para geração.

Não se cria job redundante de faturas vazias: compras já geram todas as faturas
necessárias transacionalmente, e ciclo vazio é projeção. Não se persiste resumo
duplicado: próximas leituras recalculam dashboard após invalidar cache.

## Configuração

BackgroundJobs:

- Enabled: falso na API direta; Compose/Makefile habilitam.
- IntervalSeconds: 60, permitido 1–86400.
- MaxAttempts: 3, permitido 1–5.
- RetryDelayMilliseconds: 500, permitido 50–10000.
- MaxUsersPerCycle: 100, permitido 1–1000.

Testes e runner E2E desabilitam worker implicitamente para evitar mudanças
financeiras não controladas. Teste específico habilita serviço real com relógio
fixo e intervalo de um segundo.

## Alternativas

- Worker separado: adiado; HostedService dentro da API simplifica etapa inicial.
- Timer com callback concorrente: rejeitado em favor de loop async sequencial.
- Reutilizar DbContext em retry: rejeitado por estado após falha de transação.
- Bypass irrestrito de filtros na escrita: rejeitado; geração exige um usuário.
- Scheduler/queue externo: adiado para mensageria e evolução operacional.

## Consequências

Nenhuma migration nova. Restart e escalonamento usam checkpoints/locks existentes.
Integração valida serviço hospedado, cache, retries com escopo novo, falha isolada,
concorrência com endpoint manual e continuação de backlog. Fase não implementa
agendamento distribuído com fila nem processamento de pagamentos.
