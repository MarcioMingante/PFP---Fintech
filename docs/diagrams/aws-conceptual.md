# Arquitetura AWS conceitual — Fase 28

**Proposta de estudo, não implantada.** Nenhuma conta, credencial ou recurso AWS
foi criado. Ambiente executável continua sendo Docker Compose local.

```mermaid
flowchart TB
    browser["Navegador"]
    cdn["CloudFront: HTTPS; cache somente de estáticos públicos"]
    objects["S3 privado: arquivos opcionais futuros"]
    browser --> cdn
    cdn -. "Origem opcional com acesso restrito" .-> objects

    subgraph vpc["VPC proposta: distribuição em duas zonas de disponibilidade"]
        subgraph ingress["Camada de entrada"]
            alb["ALB: origem HTTPS do frontend"]
        end
        subgraph app["Sub-redes privadas: tasks sem IP público"]
            web["ECS Fargate: Next.js SSR e sessão"]
            api["ECS Fargate: API ASP.NET Core"]
            workers["ECS Fargate: jobs, outbox e consumidores propostos"]
            migration["Task única de migrations antes da API"]
            collector["Collector OpenTelemetry proposto"]
        end
        subgraph data["Camada de dados privada"]
            db["RDS PostgreSQL: Multi-AZ; dados e outbox"]
            cache["ElastiCache: cache Redis compatível"]
        end
        alb --> web
        web -->|"HTTP interno; descoberta de serviço"| api
        api --> db
        api --> cache
        migration --> db
        workers --> db
        workers --> cache
        web -. "Telemetria futura" .-> collector
        api -. "OTLP" .-> collector
        workers -. "OTLP" .-> collector
    end

    cdn -->|"Rotas dinâmicas sem cache compartilhado"| alb
    sns["SNS: fanout proposto"]
    analytics["SQS: analytics"]
    notifications["SQS: notifications"]
    analyticsDead["SQS: DLQ analytics"]
    notificationsDead["SQS: DLQ notifications"]
    telemetry["CloudWatch: logs e métricas; X-Ray para traces"]
    workers -->|"Publicação da outbox"| sns
    sns --> analytics
    sns --> notifications
    analytics -->|"Polling"| workers
    notifications -->|"Polling"| workers
    analytics -. "Após limite de tentativas" .-> analyticsDead
    notifications -. "Após limite de tentativas" .-> notificationsDead
    collector --> telemetry
    api -. "Logs JSON" .-> telemetry
    workers -. "Logs JSON" .-> telemetry
```

SQS, SNS, S3 e observabilidade são serviços regionais acessados por APIs; posição
fora da VPC não significa exposição pública dos dados. Políticas IAM, TLS e
endpoints privados/egress controlado precisariam ser definidos. Diagrama omite
DNS, certificados, distribuição de imagens, secrets e rotas de saída; são
dependências de uma implantação futura, descritas no estudo.

Tasks web/API teriam réplicas em duas AZs. Worker representa papéis lógicos;
separá-los da API requer mudança de composição do código atual. Migrations
seriam uma execução única controlada pelo processo de entrega. RDS Multi-AZ
representa primário e standby, sem prometer consultas no standby.

Detalhes: [estudo e comparação](../architecture/aws-conceptual.md) e
[ADR 024](../decisions/024-cloud-conceptual-architecture.md).
