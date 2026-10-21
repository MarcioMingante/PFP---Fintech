# 003 — Docker Compose como ambiente executável local

## Contexto

Projeto precisa de ambiente reproduzível para uso pessoal e demonstração sem
infraestrutura gerenciada obrigatória. Compose existente reúne aplicação e
serviços auxiliares; este registro formaliza essa base retrospectivamente.

## Decisão

Manter Compose como definição executável local de frontend, API, migrador,
PostgreSQL, Redis, RabbitMQ e observabilidade. Healthchecks e dependências
ordenam startup; migrador deve terminar com sucesso antes da API. Configuração
e segredos chegam em runtime pelo ambiente, com portas no loopback.

Volumes preservam PostgreSQL, RabbitMQ e Prometheus. Desenvolvimento pode rodar
API/frontend no host e dependências no Docker. Detalhes operacionais ficam nos
ADRs 022/023/027; laboratório Terraform do ADR 026 não gerencia objetos Compose.

## Alternativas

- Instalar todos os serviços no host: menos containers, mas mais diferenças entre máquinas.
- Kubernetes: adicionaria orquestração sem requisito atual de cluster.
- Serviços cloud gerenciados: exigiriam configuração externa e possível cobrança;
  arquitetura AWS permanece estudo documental (ADR 024).

## Consequências

Docker e imagens compatíveis com arquitetura do host são pré-requisitos. Ambiente
é de uma máquina e não oferece alta disponibilidade. Volumes não são backups;
remover volumes perde dados. Mudança de projeto Compose seleciona outros volumes.
`/health` confirma HTTP, não prontidão completa de todas as dependências.

## Evidências

- [Compose](../../docker-compose.yml).
- [Comandos Make](../../Makefile).
- [Entrega funcional — ADR 023](023-local-functional-delivery.md).
