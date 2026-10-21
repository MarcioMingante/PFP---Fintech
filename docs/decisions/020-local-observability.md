# 020 — Observabilidade local

## Contexto

Requests, jobs e mensagens atravessam componentes diferentes; logs isolados não
explicam causalidade. Fase 24 pede diagnóstico local sem exportar dados financeiros
ou depender de plataforma gerenciada.

## Decisão

API usa ILogger com console JSON, UTC, campos estruturados e scopes TraceId/SpanId.
Middleware registra método, template de rota, status e duração, incluindo 401,
404 e falhas. X-Correlation-ID devolve trace ID W3C gerado ou recebido via
traceparent; não ecoa valores arbitrários de um cabeçalho de correlação. Não
registra corpo, query string, token, email, owner nem valores financeiros.
Logging EF fica em Warning, com sensitive-data logging desativado.

OpenTelemetry 1.18 registra ASP.NET Core, ActivitySource FinTrack e fonte Npgsql.
Uma allowlist remove tags de SQL, URLs/query strings, headers e dados de negócio
antes dos exporters; spans de banco recebem nome genérico e descrições de erros
são removidas. Não gravamos eventos de exceção com stack/payload. Métricas incluem
HTTP, runtime .NET e counters locais de publicação, consumo, falhas, dead letters,
transações recorrentes geradas e ciclos por outcome. Nenhuma dimensão por usuário,
evento ou ID de requisição. Métricas Npgsql de pool não são exportadas para evitar
que nomes de conexão se tornem labels.

Migration acrescenta trace_parent/trace_state nullable na outbox. Contexto é
salvo junto ao evento, inclusive em SaveChanges síncrono, e não muda seu contrato
financeiro. Publicação inicia span Producer descendente do contexto persistido,
injeta headers W3C em RabbitMQ, e consumidores criam spans Consumer descendentes.
SQL dos consumidores fica no contexto recebido. Eventos antigos sem contexto
continuam válidos, com novo trace. Retries/delivery duplicada mantêm mesma origem.
Jobs de recorrência criam trace por ciclo e marcam falhas; geração/outbox herdam
contexto do ciclo. Publicação e consumo são spans FinTrack explícitos; não
habilitamos instrumentação nativa RabbitMQ adicional que duplicaria spans.

Collector local recebe OTLP gRPC e encaminha traces ao Jaeger e métricas ao
exporter Prometheus. Collector usa memory_limiter e batch; Prometheus coleta a
cada cinco segundos e retém sete dias em volume. Jaeger usa memória; seus traces
são perdidos no restart. UIs e OTLP são expostos somente no loopback. Logs ficam
no console/Docker; não introduzimos Loki ou exportação de logs nesta fase.

Observability:Enabled permite desligar SDK. Endpoint vazio habilita instrumentação
sem envio externo. Compose usa Collector; API local pode definir endpoint
explicitamente. Exportação assíncrona com timeout não participa da transação de
negócio. Collector offline pode perder telemetria; outbox financeira continua
com garantias próprias. --migrate não inicia SDK/exporters. Não há tracing do
browser/Next.js, alertas ou infraestrutura AWS nesta fase.

## Alternativas

Somente logs reduziria componentes, mas limitaria correlação entre produtor e
consumidores. Plataforma gerenciada exigiria serviço externo. SDKs específicos
por destino aumentariam acoplamento; OpenTelemetry com Collector mantém
instrumentação separada dos destinos locais.

## Consequências

Collector, Jaeger e Prometheus ampliam consumo de recursos e operação local.
Telemetria pode ser perdida sem afetar commit financeiro; traces em memória
não são auditoria durável. Redação e baixa cardinalidade limitam exposição de
dados, mas também reduzem detalhes disponíveis ao diagnóstico.

## Referências

- [OpenTelemetry ASP.NET Core](https://opentelemetry.io/docs/languages/dotnet/metrics/getting-started-aspnetcore/)
- [Npgsql tracing](https://www.npgsql.org/doc/diagnostics/tracing.html)
- [Collector configuration](https://opentelemetry.io/docs/collector/configuration/)
- [Jaeger local](https://www.jaegertracing.io/docs/2.22/getting-started/)
