# Personal Finance Platform — Roadmap Revisado

## Objetivo do projeto

Construir uma plataforma financeira completa que sirva simultaneamente como:

- projeto principal de portfólio;
- prática real de backend com C#/.NET;
- prática de frontend com Next.js/TypeScript;
- estudo de PostgreSQL;
- estudo de Docker;
- prática de testes;
- introdução prática a System Design;
- base para AWS, CI/CD e observabilidade.

A stack principal continua:

```text
Frontend
Next.js + TypeScript + Tailwind + Shadcn + ECharts

Backend
ASP.NET Core + .NET

Database
PostgreSQL

Infra local
Docker + Docker Compose

Testing
xUnit + Testcontainers + Playwright

Cloud
AWS

Complementos futuros
Redis
RabbitMQ/SQS
OpenTelemetry
Terraform
GitHub Actions
```

---

# Fase 0 — Produto e planejamento

Antes de escrever código, defina o que o sistema realmente precisa resolver.

## Criar documentação inicial

Estrutura:

```text
docs/

├── product/
│   ├── vision.md
│   ├── requirements.md
│   ├── business-rules.md
│   └── use-cases.md
│
├── architecture/
├── database/
├── decisions/
└── diagrams/
```

## Definir domínio inicial

Entidades principais:

```text
User

Account

Transaction

Category

CreditCard

Invoice

Installment

RecurringTransaction
```

## Regras iniciais

Exemplos:

```text
Uma transação deve pertencer a um usuário.

Uma transação não pode ter valor zero.

Uma conta pode possuir várias transações.

Uma compra parcelada deve gerar N parcelas.

Cada parcela pertence a uma fatura específica.

Uma transferência deve possuir conta de origem
e conta de destino diferentes.
```

## O que estudar

```text
Requirements
Use Cases
Business Rules
Domain Modeling
Entity
Value Object
Aggregate
```

O objetivo é entender:

> "Qual problema estou modelando antes de pensar em framework?"

---

# Fase 1 — Repository + estrutura inicial

Agora criamos o projeto.

Estrutura:

```text
personal-finance-platform/

├── apps/
│   ├── api/
│   └── web/
│
├── infrastructure/
│   └── docker/
│
├── docs/
│
├── scripts/
│
├── .github/
│   └── workflows/
│
├── docker-compose.yml
├── .env.example
├── .editorconfig
├── .gitignore
└── README.md
```

## Primeiros objetivos

```text
[ ] Criar repository
[ ] Criar README
[ ] Criar estrutura de pastas
[ ] Criar documentação inicial
[ ] Criar .gitignore
[ ] Criar .editorconfig
[ ] Criar .env.example
```

## O que estudar

```text
Monorepo
Repository structure
Environment variables
Secrets
Configuration
Git workflow
```

---

# Fase 2 — Docker Foundation

Agora Docker entra oficialmente no início do projeto.

Mas começamos simples.

Inicialmente:

```text
Docker Compose

└── PostgreSQL
```

Seu `docker-compose.yml` começa apenas com banco.

Exemplo conceitual:

```text
services:

  postgres:
    image: postgres
    ports:
      - 5432:5432

    volumes:
      - postgres_data

    environment:
      database
      username
      password
```

## Objetivo

Rodar:

```bash
docker compose up
```

e ter PostgreSQL funcionando sem instalar PostgreSQL diretamente na máquina.

## O que estudar

Aqui você precisa realmente entender:

```text
Docker Image

Docker Container

Dockerfile

Docker Compose

Port Mapping

Volume

Environment Variable

Docker Network
```

Principalmente a diferença:

```text
Image
```

é o template.

```text
Container
```

é a instância executando.

## Milestone

```text
docker compose up postgres
```

Banco funcionando e persistindo dados mesmo depois de reiniciar o container.

---

# Fase 3 — Backend Skeleton

Agora criamos o backend.

Estrutura:

```text
apps/api/

├── src/
│   ├── FinTrack.Api/
│   ├── FinTrack.Application/
│   ├── FinTrack.Domain/
│   └── FinTrack.Infrastructure/
│
├── tests/
│   ├── FinTrack.UnitTests/
│   ├── FinTrack.IntegrationTests/
│   └── FinTrack.ArchitectureTests/
│
└── FinTrack.sln
```

Dependências:

```text
Api
 │
 ▼
Application
 │
 ▼
Domain

Infrastructure
 │
 ├── Application
 └── Domain
```

## Criar endpoint inicial

```http
GET /health
```

Resposta:

```json
{
  "status": "healthy"
}
```

## O que estudar

```text
ASP.NET Core

Dependency Injection

Middleware

Controllers / Minimal APIs

HTTP

REST

Status Codes

JSON

Configuration

Environment
```

O principal objetivo conceitual é entender:

```text
Request
  ↓
ASP.NET Pipeline
  ↓
Endpoint
  ↓
Response
```

---

# Fase 4 — API + PostgreSQL

Agora conectamos:

```text
ASP.NET
   ↓
EF Core
   ↓
PostgreSQL Docker
```

A API ainda roda localmente:

```text
dotnet watch
```

PostgreSQL:

```text
Docker
```

Arquitetura:

```text
Host Machine

ASP.NET API
     │
     │ localhost:5432
     ▼
Docker PostgreSQL
```

## Implementar

Primeiramente:

```text
DbContext

Migrations

Account

Category
```

## Estudar

```text
ORM

EF Core

DbContext

DbSet

Migration

Primary Key

Foreign Key

Constraint

Index

Relationships

SQL
```

Aqui é importante aprender a olhar o SQL produzido pelo EF Core.

---

# Fase 5 — Primeiro módulo real: Accounts

Agora começa o produto.

Use cases:

```text
CreateAccount

ListAccounts

GetAccount

UpdateAccount

DeleteAccount
```

Endpoints:

```http
POST /accounts

GET /accounts

GET /accounts/{id}

PUT /accounts/{id}

DELETE /accounts/{id}
```

Estrutura:

```text
Application/

Accounts/

├── Commands/
├── Queries/
├── DTOs/
├── Validators/
└── Mappings/
```

## Estudar

```text
DTO

Entity

Command

Query

Validation

Mapping

Repository Pattern

Dependency Inversion
```

Principal conceito:

```text
HTTP != Application Logic
```

e:

```text
DTO != Domain Entity
```

---

# Fase 6 — Categories e Transactions

Depois:

```text
Categories
```

e finalmente:

```text
Transactions
```

Tipos:

```text
Income

Expense

Transfer
```

Inicialmente não faça cartão ainda.

Primeiro faça contas comuns.

## Regras

```text
Income aumenta saldo.

Expense reduz saldo.

Transfer movimenta valor entre duas contas.

Transaction precisa possuir categoria opcional
ou obrigatória conforme decisão de domínio.
```

## Estudar

```text
Database transactions

Atomicity

Consistency

Business validation

Domain service

Application service
```

Transferências são especialmente interessantes porque você começa a lidar com:

```text
Debit Account A

Credit Account B

Tudo ou nada
```

---

# Fase 7 — Testes desde cedo

Os testes não ficam mais apenas no final.

A partir dessa fase, toda nova feature importante deve ter testes.

## Unit tests

Exemplos:

```text
AccountTests

TransactionTests

TransferTests
```

## Integration tests

Inicialmente:

```text
API
 ↓
PostgreSQL
```

Depois usaremos Testcontainers.

## Estudar

```text
Unit Test

Integration Test

Test Double

Mock

Fixture

Arrange / Act / Assert
```

Regra prática:

```text
Domain rules
→ Unit Test

Database/API
→ Integration Test
```

---

# Fase 8 — Dockerizar API

Agora a API já possui:

```text
Accounts

Categories

Transactions

PostgreSQL

EF Core
```

Agora faz sentido dockerizá-la.

Criamos:

```text
apps/api/Dockerfile
```

Arquitetura:

```text
Docker Compose

├── api
└── postgres
```

Agora surge uma mudança importante.

Antes:

```text
Host=localhost
```

Agora:

```text
Host=postgres
```

Porque os containers usam o nome do serviço dentro da Docker Network.

## Estudar

Aqui:

```text
Dockerfile

Base Image

Build Stage

Runtime Stage

Multi-stage Build

Docker Network

Service Discovery

Container Health Check
```

---

# Fase 9 — Frontend Skeleton

Agora criamos:

```text
apps/web/
```

Estrutura:

```text
src/

├── app/
├── components/
├── features/
├── hooks/
├── services/
├── schemas/
├── types/
├── utils/
└── config/
```

Rotas iniciais:

```text
/dashboard

/accounts

/transactions

/categories
```

Inicialmente:

```text
Next.js local
```

com API Dockerizada.

Arquitetura:

```text
Next.js local
      │
      ▼
Docker API
      │
      ▼
Docker PostgreSQL
```

## Estudar

```text
App Router

Server Component

Client Component

React State

Forms

Zod

API Calls

Loading State

Error State
```

---

# Fase 10 — Frontend + backend integration

Agora implementamos:

```text
List accounts

Create account

Edit account

Delete account

List transactions

Create transaction
```

Primeiro faça funcionar.

Depois refine UX.

Fluxo:

```text
Browser
  ↓
Next.js
  ↓
ASP.NET
  ↓
Application
  ↓
Domain
  ↓
EF Core
  ↓
PostgreSQL
```

Você deve conseguir explicar esse fluxo inteiro sem consultar código.

---

# Fase 11 — Dockerizar frontend

Agora o projeto inteiro pode rodar via Docker.

Arquitetura:

```text
Docker Compose

├── web
├── api
└── postgres
```

Rodando:

```bash
docker compose up --build
```

Todo o sistema sobe.

## Estudar

Agora entram:

```text
Build-time variables

Runtime variables

Frontend Dockerfile

Multi-stage Node build

Container networking
```

---

# Fase 12 — Development Compose vs Full Compose

Aqui vale organizar o workflow.

Durante desenvolvimento:

```text
Postgres → Docker

API → dotnet watch

Web → npm run dev
```

Quando quiser testar ambiente completo:

```text
docker compose up
```

Você pode até posteriormente separar:

```text
compose.yml

compose.override.yml

compose.prod.yml
```

Mas não precisa fazer isso imediatamente.

---

# Fase 13 — Authentication

Agora implemente autenticação.

Funcionalidades:

```text
Register

Login

Logout

Refresh Token

Protected Endpoints
```

Depois:

```text
User A

não pode acessar

Account / Transaction do User B
```

## Estudar

```text
Authentication

Authorization

JWT

Claims

Access Token

Refresh Token

Password Hashing

401

403
```

Essa é uma das fases mais importantes do backend.

---

# Fase 14 — Credit Cards

Agora começamos a parte realmente interessante do domínio financeiro.

```text
CreditCard

name

limit

closingDay

dueDay
```

Relacionamentos:

```text
User
 │
 └── CreditCard
       │
       └── Purchases
```

## Estudar

```text
Aggregate

Invariant

Domain Rule

Money

decimal
```

Nunca use `float` para dinheiro.

---

# Fase 15 — Installments

Agora:

```text
Purchase

R$ 1.200

4 installments
```

gera:

```text
1/4 → R$300

2/4 → R$300

3/4 → R$300

4/4 → R$300
```

Mas agora entra o problema real:

```text
qual mês recebe cada parcela?
```

Dependendo de:

```text
purchaseDate

closingDay

dueDay
```

## Estudar

```text
Date calculations

Domain service

Idempotency

Precision

Rounding
```

Excelente parte para testes unitários.

---

# Fase 16 — Invoices

Agora criamos:

```text
CreditCardInvoice
```

Conceito:

```text
Card

├── September Invoice
├── October Invoice
├── November Invoice
└── December Invoice
```

Cada compra/parcela cai em uma invoice.

## Funcionalidades

```text
Current invoice

Future invoices

Invoice total

Available card limit

Invoice due date
```

Esse módulo vai ser um dos maiores diferenciais do projeto.

---

# Fase 17 — Recurring Transactions

Exemplo:

```text
Netflix

Monthly

R$ 59.90
```

ou:

```text
Salary

Monthly

R$ 4.100
```

Agora surge processamento periódico.

Inicialmente você pode gerar recorrências de forma simples.

Depois será uma ótima justificativa para Background Jobs e mensageria.

## Estudar

```text
Scheduling

Background processing

Idempotency

Recurring jobs
```

---

# Fase 18 — Dashboard

Agora finalmente temos dados suficientes para um dashboard relevante.

Exibir:

```text
Current balance

Income this month

Expenses this month

Credit card invoice

Future expenses

Expenses by category

Income vs expenses

Monthly history
```

Frontend:

```text
ECharts
```

Backend começa a ter endpoints específicos de leitura.

Exemplo:

```http
GET /dashboard/summary
```

## Estudar

```text
Aggregation

GROUP BY

Projection

Read Model

Query optimization

CQRS concepts
```

Sem implementar CQRS complexo.

Apenas entender:

```text
Write model

!=

Read model
```

---

# Fase 19 — Integration Tests com Testcontainers

Agora você começa a usar containers também nos testes.

Fluxo:

```text
Test
 ↓
Start PostgreSQL Container
 ↓
Run API
 ↓
Execute test
 ↓
Destroy container
```

Com Testcontainers.

## Testar

```text
POST /accounts

GET /accounts

POST /transactions

Transfers

Authentication
```

## Estudar

```text
Integration environment

Test isolation

Containerized tests

Database reset

Deterministic tests
```

---

# Fase 20 — E2E Testing

Agora Playwright.

Fluxos:

```text
Login

Create Account

Create Transaction

View Dashboard
```

Outro:

```text
Create credit card

Add installment purchase

Check invoice
```

Aqui você testa o produto, não apenas código.

---

# Fase 21 — Redis

Agora existe motivo real para Redis.

Por exemplo:

```text
GET /dashboard/summary
```

pode executar diversas agregações.

Fluxo:

```text
Request
  ↓
Redis
 │
 ├── HIT → Return
 │
 └── MISS
       ↓
    PostgreSQL
       ↓
     Cache
       ↓
     Return
```

Docker Compose agora:

```text
web

api

postgres

redis
```

## Estudar

```text
Cache

TTL

Cache Hit

Cache Miss

Cache Invalidation

Distributed Cache
```

---

# Fase 22 — Background Jobs

Agora implemente jobs.

Exemplos:

```text
Generate recurring transactions

Generate monthly invoices

Update summaries
```

Você pode começar com um worker dentro da solução.

Depois evoluir.

## Estudar

```text
Worker Service

HostedService

BackgroundService

Retry

CancellationToken
```

---

# Fase 23 — Messaging

Agora mensageria passa a ter propósito.

Evento:

```text
TransactionCreated
```

Fluxo:

```text
API
 │
 ▼
Message Broker
 │
 ├── Analytics Consumer
 └── Notification Consumer
```

Local:

```text
RabbitMQ
```

Compose:

```text
web

api

postgres

redis

rabbitmq
```

## Estudar

```text
Producer

Consumer

Queue

Exchange

Event

Retry

Dead Letter Queue

Eventual Consistency

Idempotency
```

---

# Fase 24 — Observabilidade local

Antes da AWS, você pode começar com:

```text
Structured Logging

OpenTelemetry
```

Observar:

```text
Request
 ↓
API
 ↓
Database
 ↓
Message
 ↓
Consumer
```

## Estudar

```text
Logs

Metrics

Tracing

Correlation ID

Distributed tracing
```

---

# Fase 25 — CI

Agora GitHub Actions.

Pull Request:

```text
Frontend

├── lint
├── typecheck
└── tests


Backend

├── restore
├── build
└── tests
```

Depois:

```text
Integration Tests
```

com PostgreSQL container.

## Pipeline

```text
Push
 ↓
Build
 ↓
Test
 ↓
Validate
```

Nenhum deploy ainda.

---

# Fase 26 — Docker Image Registry

Agora pipelines começam a gerar imagens.

```text
GitHub Actions
      │
      ▼
docker build
      │
      ▼
Container Registry
```

Posteriormente:

```text
Amazon ECR
```

---

# Fase 27 — AWS Foundation

Agora cloud.

Comece simples.

Backend:

```text
ECS Fargate
```

Banco:

```text
RDS PostgreSQL
```

Arquivos:

```text
S3
```

Logs:

```text
CloudWatch
```

Frontend pode inicialmente continuar separado ou ir para:

```text
Amplify

ou

Container
```

dependendo da decisão final.

---

# Fase 28 — Arquitetura AWS completa

A arquitetura pode evoluir para:

```text
Internet
   │
   ▼
CloudFront
   │
   ▼
Frontend
   │
   ▼
Application Load Balancer
   │
   ▼
ECS Fargate
   │
   ├── RDS PostgreSQL
   │
   ├── ElastiCache Redis
   │
   └── SQS
```

Monitoramento:

```text
CloudWatch
```

Tracing:

```text
OpenTelemetry
```

---

# Fase 29 — SQS

Aqui você pode substituir ou complementar RabbitMQ:

```text
RabbitMQ local
```

por:

```text
SQS AWS
```

E entender diferenças.

Muito importante para entrevistas:

```text
Queue vs Pub/Sub

Managed broker

Visibility timeout

Dead Letter Queue

Retry
```

---

# Fase 30 — Terraform

Agora finalmente IaC.

Terraform cria:

```text
VPC

ECS

RDS

SQS

Redis

Security Groups

IAM

CloudWatch
```

Estrutura:

```text
infrastructure/

├── docker/
│
└── terraform/
    ├── modules/
    ├── environments/
    │   ├── dev/
    │   └── prod/
    └── main.tf
```

## Estudar

```text
Infrastructure as Code

State

Provider

Resource

Module

Plan

Apply
```

---

# Fase 31 — CD

Agora:

```text
Merge main
   ↓
Tests
   ↓
Build Docker Image
   ↓
Push ECR
   ↓
Deploy ECS
```

Pipeline completo.

---

# Fase 32 — Architecture Decision Records

Durante todo o projeto, registre decisões.

Exemplos:

```text
001-modular-monolith.md

002-postgresql.md

003-docker-compose.md

004-nextjs.md

005-jwt.md

006-redis.md

007-rabbitmq.md

008-aws-ecs.md
```

Formato:

```text
Context

Decision

Alternatives

Consequences
```

Isso deve acontecer continuamente, não só no final.

---

# Fase 33 — README profissional

README final:

```text
Project overview

Architecture diagram

Features

Tech stack

Local setup

Docker setup

Environment variables

Tests

API documentation

System design decisions

Cloud architecture

Screenshots

Demo

Roadmap
```

Aqui o repositório passa de:

```text
"meu projeto"
```

para:

```text
"case técnico de engenharia"
```

---

# Fluxo completo de evolução

O projeto agora evolui assim:

```text
Requirements
      ↓
Repository
      ↓
Docker + PostgreSQL
      ↓
ASP.NET
      ↓
EF Core
      ↓
Accounts
      ↓
Categories
      ↓
Transactions
      ↓
Tests
      ↓
Docker API
      ↓
Next.js
      ↓
Frontend Integration
      ↓
Docker Web
      ↓
Authentication
      ↓
Credit Cards
      ↓
Installments
      ↓
Invoices
      ↓
Recurring Transactions
      ↓
Dashboard
      ↓
Integration Tests
      ↓
E2E
      ↓
Redis
      ↓
Background Jobs
      ↓
RabbitMQ
      ↓
Observability
      ↓
CI
      ↓
AWS
      ↓
SQS
      ↓
Terraform
      ↓
CD
      ↓
Portfolio Release
```

---

# Como eu dividiria em milestones

| Milestone | Objetivo |
|---|---|
| M0 | Planning |
| M1 | Docker Foundation |
| M2 | Core Backend |
| M3 | Transactions |
| M4 | Frontend |
| M5 | Authentication |
| M6 | Credit Cards |
| M7 | Financial Engine |
| M8 | Dashboard |
| M9 | Testing |
| M10 | Redis & Background Jobs |
| M11 | Messaging |
| M12 | Observability |
| M13 | CI/CD |
| M14 | AWS |
| M15 | Infrastructure as Code |
| M16 | Portfolio Release |

---

# O Milestone 1 que eu começaria agora

Eu começaria exatamente assim:

```text
M1 — Foundation

[ ] Criar repository

[ ] Criar monorepo structure

[ ] Criar README inicial

[ ] Criar /docs

[ ] Criar requirements.md

[ ] Criar business-rules.md

[ ] Criar architecture.md

[ ] Criar docker-compose.yml

[ ] Criar PostgreSQL container

[ ] Criar volume persistente

[ ] Criar .env

[ ] Criar .env.example

[ ] Testar conexão PostgreSQL

[ ] Criar solução .NET

[ ] Criar Domain

[ ] Criar Application

[ ] Criar Infrastructure

[ ] Criar API

[ ] Criar GET /health

[ ] Conectar API → PostgreSQL

[ ] Criar migration inicial

[ ] Criar primeiro integration test
```

E só então:

```text
M2 — Accounts
```

Essa versão considera Docker como parte natural do ambiente desde o começo, sem transformar infraestrutura em complexidade prematura.

A evolução ideal é:

```text
Simple
  ↓
Correct
  ↓
Tested
  ↓
Containerized
  ↓
Observable
  ↓
Scalable
  ↓
Cloud-ready
```

O foco do projeto deve continuar sendo entender o motivo de cada decisão, e não simplesmente acumular tecnologias.

A regra de estudo durante o projeto deve ser:

```text
Study
  ↓
Implement
  ↓
Break
  ↓
Debug
  ↓
Test
  ↓
Document
```

Sempre que uma nova tecnologia for adicionada, deve existir uma justificativa concreta no projeto para ela.
