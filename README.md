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
