# PFP---Fintech
Personal Finance Platform

## Backend local

Pré-requisitos: SDK .NET 10. PostgreSQL em Docker é necessário para operações
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

Para integração PostgreSQL, use uma instância local exclusiva de testes:

```bash
docker run -d --rm --name fintrack-tests \
  -e POSTGRES_USER=fintrack_test \
  -e POSTGRES_PASSWORD=local_test_password \
  -p 127.0.0.1:55432:5432 postgres:17-alpine
docker exec fintrack-tests pg_isready -U fintrack_test
export FINTRACK_TEST_DATABASE='Host=127.0.0.1;Port=55432;Database=postgres;Username=fintrack_test;Password=local_test_password'
dotnet test apps/api/FinTrack.sln -m:1
docker stop fintrack-tests
unset FINTRACK_TEST_DATABASE
```

Espere `pg_isready` indicar disponibilidade antes de executar testes. Fixture
cria banco com nome aleatório, aplica migrations e remove somente esse banco
no encerramento; usuário precisa de permissão para criar bancos. Cada request
usa seu próprio escopo de persistência. Falhas de conexão ou migrations falham
a suíte quando variável está configurada. Sem `FINTRACK_TEST_DATABASE`, testes
PostgreSQL aparecem como ignorados; demais testes continuam executando.
Não configure credenciais do banco pessoal ou de produção.

Transactions e transferências da fase 6 ainda não existem neste repositório.
Seus testes de domínio, saldo e atomicidade deverão acompanhar implementação.
Testcontainers permanece previsto para fase 19.

## Fase 8 — API em Docker

Na raiz, copie `.env.example` para `.env` caso ainda não exista e configure
credenciais locais do PostgreSQL. `API_PORT` é opcional e usa `5168` por padrão.

```bash
docker compose up --build -d --wait
curl --fail http://localhost:5168/health
curl --fail http://localhost:5168/accounts
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

Dockerização cobre módulos atuais de contas e categorias. Transações continuam
pendentes da fase 6.

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
“em breve”, pois módulo backend ainda está pendente da fase 6. Formulários de
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
