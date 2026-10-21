# 024 — Arquitetura AWS conceitual

## Status e contexto

Aceita para documentação em 13/10/2026; implantação não autorizada nem realizada.
Projeto tem stack completa Docker Compose e restrição de não contratar serviços
pagos. Fase 28 pede demonstrar escolhas de cloud sem executar infraestrutura.

## Decisão

Documentar alternativa ECS Fargate, RDS PostgreSQL, ElastiCache, SQS, S3,
CloudFront, ALB e CloudWatch. SNS complementa SQS para preservar fanout dos dois
consumidores; X-Ray complementa observabilidade de traces. Desenho, premissas,
comparações e referências ficam no [estudo](../architecture/aws-conceptual.md)
e [diagrama](../diagrams/aws-conceptual.md), ambos identificados como proposta.

Runtime oficial permanece local. Não criar contas, credenciais, recursos, filas,
registry, Terraform AWS ou jobs de deploy. Nenhuma troca de transporte ou SDK
AWS entra no código. GHCR continua excluído por decisão do proprietário.

## Alternativas consideradas

- Implantar AWS agora: demonstraria operação gerenciada, mas introduziria
  cobrança potencial e trabalho de segurança/recuperação fora do objetivo.
- Presumir cobertura por Free Tier/créditos: depende de condições, prazos e
  franquias; não garante operação contínua sem cobrança.
- Executar Compose local e estudar cloud documentalmente: escolhido; mantém
  demonstração reproduzível e explicita adaptações reais de uma migração.

## Consequências

Cloud acrescentaria opções de redundância, escala e gestão, com custos por
capacidade/consumo e responsabilidades de configuração. Estudo não comprova
deployment, SLA, failover, performance ou segurança de uma instalação AWS.
Os benefícios descritos dependem de implementação e validação futuras.

Next.js SSR exige runtime; S3 é opcional para arquivos, não substitui aplicação.
SQS exige novo transporte; workers precisam de composição dedicada para escalar
separadamente. Readiness, egress, secrets e entrega das imagens permanecem abertos.
Esses limites evitam apresentar diagrama como infraestrutura pronta.

## Validação documental

Verificar todos os serviços solicitados, vínculos entre ADR/diagrama/estudo,
coerência com stack atual e referências oficiais para semântica/cobrança.
Sem testes de aplicação ou comandos AWS: entrega altera somente documentação.
