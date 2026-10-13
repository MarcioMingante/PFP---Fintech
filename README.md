# PFP---Fintech
Personal Finance Platform

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
`${API_PORT:-5168}:8080`. Para manter desenvolvimento com API local:

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
