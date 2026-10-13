# PFP---Fintech
Personal Finance Platform

Projeto de portfólio e uso pessoal, sem serviços pagos obrigatórios. Ambiente
funcional completo executa localmente via Docker Compose; hospedagem pública
contínua e provisionamento AWS não fazem parte da entrega. Demonstração por
screenshots, vídeo e setup reproduzível. CI/registry públicos gratuitos são
opcionais; execução e build locais permanecem disponíveis.

Diretrizes e fases futuras no
[roadmap](docs/personal-finance-platform-roadmap.md#restrição-de-custo-e-publicação).

## Executar ambiente completo — Fase 27

Pré-requisitos: Docker Engine/Docker Desktop ativo com Compose v2 (suporte a
`up --wait`), GNU Make e OpenSSL. Linux: usuário precisa acessar Docker sem
erro de permissão. No Windows, execute comandos pelo WSL2 com integração Docker.
SDK .NET e Node.js locais são necessários somente para desenvolvimento/testes;
build completo usa containers. Primeiro build exige internet para bases e pacotes.

Na raiz, após clonar repositório:

```bash
docker info
docker compose version
make setup
make config-check
make full
make status
```

`setup` cria `.env` com senhas aleatórias PostgreSQL/RabbitMQ e chave JWT, com
permissão 600. Se arquivo existir, preserva conteúdo; revise valores conforme
`.env.example`. Não publique `.env` ou compartilhe saída de `docker compose config`
sem `--quiet`: ela contém segredos. `config-check` valida sem imprimi-los.
`full` constrói imagens, inicia serviços, aguarda healthchecks existentes e
aplica migrations antes da API. `migrate` encerrado com código 0 é esperado.
Collector, Jaeger e Prometheus podem levar alguns segundos adicionais para
receber dados; healthcheck HTTP da API não comprova conexão com dependências.

Abra frontend em **http://localhost:3000** e cadastre usuário na tela de registro.
Não há conta inicial nem dados financeiros predefinidos. Depois crie conta,
categorias e lançamentos; dashboard usa dados reais do usuário autenticado.

Serviços e portas padrão, expostos somente no loopback:

| Serviço | Endereço / porta | Uso |
| --- | --- | --- |
| Frontend | http://localhost:3000 | Interface e sessão |
| API | http://localhost:5168/health | Disponibilidade HTTP |
| PostgreSQL | localhost:5432 | Banco persistente |
| Redis | localhost:6379 | Cache descartável |
| RabbitMQ | localhost:5672 | Mensageria |
| RabbitMQ UI | http://localhost:15672 | Usuário/senha do `.env` |
| Collector | localhost:4317 | Entrada OTLP gRPC |
| Jaeger | http://localhost:16686 | Traces |
| Prometheus | http://localhost:9090 | Métricas, targets e consultas |

Na rede Docker, serviços usam nomes `api`, `postgres`, `redis`, `rabbitmq` e
`otel-collector`; não use `localhost` para comunicação entre containers.
Para resolver conflito de portas, altere variáveis `*_PORT` do `.env` e execute
`make full` novamente. `AUTH_COOKIE_SECURE=false` atende HTTP local. Recursos
Redis, jobs, RabbitMQ e observabilidade já vêm configurados para stack completa;
`BACKGROUND_JOBS_ENABLED`, `MESSAGING_ENABLED` e `OBSERVABILITY_ENABLED` controlam
respectivos recursos. Não existem credenciais ou serviços AWS necessários.

### Migrations, logs e encerramento

Após atualizar código, execute `make full` para reconstruir imagens e aplicar
migrations pendentes. Para reaplicar verificação de migrations separadamente,
com imagem API atualizada disponível:

```bash
make migrate
make logs
# Ctrl+C encerra somente acompanhamento dos logs.
make stop
```

`stop` remove containers/rede, preservando volumes. Para iniciar novamente:
`make full`, ou `make images-run IMAGE_TAG=phase26` se essas imagens e todas as
dependências já estiverem disponíveis. `images-run` não compila nem baixa imagens.

PostgreSQL preserva usuários, sessões e dados financeiros; RabbitMQ preserva
filas/mensagens persistentes e Prometheus guarda métricas por sete dias.
Redis mantém somente cache em memória; Jaeger também perde traces ao reiniciar.
Volumes são vinculados ao nome do projeto Compose. Mantenha checkout/nome do
projeto estável; trocar nome pode abrir volumes novos e parecer perder dados.
Não execute `docker compose down --volumes` no ambiente pessoal: apaga volumes
e seus dados. Nenhum comando Make remove volumes. Faça backup antes de atualizar
schema ou alterar armazenamento.

### Problemas comuns

- Falha de configuração: `make config-check`; complete `.env` conforme exemplo.
  Chave JWT precisa ter pelo menos 32 bytes; senhas RabbitMQ geradas são
  alfanuméricas, adequadas à URI AMQP.
- Porta ocupada: pare processos locais ou altere portas no `.env`.
- Container unhealthy ou migrations falharam: `docker compose logs --tail=100
  postgres migrate api web rabbitmq`; corrija causa antes de repetir `make full`.
- Alterar senha/usuário PostgreSQL no `.env` não altera banco já inicializado
  no volume. Preserve credenciais originais ou faça alteração explícita no banco.
- Métricas ausentes: confira Prometheus em `/targets` e logs de `otel-collector`.
  Gere requisições na API antes de buscar traces no Jaeger (`fintrack-api`).

Decisão: [023 — Entrega funcional local](docs/decisions/023-local-functional-delivery.md).

Validação da fase 27: setup novo/repetido, permissão 600 e rejeição de JWT ausente
aprovados. Compose completo isolado validou migrations, API/frontend, autenticação,
lançamento financeiro, Redis, RabbitMQ, coleta Collector/Prometheus e UI Jaeger.
Reaplicação de migrations e persistência de usuário/conta após down/up aprovadas.
Containers, rede e volumes de teste removidos; ambiente pessoal preservado.


## Fase 28 — Arquitetura AWS conceitual

**Estudo de portfólio, não implantado.** Ambiente real continua local; nenhuma
conta, credencial ou recurso AWS foi criado. GHCR permanece fora do escopo.

- [Estudo e comparação](docs/architecture/aws-conceptual.md): disponibilidade,
  segurança, escala, dimensões de cobrança e limites da migração.
- [Diagrama Mermaid](docs/diagrams/aws-conceptual.md): ECS Fargate, RDS,
  ElastiCache, SQS, S3, CloudFront, ALB e CloudWatch, com complementos necessários.
- [ADR 024](docs/decisions/024-cloud-conceptual-architecture.md): motivos para
  manter serviços gerenciados fora da execução pessoal sem cobrança.

Next.js SSR exige runtime; SQS requer adaptação do transporte. Diagrama não
representa deploy realizado nem altera Compose, CI ou código da aplicação.

## Fase 29 — Mensageria local e estudo de SQS

RabbitMQ permanece implementação real, com outbox, publisher confirms, retries,
deduplicação por consumidor e DLQs. [Demonstração e comparação com SQS](docs/architecture/rabbitmq-and-sqs.md)
explicam fanout, visibility timeout, long polling, entrega e dead letters com
referências oficiais. [ADR 025](docs/decisions/025-local-messaging-sqs-comparison.md)
mantém SQS/SNS somente como estudo, sem SDK, recursos ou credenciais AWS.

Para validar garantias com PostgreSQL/RabbitMQ descartáveis, sem usar `.env` ou
banco pessoal:

```bash
make test-messaging
```

Diagnóstico local somente leitura disponível em
[messaging-diagnostics.sql](docs/database/messaging-diagnostics.sql).
Não há replay automático nem envio de notificações externas.
Validação da fase 29: 5 unitários e 9 integrações de mensageria aprovados,
sem skips. Links locais e diagnóstico somente leitura conferidos.

## Fase 30 — Infrastructure as Code local

Docker Compose permanece gerenciador da aplicação. [Laboratório Terraform Docker](ops/terraform/local/README.md)
pratica provider, resources, módulo reutilizável, state local e ciclo plan/apply/destroy
com servidor HTTP descartável, sem volumes ou dados pessoais. [ADR 026](docs/decisions/026-local-terraform-lab.md)
registra isolamento e decisões.

Com Terraform CLI e Docker local disponíveis:

```bash
make iac-init
make iac-validate
make iac-plan
# Revise plano: somente rede, imagem e container do laboratório.
make iac-apply
make iac-destroy
```

Apply executa plano salvo; destroy pede confirmação. State/planos ficam fora do
Git; lockfile fica versionado. Sem AWS, backend remoto, HCP Terraform ou GHCR.
Exercício opcional: não substitui `make full` nem gerencia recursos Compose.
Validação: fmt/validate, plan/apply, HTTP e destroy aprovados; segundo plan sem
mudanças. Containers existentes preservados. Terraform CLI não foi instalado globalmente.

## Backend local

Pré-requisitos: SDK .NET 10 e chave JWT configurada conforme fase 13. PostgreSQL em Docker é necessário para operações
de contas e categorias; `/health` não consulta o banco.

Na raiz do repositório:

```bash
dotnet restore apps/api/FinTrack.sln -m:1
dotnet build apps/api/FinTrack.sln --no-restore -m:1
dotnet test apps/api/FinTrack.sln --no-build --no-restore -m:1
```

Configure conexão local com User Secrets, substituindo valores de exemplo:

```bash
dotnet user-secrets set "ConnectionStrings:Database" "Host=localhost;Port=5432;Database=fintrack;Username=SEU_USUARIO;Password=SUA_SENHA" --project apps/api/src/FinTrack.Api
dotnet run --project apps/api/src/FinTrack.Api --launch-profile http
```

Perfil `http` usa ambiente `Development` e porta `5168`. Mantenha terminal
da API aberto. Em outro terminal:

```bash
curl -i http://localhost:5168/health
```

Resposta esperada: HTTP `200`, conteúdo `application/json` e
`{"status":"healthy"}`. Este endpoint verifica disponibilidade HTTP da
aplicação, não disponibilidade do PostgreSQL.

Para acompanhar mudanças durante desenvolvimento:

```bash
dotnet watch --project apps/api/src/FinTrack.Api run --launch-profile http
```

## Fase 3 — Backend Skeleton

- Quatro camadas em `apps/api/src`: Api, Application, Domain e Infrastructure.
- Três projetos de testes em `apps/api/tests`: UnitTests, IntegrationTests e ArchitectureTests.
- Api coordena HTTP e composição das dependências; Application coordena casos de uso.
- Domain contém entidades e regras; Infrastructure implementa persistência.
- Api referencia Application e Infrastructure para registrar implementações.
- Application referencia Domain; Infrastructure referencia Application e Domain.
- Domain não depende das outras camadas.

Requisição chega ao pipeline ASP.NET Core, encontra endpoint Minimal API e
produz resposta HTTP. Injeção de dependência fornece serviços registrados
ao endpoint. Configuração varia conforme ambiente e pode vir de JSON,
variáveis de ambiente e User Secrets em desenvolvimento.

Testes de arquitetura verificam referências entre assemblies. Testes de
integração iniciam API em memória e validam `/health` em Development e
Production com uma conexão de banco propositalmente inacessível.
Não exigem PostgreSQL nem conexão pessoal configurada.

## Fase 7 — Testes desde cedo

Regras de domínio ficam em testes unitários; HTTP, migrations e persistência
ficam em testes de integração. Toda nova funcionalidade importante deve
incluir testes pertinentes, organizados em Arrange / Act / Assert.

- UnitTests cobre entidades, validação e handlers de contas e categorias,
  usando test doubles de repositórios sem acesso ao banco.
- IntegrationTests cobre `/health` sem banco e contas/categorias com PostgreSQL
  real: criação, leitura, listagem, atualização, exclusão de conta, 404 e
  validação sem alteração dos dados.
- ArchitectureTests verifica dependências entre camadas.

Integração PostgreSQL usa Testcontainers automaticamente (fase 19). Com Docker
ativo e acessível ao usuário:

```bash
docker info
dotnet test apps/api/FinTrack.sln -m:1
# Somente integração:
dotnet test apps/api/tests/FinTrack.IntegrationTests -m:1
```

Não é necessário iniciar Compose, escolher porta ou configurar banco externo.
Cada classe recebe PostgreSQL `postgres:17-alpine` em container próprio, porta
aleatória e credenciais efêmeras. Fixture espera prontidão, aplica migrations,
valida upgrade com registros legados e encerra API antes de remover container.
Cada cenário cria usuário próprio; testes de migração usam somente seed fixo.
Sem Docker acessível ou se migrations falharem, suíte falha em vez de ignorar
integração. `FINTRACK_TEST_DATABASE` não é mais utilizado.

Sem Docker, execute somente testes de domínio/aplicação e arquitetura:

```bash
make test-fast
```

Base de Transactions para receitas/despesas recorrentes existe na fase 17.
CRUD manual, transferências e saldo incremental da fase 6 permanecem pendentes;
seus testes de domínio e atomicidade deverão acompanhar implementação.
Testcontainers implementado na fase 19.

## Fase 8 — API em Docker

Na raiz, copie `.env.example` para `.env` caso ainda não exista e configure
credenciais locais do PostgreSQL. `API_PORT` é opcional e usa `5168` por padrão.

```bash
docker compose up --build -d --wait
curl --fail http://localhost:5168/health
docker compose logs api migrate
docker compose down
```

Compose inicia PostgreSQL, aguarda seu healthcheck, executa `migrate` e inicia
API somente quando migrations terminam com sucesso. `migrate` é um container
provisório com a mesma imagem da API, executado com `--migrate`; aplica migrations
pendentes e termina sem abrir servidor HTTP. Falha de migration impede startup
da API. O comando pode ser repetido: migrations já aplicadas não são reaplicadas.

Dockerfile usa SDK .NET 10 para restore/publicação em Release e imagem ASP.NET
Core para execução. Runtime não inclui SDK nem fontes, executa como usuário sem
privilégios e expõe porta `8080`. Healthcheck consulta `/health` via HTTP: verifica
servidor, não conexão com banco. `.dockerignore` exclui artefatos locais e testes
do contexto de build. Credenciais chegam por variáveis em runtime.

Dentro da rede Compose, API encontra PostgreSQL em `postgres:5432`; porta
`POSTGRES_PORT` é usada apenas por clientes na máquina. Serviço `api` publica
`127.0.0.1:${API_PORT:-5168}:8080`. Para manter desenvolvimento com API local:

```bash
docker compose up -d postgres
dotnet watch --project apps/api/src/FinTrack.Api run --launch-profile http
```

Use `Host=localhost` na conexão da API local. API local e API Docker usam mesma
porta padrão; pare serviço `api` ou configure outra `API_PORT` antes de executar
ambas. `docker compose down` preserva volume PostgreSQL; dados persistem entre
execuções. Para diagnóstico: `docker compose ps` e `docker compose logs`.

Dockerização cobre módulos atuais do backend. Transações geradas por
recorrências estão na fase 17; módulo completo da fase 6 permanece pendente.

## Fase 9 — Frontend Skeleton

Pré-requisitos: Node.js 24 e API Docker da fase 8. Na raiz:

```bash
docker compose up --build -d --wait
cd apps/web
npm ci
npm run dev
```

Frontend local: `http://localhost:3000`. `/` encaminha para `/dashboard`.
Rotas iniciais: `/dashboard`, `/accounts`, `/transactions` e `/categories`.
Navegação indica página atual e layout adapta-se a telas menores.

`API_URL` usa `http://localhost:5168` por padrão. Para outra porta/endereço,
crie `apps/web/.env.local` usando `apps/web/.env.example` como referência.
Reinicie Next.js após modificar variável. A URL é usada no servidor Next.js;
navegador não chama API diretamente, então este fluxo não exige CORS.

Estrutura em `src`: `app`, `components`, `features`, `hooks`, `services`,
`schemas`, `types`, `utils` e `config`. Layout e páginas são Server Components;
navegação e busca são Client Components com React State. Serviço HTTP faz
leitura sem cache, timeout de cinco segundos e valida respostas com Zod.
Contas e categorias exibem dados reais da API e permitem busca sem distinção
de acentos/maiúsculas. Lista vazia e busca sem resultados têm mensagens próprias.

`loading.tsx` fornece estado de carregamento; `error.tsx` trata falhas com
botão de nova tentativa. API indisponível não é apresentada como lista vazia.
`not-found.tsx` trata rotas inexistentes. Dashboard inicial contém navegação
para módulos, sem valores financeiros fictícios. Transações exibem estado
“em breve”: integração web com lançamentos recorrentes ainda está pendente. Formulários de
cadastro/edição, exclusão e integração de escrita pertencem à fase 10.

Validação no diretório `apps/web`:

```bash
npm run lint
npm run typecheck
npm test
npm run build
```

Testes automatizados cobrem contrato de respostas da API e normalização da
busca. Build, rotas HTTP, redirect, 404, leitura API/PostgreSQL, listas vazias e
comportamento com API offline foram verificados em stack isolada. Validação
visual e interações no navegador ainda precisam de revisão manual.

## Fase 11 — Frontend em Docker

Configure `.env` na raiz com base em `.env.example`. `WEB_PORT` usa `3000`
por padrão. Para iniciar sistema completo:

```bash
docker compose up --build -d --wait
```

Frontend: `http://localhost:3000`. API: `http://localhost:5168`.
Portas podem ser alteradas por `WEB_PORT`, `API_PORT` e `POSTGRES_PORT`.
Compose aguarda banco saudável, migrations concluídas e API saudável antes
de iniciar frontend. Para consultar estado, logs e encerrar:

```bash
docker compose ps
docker compose logs web api migrate
docker compose down
```

Dockerfile frontend separa instalação (`npm ci` com lockfile), build Next.js e
runtime Node.js 24 Alpine. Saída `standalone` copia somente servidor e dependências
necessárias, além de `.next/static` e `public`; runtime usa usuário `node`,
sem privilégios. `.dockerignore` impede inclusão de dependências locais,
artefatos e arquivos `.env` no contexto. Healthcheck consulta `/dashboard`,
verificando servidor web; não verifica disponibilidade da API.

Na rede Compose, frontend chama API em `http://api:8080`, que chama PostgreSQL
em `postgres:5432`. `API_URL` é variável **de runtime**, lida no servidor nas
páginas dinâmicas; imagem web pode ser reutilizada com outro endereço de API
sem reconstrução. Não é preciso configurar `API_URL` em build. Variáveis
`NEXT_PUBLIC_*`, se introduzidas futuramente, são incorporadas ao bundle durante
build e não devem conter segredos. Browser acessa apenas origem do frontend.

Frontend local da fase 9 continua usando `http://localhost:5168`; variáveis em
`apps/web/.env.local` ficam fora da imagem Docker. Pare `web` antes de executar
Next.js local na mesma porta. Volume PostgreSQL persiste após `compose down`.
Dockerização não implementa CRUD pendente da fase 10 nem transações da fase 6.

## Fase 12 — Desenvolvimento vs ambiente completo

`Makefile` na raiz reúne os dois workflows. Pré-requisitos: GNU Make, Docker
Compose, SDK .NET 10 e Node.js 24. `make help` lista comandos.

### Desenvolvimento

Neste modo, PostgreSQL e Redis executam em Docker. API e frontend executam
localmente com atualização automática.

1. Configure `.env` da raiz com as credenciais e porta do PostgreSQL e execute:

   ```bash
   make dev-db
   ```

   Este comando para `web` e `api` do Compose antes de iniciar PostgreSQL/Redis.
   Volume e dados são preservados; container de migrations pode permanecer
   listado como encerrado. Banco e cache aguardam healthchecks antes de concluir.

2. Configure conexão da API local com User Secrets (uma vez), usando os valores
   de `.env` e **Host=localhost**, e aplique migrations:

   ```bash
   dotnet user-secrets set "ConnectionStrings:Database" "Host=localhost;Port=5432;Database=fintrack;Username=SEU_USUARIO;Password=SUA_SENHA" --project apps/api/src/FinTrack.Api
   make dev-migrate
   ```

   `dev-migrate` usa a mesma configuração da API local e aplica migrations sem
   iniciar servidor HTTP. Execute novamente após adicionar migrations.

3. Em um terminal, execute API:

   ```bash
   make dev-api
   ```

4. Em outro terminal, instale dependências se necessário e execute frontend:

   ```bash
   npm --prefix apps/web ci
   make dev-web
   ```

Next.js permite uma instância de desenvolvimento por checkout, mesmo com portas
diferentes. Reutilize servidor existente ou encerre seu terminal antes de
iniciar outro.

API local usa `5168`; frontend usa `3000`. Para portas alternativas:

```bash
make dev-api API_PORT=5169
make dev-web WEB_PORT=3001
```

Se porta da API mudar, ajuste `API_URL=http://localhost:5169` em
`apps/web/.env.local` e reinicie frontend. Portas dos processos locais são
opções do Make; portas Docker vêm de `.env`. `.env` da raiz configura Compose,
não é carregado automaticamente pela API local. User Secrets ou variável
`ConnectionStrings__Database` configuram a conexão local.

### Ambiente completo

Pare processos locais com `Ctrl+C` nos dois terminais antes de alternar:

```bash
make full
make status
```

`full` executa `docker compose up --build -d --wait`. Todo runtime fica em
Docker, com conexões `web` para `api:8080` e `api` para `postgres:5432`.
O mesmo volume PostgreSQL é usado nos dois modos, mantendo dados entre eles.
Para voltar ao desenvolvimento, execute `make dev-db` e inicie processos locais.

```bash
make stop
```

`stop` encerra Compose e preserva volume; processos locais devem ser encerrados
nos seus terminais. Nenhum comando do Make remove dados. Não há arquivos
Compose adicionais obrigatórios nesta fase: seleção explícita de serviços
mantém fluxo simples. Configuração de produção permanece fora deste escopo.

Validação da fase 12: `make full`, alternância para `dev-db`, migrations locais,
API com dotnet watch e preservação de dados passaram em ambiente isolado.
Comando `dev-web` foi conferido; nova instância Next.js não pôde ser validada
porque já havia servidor de desenvolvimento neste checkout.

## Fase 13 — Authentication

Autenticação implementada na API e frontend. Crie acesso em `/register` e entre
em `/login`; dashboard, contas, categorias e transações exigem sessão. Navegação
mostra e-mail autenticado e botão `Sair`. Tokens ficam em cookies **HttpOnly**
com **SameSite=Lax**, sem armazenamento em localStorage ou retorno no estado
client do formulário. Server Actions fazem login, registro, renovação e logout.
Quando access token expira, página de login oferece `Renovar sessão` enquanto
cookie de refresh existir; renovação é explícita por POST, sem mutação em GET.

### Configuração

Compose recebe chave por `JWT_SIGNING_KEY` do `.env` da raiz. Nesta execução,
chave aleatória foi gerada no `.env` local quando ausente, sem versionamento.
Em checkout novo, gere chave e coloque em `JWT_SIGNING_KEY`:

```bash
openssl rand -base64 48
make full
```

API recusa startup sem `Jwt:SigningKey` com pelo menos 32 bytes. Chave não é
incorporada às imagens. Não reutilize chave de teste. API local exige configuração
própria via User Secrets (independente do `.env` de Compose):

```bash
dotnet user-secrets set "Jwt:SigningKey" "$(openssl rand -base64 48)" --project apps/api/src/FinTrack.Api
make dev-db
make dev-migrate
make dev-api
```

`dev-migrate` não exige chave JWT e continua usando conexão local configurada.
`AUTH_COOKIE_SECURE=false` atende HTTP local; configure **true** para frontend
servido por HTTPS. Issuer padrão `FinTrack`, audience `FinTrack.Client`, access
TTL de 15 minutos e sessão/refresh de 7 dias. `Jwt:AccessMinutes` aceita 1–60 e
`Jwt:RefreshDays` aceita 1–30. Renovação não prolonga limite absoluto da sessão.

### API

- `POST /auth/register`: `{ "email": "user@example.com", "password": "Password!123456" }`.
- `POST /auth/login`: mesmos campos.
- `POST /auth/refresh`: `{ "refreshToken": "TOKEN_RECEBIDO" }`.
- `POST /auth/logout`: exige access token; encerra sessão atual.
- `GET /auth/me`: exige access token; retorna ID e e-mail do usuário.

Registro retorna `201`; login e refresh, `200`, com `accessToken`, `expiresAt`,
`refreshToken` e `refreshExpiresAt`. Registro valida e-mail e senha de 12–128
caracteres (`400` para dados inválidos); e-mail normalizado é único (`409` para
duplicado). Credenciais/token inválidos, expirados ou revogados retornam `401`.
Envie `Authorization: Bearer ACCESS_TOKEN` em contas/categorias e endpoints
protegidos de auth. `/health` permanece público e sem consulta ao banco.

Senhas são armazenadas pelo PasswordHasher do ASP.NET Core (PBKDF2 com salt e
210.000 iterações). JWT HS256 valida assinatura, issuer, audience e expiração,
com claims `sub`, `sid`, `email`, `jti` e `iat`. Middleware também verifica sessão
ativa no banco, permitindo revogar access token imediatamente no logout.
Refresh tokens usam 64 bytes aleatórios; banco guarda apenas hash SHA-256.
Rotação usa comparação e atualização atômica: apenas uma requisição concorrente
pode consumir token; token antigo não pode ser reutilizado.

Registro, login e refresh têm limite compartilhado de 20 requisições/minuto por
IP (`429` quando excedido; `Auth:RequestsPerMinute` configura limite). Sessão/me
não consomem esse limite. No fluxo Next.js, API observa IP do servidor frontend;
limite pode ser compartilhado pelos seus clientes. Logout revoga sessão atual,
não sessões de outros dispositivos. Se API estiver indisponível no logout web,
cookies locais são removidos; sessão no banco permanece até revogação/expiração.

### Isolamento e dados existentes

Migration `AddAuthentication` cria `users`, `refresh_sessions` e `user_id` em
contas/categorias. Repositório atribui usuário autenticado ao criar recurso;
filtros EF limitam leitura, edição e exclusão ao proprietário. ID pertencente
a outro usuário retorna **404** para não revelar sua existência. `403` fica
reservado a futuras políticas de permissão; nesta fase não existem papéis.

Dados anteriores são preservados com `user_id` nulo e ficam **inacessíveis**
a usuários novos. Nenhum registro é atribuído automaticamente. Recuperação
exige atribuição explícita ao usuário correto após revisão de titularidade.
Transações geradas por recorrências seguem o mesmo isolamento na fase 17.
Futuros lançamentos manuais e transferências deverão manter essa política.

### Validação

86 testes backend e 5 frontend passaram. Cobertura inclui credenciais, senha
hashed, endpoints anônimos, assinatura/issuer/audience/expiração JWT, logout,
sessão expirada, refresh concorrente, isolamento de contas/categorias e upgrade
com dados antigos. Build Docker, healthchecks e formulários web foram verificados
por HTTP real em stack isolada, incluindo cookies HttpOnly/SameSite e revogação.
Revisão visual e interações por navegador seguem pendentes.

Implementação estudada a partir da [documentação oficial de JWT bearer](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/configure-jwt-bearer-authentication?view=aspnetcore-10.0).

### Cartões de crédito — fase 14

API autenticada para criar, listar, consultar, atualizar e excluir cartões:

```text
POST   /credit-cards
GET    /credit-cards
GET    /credit-cards/{id}
PUT    /credit-cards/{id}
DELETE /credit-cards/{id}
```

POST e PUT recebem:

```json
{
  "name": "Cartão principal",
  "limit": 5000.50,
  "closingDay": 25,
  "dueDay": 5
}
```

Resposta inclui `id` e os quatro campos. Nome é normalizado com trim e limitado
a 100 caracteres. Limite deve ser positivo, com até duas casas decimais e
16 dígitos inteiros; dias aceitam 1–31. Proprietário vem do JWT; cada usuário
acessa somente seus cartões. Entrada inválida retorna 400; cartão inexistente
ou de outro usuário retorna 404. DELETE bem-sucedido retorna 204.

Migration `AddCreditCards` cria tabela, FK, índice e constraints. Aplicação segue
mesmo workflow de migrations local/Docker documentado acima. Testes usam
Testcontainers e banco isolado por classe. Decisões em
[010 — Cartões de crédito](docs/decisions/010-credit-cards.md).

Fase cobre backend. Compras e parcelas estão na fase 15 abaixo; faturas e
limite disponível, na fase 16. Interface de cartões pendente.

### Compras parceladas — fase 15

Endpoints autenticados:

```text
POST /credit-cards/{id}/purchases
GET  /credit-cards/{id}/purchases
```

Exemplo de POST (gere chave nova para cada compra; preserve-a em reenvios):

```json
{
  "description": "Notebook",
  "amount": 1200.00,
  "installmentCount": 4,
  "purchaseDate": "2026-01-24",
  "idempotencyKey": "9de59d5c-ae9f-49b7-ae42-4a9956d7c166"
}
```

Retorna compra com `id`, `creditCardId`, descrição, valor, quantidade, data e
`installments`: cada parcela possui `id`, `number`, `amount`, `closingDate` e
`dueDate` e `invoiceId`. Para fechamento 25 e vencimento 5, exemplo gera quatro parcelas de
R$300, vencendo em 05/02, 05/03, 05/04 e 05/05. Compra em 25/01 começa em 05/03.

Valores positivos, até duas casas decimais; 1–120 parcelas com pelo menos um
centavo cada. Centavos restantes são distribuídos nas primeiras parcelas.
Dias inexistentes ajustam ao último dia do mês. Datas existentes permanecem
quando cartão muda. Detalhes em
[011 — Compras parceladas](docs/decisions/011-installment-purchases.md).

Primeiro POST retorna 201; reenvio idêntico com mesma chave retorna 200; chave
reutilizada com outro conteúdo retorna 409. Validação retorna 400; cartão
inexistente ou de outro usuário retorna 404. Cartão com compras não pode ser
excluído (409). GET retorna compras e parcelas persistidas do cartão.

Migration `AddPurchasesAndInstallments` usa workflow de migrations existente.
Testes PostgreSQL incluem reenvios concorrentes. Faturas e cálculo de limite
disponível implementados na fase 16 abaixo. Interface de compras pendente.

### Faturas — fase 16

Endpoints autenticados:

```text
GET /credit-cards/{id}/invoices
GET /credit-cards/{id}/invoices/{invoiceId}
GET /credit-cards/{id}/invoices/summary?asOf=2026-01-24
```

Fatura retorna `id`, `creditCardId`, `closingDate`, `dueDate`, `total` e `items`.
Cada item identifica parcela/compra, descrição, número, quantidade e valor.
Parcelas no mesmo cartão com as mesmas datas compartilham fatura persistida;
respostas de compras agora incluem `invoiceId` em cada parcela.

Resumo retorna `asOf`, `limit`, `usedLimit`, `availableLimit`, `currentInvoice`
e `futureInvoices`. Data opcional controla classificação dos ciclos; padrão é
data UTC. No fechamento, ciclo atual avança. Sem compras no ciclo atual,
resposta projeta fatura vazia (`id: null`) sem gravar no banco. Listagem completa
inclui faturas históricas. Mudança de calendário preserva datas existentes.

Limite utilizado soma todas as parcelas registradas. Disponível é limite menos
esse total, podendo ser negativo. Fatura fechada/vencida continua consumindo
limite: pagamento/quitação ainda não implementados. Compras não são bloqueadas
por excesso nesta fase. `asOf` não recalcula histórico de compras.

Migration `AddCreditCardInvoices` migra parcelas existentes preservando IDs,
valores e datas. Use workflow de migrations local/Docker já documentado.
Testes PostgreSQL validam atualização com dados da fase 15 e concorrência.
Detalhes em [012 — Faturas](docs/decisions/012-credit-card-invoices.md).
Interface de faturas, pagamentos e integração com saldos permanecem pendentes.

### Transações recorrentes — fase 17

Endpoints autenticados:

```text
POST /recurring-transactions
GET  /recurring-transactions
GET  /recurring-transactions/{id}
POST /recurring-transactions/{id}/deactivate
POST /recurring-transactions/generate
GET  /transactions
```

Cadastro mensal (substitua `accountId` por conta do usuário autenticado):

```json
{
  "accountId": "9de59d5c-ae9f-49b7-ae42-4a9956d7c166",
  "categoryId": null,
  "description": "Netflix",
  "amount": 59.90,
  "type": "Expense",
  "startDate": "2026-01-31",
  "endDate": null
}
```

`type` aceita `Income`/`Expense`; frequência é `Monthly`. Valor positivo com
até duas casas decimais; descrição até 200 caracteres. Conta e categoria
opcional devem pertencer ao usuário. POST retorna 201 e recurso com
`frequency`, `isActive` e `nextOccurrenceDate`. Referência inacessível retorna
404; entrada inválida, 400.

Geração periódica inicial é explícita:

```http
POST /recurring-transactions/generate
Content-Type: application/json

{"throughDate":"2026-03-31"}
```

Exemplo gera 31/01, 28/02 e 31/03, preservando dia original nos meses seguintes.
`endDate` é inclusiva. Data de geração não pode ser futura; `throughDate: null`
ou corpo `{}` usam hoje em UTC. Resposta contém `throughDate`, `generatedCount`
e `hasMore`. Cada chamada recupera até 120 ocorrências por recorrência; repita
se `hasMore` for verdadeiro. Reenvios não duplicam ocorrências já persistidas.

Desativação usa POST sem corpo, retorna 200 e preserva histórico. Conta com
recorrências/lançamentos retorna 409 ao excluir. Templates não possuem edição
ou reativação; alterações exigem desativar anterior e cadastrar novo.

`GET /transactions` lista receitas/despesas geradas, com IDs de conta,
categoria, recorrência e data. Não executa geração. CRUD manual, transferências,
saldo incremental e interface de recorrências permanecem pendentes.
Jobs automáticos disponíveis na fase 22 abaixo; geração manual continua disponível.

Migration `AddRecurringTransactions` cria templates, lançamentos, FKs,
constraints e índice único por recorrência/data. Testes PostgreSQL cobrem
concorrência e recuperação de meses pendentes. Decisões em
[013 — Recorrências](docs/decisions/013-recurring-transactions.md).

### Dashboard — fase 18

`GET /dashboard/summary?asOf=2026-10-13` retorna resumo autenticado por usuário:

- `currentBalance`, `incomeThisMonth`, `expensesThisMonth`;
- `creditCardInvoicesThisMonth` e faturas detalhadas em `invoices`;
- `expensesByCategory`, incluindo despesas sem categoria;
- `monthlyHistory`, com 12 meses e zeros nos meses sem movimento;
- `forecast`, com próximos três meses completos, e `futureExpenses` total.

Data opcional usa hoje em UTC. Saldo considera receitas menos despesas até data
selecionada, sem saldo inicial. Despesas das contas e faturas são separadas;
faturas exibidas vencem no mês selecionado. Previsão inclui parcelas,
lançamentos futuros conhecidos e recorrências ativas ainda não geradas,
respeitando término e evitando duplicação. `asOf` não reconstrói alterações
históricas de templates ou pagamentos inexistentes.

`/dashboard` agora consome API e exibe indicadores, filtro de data, gráficos
ECharts, histórico em tabela, categorias, faturas e previsão. Estados vazios,
carregamento e erro são tratados sem dados fictícios. Validação de sessão e
cookies HttpOnly seguem fluxo existente. Acesso sem autenticação retorna 401
na API; frontend redireciona ao login.

Sem migration adicional. Testes validam totais decimais, cortes de data,
previsões e isolamento usando PostgreSQL. Decisão em
[014 — Dashboard](docs/decisions/014-dashboard-read-model.md). Implementação
dos gráficos segue [documentação ECharts](https://echarts.apache.org/handbook/en/concepts/chart-size/).

Validação da fase 18: 149 testes backend (sem skips), 7 frontend, lint,
typecheck e build com `npm run build -- --webpack` passaram. Build Turbopack
esbarrou em restrição de sockets deste ambiente. Smoke HTTP confirmou
dashboard autenticado com dados reais da API isolada, recuperação de data
inválida e redirecionamento sem sessão. Revisão visual/interação dos gráficos
no navegador pendente: nenhum navegador disponível na sessão.

### Testcontainers — fase 19

```bash
make test-api          # Toda suíte backend, com Docker
make test-integration  # Somente integração, com Docker
make test-fast         # Unitários e arquitetura, sem Docker
```

Validação: 150 testes backend passaram, incluindo 48 de integração, sem skips
e sem FINTRACK_TEST_DATABASE. Containers temporários removidos automaticamente.
Novo teste verifica isolamento físico e encerramento independente de duas
fixtures. Não há banco externo ou porta fixa para configurar.

POST manual `/transactions` implementado na fase 20; transferências ainda
dependem da fase 6. Transações geradas por recorrências já são testadas. Decisão em
[015 — Testcontainers](docs/decisions/015-testcontainers-integration.md), baseada
no [módulo oficial PostgreSQL](https://dotnet.testcontainers.org/modules/postgres/).

### Playwright E2E — fase 20

Instale dependências do frontend e Chromium oficial:

```bash
cd apps/web
npm ci
npx playwright install chromium
npm run e2e
```

Em Linux sem dependências nativas do navegador, use
`npx playwright install --with-deps chromium` conforme ambiente. Docker e SDK
.NET 10 precisam estar disponíveis. Na raiz, `make test-e2e` executa mesmo runner.

Runner cria banco PostgreSQL temporário sem volume, aplica migrations, builda
API e Next.js com Webpack em `.next-e2e`, inicia servidores em portas aleatórias
e roda Chromium desktop/mobile. Não depende de Compose, `.env` pessoal ou banco
externo. Processos e container são encerrados também quando testes falham.
Config rejeita execução direta sem ambiente isolado; prefira `npm run e2e`.

Cada cenário cadastra usuário próprio pelo navegador. Fluxos cobrem:

- Cadastro, login inválido/válido, logout, conta, receita/despesa e dashboard;
- Cartão, compra parcelada e datas/valores de faturas;
- Sessão obrigatória, isolamento entre usuários e filtro de data inválida.

Sem mocks ou retries. Gráficos SVG são verificados, histórico possui alternativa
acessível, e fluxo principal testa overflow no mobile. Falhas retêm trace,
screenshot e vídeo em `test-results`; relatório HTML em `playwright-report`:

```bash
npm run e2e:report
```

Artefatos são ignorados no Git e contêm somente sessão/dados sintéticos da
stack de teste. Playwright não reutiliza sessão pessoal do navegador.

Para permitir fluxos completos, `/accounts` ganhou criação; `/transactions`
possui formulário/listagem e POST autenticado de receitas/despesas manuais;
`/credit-cards` cria cartões/compras e mostra faturas. Migration
`AllowManualTransactions` permite referência de recorrência nula e preserva
lançamentos gerados. Data manual não pode ser futura; conta/categoria devem
pertencer ao usuário. Transferências, edição/exclusão e pagamentos não fazem
parte desta entrega. Login preserva e-mail após erro de credenciais.

Validação: **6 E2E** desktop/mobile, **152 backend** (50 integração), **8 frontend**,
lint, typecheck e build Webpack passaram. Cleanup de containers confirmado.
Decisão em [016 — Playwright E2E](docs/decisions/016-playwright-e2e.md), com
[configuração oficial Playwright](https://playwright.dev/docs/test-configuration).

### Redis — fase 21

Compose agora inicia `web`, `api`, `postgres`, `redis` e migrador provisório.
Redis usa `redis:7-alpine`, healthcheck, memória limitada a 128 MB e porta de
host em loopback. Cache não possui volume/persistência; PostgreSQL conserva dados.

```bash
make full
# Desenvolvimento: banco/cache Docker; API/web locais
make dev-db
make dev-migrate
make dev-api
make dev-web
```

`REDIS_PORT` em `.env` controla porta do Compose (padrão 6379). Se mudar,
alinhe `make REDIS_PORT=6380 dev-api` com valor configurado. TTL padrão 60s,
configurável por `DASHBOARD_CACHE_TTL_SECONDS` no Compose (1–3600 segundos).

API Docker usa `ConnectionStrings__Redis=redis:6379` e cache habilitado.
`make dev-api` usa endereço local e habilita cache. Execução direta de
`dotnet run` mantém cache desabilitado por padrão; para habilitar manualmente:

```bash
ConnectionStrings__Redis=127.0.0.1:6379 DashboardCache__Enabled=true \
  dotnet run --project apps/api/src/FinTrack.Api --launch-profile http
```

Cache-aside de `/dashboard/summary`: hit retorna DTO Redis; miss executa
agregações PostgreSQL e armazena resultado com TTL absoluto. Chave separa
usuário, data e revisão. Hit ainda consulta revisão por índice em PostgreSQL;
autenticação e regras financeiras permanecem no banco.

Migration `AddDashboardCacheRevisions` cria revisão por usuário. Escritas
financeiras a incrementam na mesma transação: contas, categorias, cartões,
compras/faturas, recorrências e lançamentos. Todas as datas desse usuário são
invalidadas por nova revisão; chaves antigas expiram. Consulta concorrente
não consegue publicar snapshot antigo na chave da nova revisão. Invalidação
continua funcionando quando Redis está indisponível, sem impedir escritas.

Redis offline, timeout ou JSON inválido produzem fallback ao PostgreSQL.
Falha ao salvar cache não altera resposta. Logs Debug registram hit/miss;
warnings registram fallback. Contrato HTTP e interface do dashboard não mudam.

Integração usa Redis Testcontainers próprio para hits, TTL, invalidação,
outage, timeout e corrida entre leitura/commit. E2E também inicia Redis isolado
e prima cache antes de criar lançamentos. Decisão em
[017 — Redis](docs/decisions/017-redis-dashboard-cache.md), baseada no
[cliente oficial documentado pela Redis](https://redis.io/docs/latest/develop/clients/dotnet/).

Validação da fase 21: **161 testes backend**, incluindo 55 de integração,
sem skips; **6 E2E** desktop/mobile com Redis ativo; build Webpack, lint,
configuração Compose e alinhamento de migrations validados. Containers de
teste removidos automaticamente.

### Background jobs — fase 22

API executa worker de recorrências quando habilitado. Compose e `make dev-api`
ativam processamento; execução direta de `dotnet run` deixa worker desativado
por padrão. Não executa jobs em `--migrate`.

```bash
make full
# Local, após preparar banco/cache e migrations:
make dev-api
```

Em `.env`, `BACKGROUND_JOBS_ENABLED` controla Compose e
`BACKGROUND_JOBS_INTERVAL_SECONDS` define intervalo (padrão 60 segundos).
Para habilitar manualmente na API local:

```bash
BackgroundJobs__Enabled=true BackgroundJobs__IntervalSeconds=60 \
  dotnet run --project apps/api/src/FinTrack.Api --launch-profile http
```

Um ciclo inicia com API e repete após intervalo de conclusão. Descobre usuários
com recorrências pendentes, gera até hoje em UTC e preserva isolamento. Cada
ciclo processa até 100 usuários por padrão; cada recorrência gera até 120
ocorrências por lote. Backlog continua no próximo ciclo. Datas iniciais futuras
aguardam chegada do dia; término e regras mensais da fase 17 permanecem iguais.

Configuração `BackgroundJobs` também aceita `MaxUsersPerCycle` (1–1000),
`MaxAttempts` (1–5) e `RetryDelayMilliseconds` (50–10000). Timeout/falha transitória
recebe retry exponencial com DbContext novo; erro permanente não é repetido no
mesmo ciclo. Falhas são registradas e demais usuários selecionados continuam.
Geração manual e múltiplas instâncias compartilham lock/checkpoint, sem duplicar
ocorrências. Encerramento cancela consultas, locks e atrasos cooperativamente.

Geração atualiza revisão do cache na mesma transação; próxima leitura do
dashboard mostra novos lançamentos. Redis indisponível não impede worker.
Faturas já são criadas com compras e resumo permanece projeção/cache: não há
jobs redundantes para gerar faturas vazias ou armazenar totais duplicados.

Nenhuma migration adicional. Testes normais/E2E mantêm worker desativado;
cobertura específica inicia HostedService real com PostgreSQL/Redis isolados.
Decisão em [018 — Background jobs](docs/decisions/018-recurring-background-jobs.md),
baseada na [documentação de HostedService](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/host/hosted-services?view=aspnetcore-10.0).


Validação da fase 22 (escopo solicitado por AGENTS.md): 9 unitários dos jobs e
16 integrações de jobs/recorrências/cache passaram, sem skips. Concorrência e
rollback após escrita parcial revalidados isoladamente. Build API e configuração
Compose validados; suíte completa e E2E não foram executados nesta fase.

### Messaging — fase 23

`TransactionCreated` v1 alimenta Analytics Consumer e Notification Consumer via
RabbitMQ. Transações manuais e recorrentes gravam evento na mesma transação do
banco (outbox), mesmo com broker offline. Eventos de transações anteriores à
migration não são recriados. Aplicar migrations antes de iniciar API atualizada.

Configure `RABBITMQ_PASSWORD` no `.env` com senha local própria, alfanumérica;
`make full` inicia RabbitMQ e habilita `Messaging`. `make dev-db` inicia também
broker. Management UI: `http://localhost:15672`, usuário `RABBITMQ_USER` e senha
configurada. Portas AMQP/management são publicadas somente no loopback.
Dados RabbitMQ persistem em volume; `make stop` preserva filas.

API local mantém mensageria desativada por padrão. Para habilitar, exporte
`Messaging__Enabled=true` e `ConnectionStrings__RabbitMq` com URI
`amqp://usuario:senha@127.0.0.1:5672/` antes de `make dev-api`. Credenciais com
caracteres especiais precisam de percent-encoding na URI. Compose controla
ativação por `MESSAGING_ENABLED`; migration não inicia mensageria.

Exchange fanout `fintrack.transaction-created.v1` publica nas filas quorum
`fintrack.analytics.v1` e `fintrack.notifications.v1`. Mensagens persistentes,
confirmações de publicação, ack após escrita e deduplicação no PostgreSQL
protegem entrega. Falhas recebem três tentativas por entrega; payload inválido
vai diretamente à DLQ `<fila>.dead`. DLQs usam dead-lettering at-least-once.
Queda de conexão devolve mensagens sem ack; serviço reconecta automaticamente.

Analytics persiste projeção por evento em `transaction_analytics`; Notification
persiste registro local em `transaction_notifications`. Não há email/push nem
nova interface pública nesta fase. Dashboard permanece baseado em transações.
Projeções são eventualmente consistentes; broker offline acumula outbox sem
bloquear API. Publicador processa 50 eventos por ciclo; consumidores até 50 por
fila; intervalo normal de um segundo, reconexão após dois segundos.

DLQ permite investigar e republicar manualmente após corrigir causa. Replay e
limpeza de outbox não são automáticos. Detalhes e limitações em
[019 — Transaction messaging](docs/decisions/019-transaction-messaging.md).
Testes comuns/E2E desativam worker; integração específica usa RabbitMQ e
PostgreSQL reais, isolados por Testcontainers.

Validação da fase 23: 5 unitários de configuração/registro e 21 integrações
relacionadas passaram (8 de mensageria, 13 de transações/recorrências/jobs).
Cobertura inclui fanout, duplicatas, rollback, falha de publicação, DLQ,
falha de consumidor, ciclo/encerramento do worker, publishers concorrentes e
SaveChanges síncrono. Build API e Compose validados; suíte completa/E2E não
executados nesta fase, conforme AGENTS.md.

### Observabilidade local — fase 24

`make full` inicia Collector OTLP, Jaeger e Prometheus. API exporta tracing e
métricas automaticamente. Para API local:

```bash
make dev-observability
export Observability__OtlpEndpoint=http://127.0.0.1:4317
make dev-api
```

Aplicar migrations antes de iniciar API atualizada: outbox agora persiste contexto
W3C. `make full` aplica migration automaticamente. Serviço `--migrate` não
inicia observabilidade. Configure `.env` conforme `.env.example`, inclusive
`RABBITMQ_PASSWORD` para validação do Compose.

- Jaeger: `http://localhost:16686`, serviço `fintrack-api`.
- Prometheus: `http://localhost:9090`.
- Logs JSON: `docker compose logs -f api`.

Resposta HTTP contém `X-Correlation-ID` com trace ID; procure esse ID no Jaeger
ou campo `TraceId` dos scopes dos logs. Cliente pode enviar `traceparent` W3C
válido. Transação salva contexto na outbox; publicação posterior e ambos os
consumidores mantêm trace, com spans PostgreSQL associados. Jobs criam trace
por ciclo. Endpoint/headers/corpo completos e SQL não são exportados por tracing;
logs próprios usam templates de rota e campos operacionais.

Consultas Prometheus úteis:

```promql
rate(fintrack_messaging_published_total[5m])
rate(fintrack_messaging_consumed_total[5m])
fintrack_messaging_dead_letters_total
fintrack_recurring_generated_total
fintrack_recurring_cycles_total
```

Contadores de publicação/consumo medem confirmações/entregas, incluindo duplicatas;
não substituem contagem de transações financeiras. Falhas por tentativa ficam em
`fintrack_messaging_failures_total`. Métricas HTTP e runtime .NET também ficam
disponíveis. Exportação ocorre periodicamente; aguarde até cerca de um minuto
para primeira série após iniciar API.

`OBSERVABILITY_ENABLED=false` desativa SDK no Compose; API local usa
`Observability__Enabled=false`. Sem `Observability__OtlpEndpoint`, SDK registra
instrumentação sem exportação externa. Collector offline não bloqueia escritas;
telemetria pode ser perdida. Jaeger mantém traces em memória, Prometheus retém
sete dias em volume. Portas OTLP/UIs apenas no loopback. Logs permanecem no
console; tracing de frontend, alertas e AWS não incluídos.

Decisão em [020 — Observabilidade local](docs/decisions/020-local-observability.md).

Validação da fase 24: 6 unitários e 16 integrações relacionadas passaram,
sem skips. Trace HTTP/PostgreSQL/outbox/producer/ambos os consumidores verificado;
configuração real do Collector confirmou trace no Jaeger e counter no Prometheus
via OTLP. Stack temporário removido. Build API e Compose validados; suíte completa
e E2E não executados, conforme AGENTS.md.

## Fase 25 — CI sem cobrança

[Workflow](.github/workflows/ci.yml) executa lint, typecheck, testes frontend,
builds Next.js/.NET, testes unitários/arquitetura/integração e E2E Chromium
desktop/mobile. Usa Node.js 24, SDK .NET 10 e runners padrão `ubuntu-24.04`.
Integrações criam containers locais ao runner, sem serviços externos.

Todos os jobs são ignorados em repositórios privados antes de alocar runner.
Runners padrão públicos são gratuitos conforme [documentação do GitHub](https://docs.github.com/en/actions/reference/runners/github-hosted-runners).
Sem caches remotos, uploads de artefatos, credenciais AWS ou deploy. Resultados
ficam nos logs; execuções antigas da mesma referência são canceladas. Workflow
fica preparado para push, pull request e execução manual após publicação pelo
proprietário; esta fase não publica código nem altera visibilidade/conta.

Para executar verificações equivalentes localmente, inclusive em projeto privado:

```bash
make ci-install
make ci-install-e2e
make ci
```

Pré-requisitos: GNU Make, Node.js 24, SDK .NET 10, Docker ativo e acesso aos
registries para baixar pacotes/imagens. Instalação das dependências de sistema
do Chromium pode solicitar sudo. Testes usam dados descartáveis e não dependem
do `.env` real. `make ci-web`, `make ci-api` e `make ci-e2e` executam partes
isoladas; não rode frontend e E2E simultaneamente no mesmo checkout, pois ambos
geram `next-env.d.ts`. `make ci` executa etapas sequencialmente e para na primeira
falha. Relatório HTML continua disponível no E2E local sem `CI=true`.

Decisão: [021 — CI público sem cobrança](docs/decisions/021-free-public-ci.md).

Validação local da fase 25: lint/typecheck, builds frontend/backend e actionlint
aprovados; 3 testes frontend, 120 unitários, 4 de arquitetura, 71 integrações e
6 E2E aprovados, sem skips. Execução no GitHub ainda não realizada.

## Fase 26 — Imagens Docker locais

API e frontend têm imagens multi-stage, usuários não root e healthchecks.
Frontend usa saída standalone e build webpack, como CI. Segredos chegam por
variáveis em runtime; `.env`, chaves/certificados privados, logs e artefatos
E2E ficam fora dos contextos. Não incluir dados pessoais em `public` ou
segredos em appsettings. Nenhuma imagem foi publicada no GHCR ou outro registry.

Build na raiz, com Docker ativo:

```bash
make images-build IMAGE_TAG=phase26
docker image ls fintrack-api
docker image ls fintrack-web
```

Cria `fintrack-api:phase26` e `fintrack-web:phase26`. Sem `IMAGE_TAG`, padrão
é `local`, compatível com comandos anteriores. Use tags novas para preservar
snapshots; reconstruir mesma tag substitui referência. Build baixa imagens
base e pacotes, mas não requer `.env`, banco ativo ou credenciais reais. Bases
e dependências podem evoluir: tag local não garante reprodução byte a byte.

Para executar imagens já construídas no Compose, configure `.env` conforme
instruções existentes, incluindo senha do banco, RabbitMQ e chave JWT. Todas
as imagens de dependências precisam estar disponíveis localmente. Se ainda
não estiverem, baixe-as uma vez:

```bash
docker compose pull postgres redis rabbitmq otel-collector jaeger prometheus
make images-run IMAGE_TAG=phase26
make status
```

`images-run` usa `--no-build --pull never`: não compila nem busca imagens
ausentes. API e migrations usam mesma tag; frontend usa tag correspondente.
Para selecionar versão diretamente pelo Compose, defina `IMAGE_TAG` no `.env`
ou execute `IMAGE_TAG=phase26 docker compose up --no-build --pull never -d --wait`.
`make full IMAGE_TAG=phase26` permanece alternativa para build e execução juntos.
`make stop` encerra stack preservando volumes.

Transporte opcional sem registry:

```bash
docker save -o /tmp/fintrack-phase26.tar fintrack-api:phase26 fintrack-web:phase26
# No computador de destino:
docker load -i /tmp/fintrack-phase26.tar
```

Arquivo contém imagens da aplicação; dependências Docker precisam ser obtidas
separadamente. Não inclui volumes, banco ou `.env`; não adicionar esses dados
ao pacote. Imagens são construídas para arquitetura do computador de origem.
Decisão: [022 — Imagens Docker locais](docs/decisions/022-local-container-images.md).

Validação da fase 26: builds Docker concluídos; tags Compose validadas. Smoke
com PostgreSQL descartável aplicou migrations, registrou usuário de teste,
consultou endpoint autenticado e confirmou página de login e ambos os
healthchecks. Imagens verificadas com usuários não root e sem `.env`, chaves
privadas, logs ou artefatos E2E em `/app`. Containers/rede temporários removidos;
imagens locais preservadas. Nenhuma suíte não relacionada foi executada.
