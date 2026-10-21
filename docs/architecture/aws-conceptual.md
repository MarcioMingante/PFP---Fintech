# Arquitetura cloud como estudo de portfólio

**Status: proposta documental, não implantada.** Revisão: 13/10/2026.
Execução real permanece local, com Docker Compose, PostgreSQL, Redis, RabbitMQ,
Collector, Jaeger e Prometheus. Não há conta, recursos, credenciais AWS ou
publicação de imagens nesta entrega. [Diagrama](../diagrams/aws-conceptual.md).

## Mapeamento e limites

| Implementação local | Alternativa conceitual AWS | Adaptação necessária |
| --- | --- | --- |
| Containers Next.js/API | ECS com Fargate | Services, tasks, rede e healthchecks; preservar SSR e sessão no servidor |
| PostgreSQL e volume | RDS PostgreSQL | Conexão TLS, backups/restauração e migrations controladas |
| Redis de dashboard | ElastiCache compatível com Redis | Validar engine/versão, TLS, autenticação e política de eviction |
| RabbitMQ, fanout, duas filas e dead letters | SNS + SQS com DLQ por consumidor | Novo transporte, polling, visibility timeout, confirmação e retry |
| Arquivos públicos no container; sem upload de usuário implementado | S3 privado, opcional | Só adicionar se existir necessidade real; upload/autorização ainda não implementados |
| Browser acessa Next.js local | CloudFront + ALB + serviço Next.js | HTTPS, políticas de cache e encaminhamento de cookies/métodos/headers |
| Logs JSON + OTLP + Jaeger/Prometheus | CloudWatch + Collector; X-Ray para traces | Exporters, permissões, retenção, sampling e redaction |
| Container `migrate` antes da API | Task Fargate única de migrations | Orquestração de entrega e bloqueio do rollout em falha |

Fargate executa containers; ALB distribui HTTP para serviços ECS. Essa combinação
é suportada pela [documentação ECS](https://docs.aws.amazon.com/AmazonECS/latest/developerguide/service-load-balancing.html).
Neste desenho, somente frontend recebe tráfego externo; API permanece interna,
acessada pelo servidor Next.js via descoberta de serviço, ainda a definir.

Next.js atual usa Server Actions, cookies e SSR; não pode ser substituído por
site estático no S3 sem redesenhar funcionalidades. CloudFront teria cache para
estáticos públicos versionados; páginas financeiras, autenticação e Server
Actions usariam comportamento sem cache compartilhado, TTLs zero e encaminhamento
dos cookies, headers e métodos necessários. Validar login/refresh/logout e
ausência de vazamento entre usuários antes de qualquer implantação. Políticas
são específicas por caminho: [cache behaviors](https://docs.aws.amazon.com/AmazonCloudFront/latest/DeveloperGuide/DownloadDistValuesCacheBehavior.html).

RabbitMQ não é substituído pelo endereço de uma fila SQS. Consumidores de uma
mesma fila competem por mensagens; para cada consumidor atual receber evento,
proposta usa tópico SNS e fila SQS própria, conforme
[fanout SNS/SQS](https://docs.aws.amazon.com/sns/latest/dg/sns-sqs-as-subscriber.html).
Outbox transacional e deduplicação por consumidor permanecem necessárias: SQS
Standard admite entrega repetida ([at-least-once](https://docs.aws.amazon.com/AWSSimpleQueueService/latest/SQSDeveloperGuide/standard-queues-at-least-once-delivery.html)).
Detalhamento de semântica fica para Fase 29; transporte real continua RabbitMQ.

## Disponibilidade e escalabilidade

| Aspecto | Ambiente local real | Proposta AWS |
| --- | --- | --- |
| Falha de máquina | Todos os serviços dependem do computador; restart não protege contra perda do disco | Réplicas web/API em duas AZs; distribuição e substituição de tasks |
| Banco | Volume único; backup/restauração operados pelo proprietário | RDS Multi-AZ e backups; failover exige reconexão e restauração precisa ser testada |
| Cache | Volátil; perder cache exige consultas ao banco | Réplicas/failover quando suportados pela configuração escolhida; banco continua fonte da verdade |
| Mensagens | Broker local persistente, dependente do host | Filas gerenciadas; backlog, retries e DLQs monitorados; duplicatas tratadas pela aplicação |
| Capacidade | Limitada ao hardware disponível | Escalar tasks HTTP por carga e consumidores por backlog; limitar conexões e concorrência no banco |
| Operação | Compose e diagnóstico local | Alarmes, quotas, rollback, readiness e procedimentos de incidentes ainda necessários |

[RDS Multi-AZ com standby](https://docs.aws.amazon.com/AmazonRDS/latest/UserGuide/Concepts.MultiAZSingleStandby.html)
melhora disponibilidade; standby dessa modalidade não serve leitura. Replicação
não substitui backup. Multi-AZ não cobre indisponibilidade regional; recuperação
entre regiões seria decisão adicional, com custos próprios. Não declaramos SLA,
RPO/RTO numéricos sem dimensionamento e ensaio de recuperação.

Workers hoje executam dentro da API. Separar recorrências/outbox/consumidores em
services Fargate exige composição dedicada e testes de concorrência. Réplicas
precisariam preservar locks/idempotência existentes e impedir migrations
simultâneas. API também precisa de readiness de dependências: `/health` atual
verifica apenas HTTP. Autoscaling sozinho não resolve essas condições.

## Segurança proposta

Entrada HTTPS, sub-redes privadas para tasks/banco/cache e security groups
restritos aos fluxos do diagrama. Banco/cache sem acesso público. Aplicação
continua responsável por autenticação JWT, isolamento por usuário, validação e
limitação de requisições. HTTPS exige cookies Secure e validação dos proxies.

Aplicação usaria IAM task roles com menor privilégio para filas/objetos; role
de execução seria distinta da role da aplicação. Não incorporar chaves AWS,
senhas ou JWT nas imagens. Secrets Manager ou alternativa equivalente precisaria
ser escolhida para armazenamento/rotação; não foi implementada.
[Referência de task roles](https://docs.aws.amazon.com/AmazonECS/latest/developerguide/task-iam-roles.html).

S3 teria bloqueio de acesso público e autorização por objeto; CloudFront usaria
acesso restrito à origem. Arquivos pessoais futuros não seriam conteúdo público
cacheado. Criptografia em repouso/transporte, retenção e acesso a backups/logs
precisariam ser configurados. Preservar redaction atual: sem tokens, senhas,
SQL, payload financeiro ou dados pessoais nos traces/logs.

Saída de sub-redes privadas para imagens, APIs AWS e destinos externos requer
projeto de endpoints VPC ou NAT. Distribuição das imagens, DNS/certificados,
discovery interno, região e quotas continuam decisões abertas. Nenhum registry
foi criado; GHCR permanece fora do escopo solicitado.

## Custos e escolha do ambiente local

Sem estimativa monetária fixa: região, capacidade, tráfego, retenção e plano
alteram resultado. Tabela registra dimensões de cobrança, com fontes oficiais.
Créditos/franquias temporários não garantem operação contínua sem cobrança.

| Componente proposto | Dimensões a estimar |
| --- | --- |
| [Fargate](https://aws.amazon.com/fargate/pricing/) | vCPU, memória, tempo ativo e armazenamento efêmero adicional |
| [RDS PostgreSQL](https://aws.amazon.com/rds/postgresql/pricing/) | Instâncias, modalidade Multi-AZ, armazenamento, I/O conforme modalidade e backups excedentes |
| [ElastiCache](https://aws.amazon.com/elasticache/pricing/) | Nós/horas ou capacidade e processamento serverless; réplicas conforme configuração |
| [SQS](https://aws.amazon.com/sqs/pricing/) e [SNS](https://aws.amazon.com/sns/pricing/) | Requisições/publicações, payloads, polling, retries e fanout |
| [S3](https://aws.amazon.com/s3/pricing/) | GB armazenados, classe, requisições, recuperação e transferência aplicável |
| [CloudFront](https://aws.amazon.com/cloudfront/pricing/) | Plano escolhido ou consumo, tráfego/requisições e recursos adicionais |
| [ALB](https://aws.amazon.com/elasticloadbalancing/pricing/) | Horas e unidades de capacidade de balanceamento |
| [CloudWatch](https://aws.amazon.com/cloudwatch/pricing/) | Ingestão/retenção/consultas de logs, métricas e alarmes; traces conforme serviço/plano |
| [Rede VPC](https://aws.amazon.com/vpc/pricing/) | NAT, endpoints, IPv4 públicos e transferência aplicável |

Modelo de dimensionamento: custo mensal total = compute + banco + cache +
balanceamento + mensagens + objetos/CDN + observabilidade + rede + serviços
auxiliares. Para hipótese de 30 dias, serviço continuamente ativo representa
720 horas por instância/task; duas réplicas duplicam horas de compute. Essa é
premissa de cálculo, não cotação nem medição do projeto. Estimar também imagens,
DNS, secrets, criptografia e backups, sem somar transferência isenta duas vezes.

Mesmo sem usuários, tasks, banco, cache e ALB provisionados podem manter cobrança.
Multi-AZ e redundância aumentam capacidade provisionada; logs e retries ampliam
consumo. Portanto excluímos serviços gerenciados do uso pessoal: Compose atende
demonstração e aprendizado sem contratar cloud. Energia, equipamento, disco e
internet locais continuam custos do proprietário; não alegamos custo total zero.

## Decisão e critérios futuros

[ADR 024](../decisions/024-cloud-conceptual-architecture.md) mantém AWS como estudo.
Uma implantação futura exigiria objetivo operacional, aprovação de orçamento,
dimensionamento por região, testes de segurança/restauração/failover, readiness,
adaptação de transporte/worker e processo de entrega. Nada disso autoriza
provisionamento nesta fase. README e Compose continuam caminho reproduzível real.
