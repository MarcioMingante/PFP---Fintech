# 023 — Entrega funcional local

## Decisão

Docker Compose é entrada única do ambiente completo: frontend, API, migrations,
PostgreSQL, Redis, RabbitMQ, Collector, Jaeger e Prometheus. Não há cloud,
registry obrigatório, conta AWS ou deploy remoto. README começa com instalação,
configuração, inicialização, endereços, diagnóstico e persistência.

`make setup` gera segredos aleatórios via OpenSSL e grava `.env` com modo 600,
sem sobrescrever arquivo existente. Não carrega `.env` como código shell.
`config-check` valida Compose sem mostrar configuração resolvida. Variáveis
obrigatórias ausentes impedem inicialização. JWT também valida tamanho na API.
`logs` acompanha execução; `migrate` reaplica apenas migrations pendentes usando
imagem atual. `full`, `images-run` e `stop` mantêm fluxo estabelecido.

Portas publicadas são vinculadas ao loopback: entrega destina-se ao computador
local. Removido nome global fixo do PostgreSQL; nomes de containers/volumes
seguem projeto Compose, permitindo validação isolada sem colisões.

Migrations são executadas antes da API e podem ser repetidas. Healthchecks
verificam serviços existentes, mas `/health` é liveness HTTP, não readiness
de todas as dependências. Observabilidade exige tempo para exportação/coleta.
Não adicionamos readiness nem alteramos comportamento financeiro nesta fase.

Encerramento normal preserva volumes PostgreSQL, RabbitMQ e Prometheus.
Cache Redis e traces Jaeger são voláteis. Mudanças do nome do projeto mudam
volumes selecionados; mudanças de senhas no `.env` não reconfiguram banco
existente. Nenhum alvo Make apaga volumes. Dados reais e segredos permanecem
fora do Git e imagens; backup continua responsabilidade do proprietário.

## Validação

Setup novo e repetido, permissões de `.env` e config Compose. Stack completa
em projeto temporário com portas aleatórias: migrations, autenticação, API/web,
cache, mensageria e observabilidade. Encerrar e reiniciar verifica persistência
do banco; reaplicar migrations verifica idempotência. Remover somente volumes
temporários ao final, preservando ambiente pessoal.
