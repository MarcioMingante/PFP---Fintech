# 019 — TransactionCreated com RabbitMQ e outbox

## Contexto

A fase 23 introduz dois efeitos assíncronos para transações manuais e geradas
por recorrência: analytics e notificações locais. Banco e broker não compartilham
transação distribuída; publicar diretamente após SaveChanges perderia eventos
quando RabbitMQ estivesse indisponível.

## Decisão

Cada nova Transaction grava TransactionCreated v1 em `messaging_outbox`, na
mesma transação PostgreSQL da transação e revisão do dashboard. Funciona nos
caminhos SaveChanges síncrono e assíncrono, inclusive no worker sem usuário HTTP.
Evento contém identificadores, proprietário, valor, tipo e data; não inclui
credenciais, descrição ou dados da conta. Identificador do evento é o da transação.
Migration cria tabelas operacionais geridas por SQL, sem entidades EF adicionais.
Não há backfill de eventos para transações anteriores à migration.

HostedService opcional lê até 50 registros pendentes com FOR UPDATE SKIP LOCKED,
publica evento persistente no exchange fanout durável
`fintrack.transaction-created.v1` com mandatory e publisher confirms, depois
marca publicação e faz commit. Instâncias concorrentes não disputam mesmo lote.
Falha/restart antes de commit preserva pendências; crash entre confirmação e
commit pode publicar duplicata. Entrega é pelo menos uma vez.

Duas filas quorum duráveis (`fintrack.analytics.v1` e
`fintrack.notifications.v1`) recebem evento independentemente. Consumidores
locais usam BasicGet sem auto-ack, até 50 mensagens por fila/ciclo, e confirmam
somente após escrita PostgreSQL. Analytics cria ledger de projeção por evento,
com owner/valor/tipo/data; Notification cria registro local por evento/owner.
Primary key event_id e INSERT ON CONFLICT tornam efeitos idempotentes e atômicos.
Tabelas não são expostas por endpoints; leituras futuras devem filtrar user_id.
Dashboard existente continua usando dados transacionais/cache.

Erro de processamento recebe até três tentativas por entrega, com atrasos de
250 e 500 ms. Payload inválido segue diretamente para `<fila>.dead`. Nack sem
requeue encaminha para DLQ quorum via dead-lettering at-least-once e
reject-publish. Crash/conexão interrompida antes de ack devolve mensagem à fila;
contagem de retries local reinicia na nova entrega. Falha de conexão/ciclo
aguarda dois segundos e recria conexão, inclusive na primeira conexão.
Desligamento cancela operações e libera locks/canais. Logs omitem payload,
usuário, URI e mensagem de exceção potencialmente sensível.

## Alternativas

Publicar diretamente depois do commit deixaria janela de perda entre banco e broker.
Executar efeitos sincronamente acoplaria latência e disponibilidade ao request.
Transação distribuída ampliaria operação; outbox e consumidores idempotentes
preservam gravação local atômica e aceitam entrega duplicada. SQS permanece
apenas estudo, detalhado no ADR 025.

## Consequências

Broker indisponível não impede criação financeira. Projeções têm consistência
eventual. Notificação é registro local; esta fase não envia email/push nem
contata terceiros. Não há worker dedicado em outro processo ainda: produtor e
dois consumidores têm responsabilidades distintas dentro da API.

DLQs preservam eventos para investigação/replay manual após corrigir causa;
não há replay automático. Retenção/limpeza da outbox publicada, métricas,
alertas de backlog e interface de notificações permanecem trabalho futuro.
RabbitMQ local tem volume persistente e portas ligadas apenas ao loopback.
Uma instalação de um nó não fornece alta disponibilidade apesar das filas quorum.

## Referências

- [RabbitMQ .NET client](https://www.rabbitmq.com/client-libraries/dotnet-api-guide)
- [Publisher confirms e consumer acknowledgements](https://www.rabbitmq.com/docs/confirms)
- [Quorum queues e dead-lettering](https://www.rabbitmq.com/docs/quorum-queues)
