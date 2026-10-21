# 025 — Mensageria local e estudo de SQS

## Contexto

RabbitMQ já implementa TransactionCreated com outbox e dois consumidores locais.
Fase 29 pede demonstração das garantias existentes e comparação com SQS,
preservando restrição de execução pessoal sem cloud obrigatório.

## Decisão

Manter implementação do [ADR 019](019-transaction-messaging.md). Consolidar
[demonstração e comparação](../architecture/rabbitmq-and-sqs.md), diagnóstico
somente leitura e alvo `make test-messaging` com unitários/integrações pertinentes.
Reusar cenários de falha, fanout, duplicação, rollback, retries e DLQs existentes.

SQS/SNS permanecem conceituais, conforme [ADR 024](024-cloud-conceptual-architecture.md).
Não criar recursos/credenciais AWS, instalar SDK ou substituir transporte.
SQS não é troca de URI AMQP: queue, visibility timeout, polling, delete e
redrive exigem mudanças explícitas de aplicação e configuração.

## Alternativas

Trocar RabbitMQ por SQS agora exigiria novo transporte e recursos externos.
Adicionar emulador local criaria outra integração sem validar serviço AWS real.
Manter implementação e comparar semântica em documentação atende estudo
sem alterar garantias ou custos do runtime.

## Consequências

Demonstração executa em containers descartáveis, sem alterar dados pessoais.
Publisher confirms e consumer ack têm papéis distintos; duplicatas são esperadas
e deduplicadas por consumidor. Três tentativas atuais são locais por entrega,
sem limite persistente global de redelivery. Projeções têm consistência eventual.

Não adicionar replay automático, purge, retenção ou mudanças financeiras nesta
fase. Outbox publicada continua crescendo; broker único continua dependendo do
host. Notificações são registros PostgreSQL, sem contato externo.

## Validação

Executar somente `make test-messaging`, conferir diagnóstico SQL e links locais.
Teste de falha simula canal fechado; teste de duplicata força republicação.
Não declarar cobertura de todos os cenários de crash, particionamento ou HA.
