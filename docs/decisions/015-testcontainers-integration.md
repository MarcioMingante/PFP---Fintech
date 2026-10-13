# 015 — PostgreSQL efêmero com Testcontainers

## Contexto

Integração dependia de PostgreSQL iniciado manualmente, porta fixa e variável
FINTRACK_TEST_DATABASE. Sem variável, cenários eram ignorados. Fase 19 exige
ambiente reproduzível com inicialização/limpeza automática e isolamento.

## Decisão

Fixture xUnit v2 usa módulo oficial `Testcontainers.PostgreSql` 4.16.0 e imagem
`postgres:17-alpine`, compatível com banco já usado no projeto. Container novo
por classe tem porta escolhida automaticamente e senha efêmera. Sem volumes
persistentes, nomes fixos, reuse ou banco externo. Docker precisa estar ativo.

StartAsync usa prontidão do módulo e limite de dois minutos para startup.
Depois, WebApplicationFactory recebe connection string do container, configura
JWT de teste e aplica migrations reais, mantendo testes de upgrade com contas
legadas e compras/parcelas anteriores à criação de faturas.

Dispose encerra factory antes de container. Bloco finally garante tentativa de
remover container mesmo se API falhar no encerramento; inicialização parcial
também chama limpeza. Dispose é idempotente. Resource Reaper padrão permanece
ativo para limpeza complementar em interrupções do processo.

Integração usa Fact/Theory normais: Docker indisponível ou migrations inválidas
falham testes. FINTRACK_TEST_DATABASE é ignorada. Testes unitários/arquitetura e
health sem banco continuam independentes de PostgreSQL.

### Isolamento e reset

Banco físico é novo por classe/execução. Métodos compartilham somente ambiente
e seed de migração; cada cenário financeiro cria usuário novo e queries usam
filtros de propriedade. Não há truncamento concorrente de tabelas. Teste dedicado
com duas fixtures ignora filtros e prova isolamento físico; após encerrar uma,
outra continua gravando. Classes podem executar em paralelo com portas distintas.

## Alternativas

- Banco externo opcional: removido para eliminar configuração manual e skips.
- Container por método: custo de startup maior sem benefício para cenários
  atualmente isolados por usuário.
- Container global e truncate: rejeitado por interferir em cenários concorrentes.

## Consequências

Primeira execução pode baixar PostgreSQL e imagem do Resource Reaper. Depois,
`dotnet test apps/api/FinTrack.sln -m:1` gerencia dependências. `make test-fast`
permite unitários/arquitetura sem Docker. CI será criada na fase 25.

Cobertura inclui accounts, categories, autenticação, compras, faturas,
recorrências, transações geradas e dashboard. POST manual /transactions e
transferências continuam pendentes da fase 6; não há endpoints fictícios para
cumprir lista do roadmap.
