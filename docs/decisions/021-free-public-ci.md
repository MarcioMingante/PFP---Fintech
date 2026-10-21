# 021 — CI público sem cobrança

## Contexto

Verificações precisam ser reproduzíveis localmente e preparadas para CI público,
respeitando restrição de não assumir cobrança ou publicar repositório.

## Decisão

GitHub Actions executa frontend, backend e E2E em jobs separados com runners
padrão `ubuntu-24.04`. Todos os jobs verificam
`github.event.repository.private == false` antes de alocar runner. Repositórios
privados executam as mesmas verificações localmente com `make ci`; não há opção
automática de consumir minutos pagos nem alteração da visibilidade do projeto.

Workflow atende push, pull_request e execução manual. Token tem somente
`contents: read`; checkout não persiste credenciais. Actions oficiais ficam
fixadas por SHA. Execuções antigas da mesma referência são canceladas e jobs
têm timeout. Não usamos pull_request_target, segredos, AWS, deploy, registry,
cache remoto ou upload de artefatos. Logs mostram resultados; Playwright usa
reporter list no CI. Artefatos locais de falhas E2E ficam no runner descartável.

Node.js 24 e .NET 10 acompanham ambiente local. Frontend instala pelo lockfile,
verifica ESLint, tipos e testes Node, depois build Next.js com webpack, como
runner E2E existente. Backend restaura, compila Release e executa testes
unitários, de arquitetura e integração. Testcontainers cria PostgreSQL, Redis
e RabbitMQ locais ao runner; E2E usa sua stack isolada PostgreSQL/Redis e
Chromium desktop/mobile. Não precisamos de banco externo ou credenciais reais.

Makefile compartilha comandos com CI e executa etapas sequencialmente para
evitar concorrência sobre `next-env.d.ts`. Instalações são explícitas (`ci-install` e
`ci-install-e2e`); `ci` valida checkout já preparado. Não executar `ci-web` e
`ci-e2e` simultaneamente no mesmo checkout.

## Alternativas

CI somente local dispensaria automação remota; permanece caminho válido para
repositório privado. Runner próprio exigiria manutenção e acesso a uma máquina.
Runners maiores e serviços pagos não atendem restrição do projeto.

## Consequências

Pipeline preparado não comprova execução remota. Repositório privado ignora
jobs; operador usa Makefile local. Ausência de artefatos remotos limita inspeção
pós-execução aos logs. Mudanças em política de gratuidade exigem nova revisão.

## Limites

Gratuidade depende das condições do GitHub para runners padrão em repositórios
públicos. Revisar documentação antes de trocar runner, visibilidade ou adicionar
armazenamento. Publicar código e habilitar Actions são decisões do proprietário;
esta entrega prepara arquivos locais, sem push ou alteração da conta.

## Referências

- [Runners padrão e gratuidade pública](https://docs.github.com/en/actions/reference/runners/github-hosted-runners)
- [setup-node e controle de cache](https://github.com/actions/setup-node)
- [setup-dotnet](https://github.com/actions/setup-dotnet)
