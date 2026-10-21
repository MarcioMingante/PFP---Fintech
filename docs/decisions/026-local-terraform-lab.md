# 026 — Exercício Terraform Docker local

## Contexto

Fase 30 introduz prática de providers, resources, modules, state, plan, apply
e destroy sem credenciais cloud ou cobrança de infraestrutura gerenciada.
Stack funcional já tem Docker Compose como definição executável.

## Decisão

Adicionar [laboratório opcional](../../ops/terraform/local/README.md) com provider
`kreuzwerker/docker` fixado em 4.6.0, lockfile versionado e backend local.
Módulo HTTP cria container BusyBox sem dados reais; rede tem namespace próprio,
porta aleatória no loopback e labels de laboratório. Container sem root,
sem volumes, sem capabilities e com escrita limitada a tmpfs descartável.

Não importar ou administrar objetos Compose. Make expõe init, validate, plan,
apply do plano salvo e destroy com confirmação. Remove variáveis de contexto
Docker do processo Terraform e host aceita somente socket Unix local.
Não usar AWS, HCP Terraform, backend remoto, registry privado ou provisioners.

## Alternativas

Gerenciar stack existente também por Terraform criaria dois responsáveis pelos
mesmos recursos. Provider AWS exigiria recursos e credenciais externos.
Usar somente Compose seria suficiente para aplicação, mas não exercitaria
state/plan/apply; laboratório isolado atende objetivo didático.

## Consequências

Exercício demonstra ciclo de IaC sem mudar runtime da aplicação. Init/pull
exigem downloads públicos, não conta cloud. CLI Terraform é dependência opcional.
State, backups, planos, arquivos de variáveis e plugins ficam fora do Git;
lockfile entra. State deve ser preservado até destroy. Imagem pode permanecer
no cache local após remoção para não interferir em outros usos.

Duas cópias com mesmo nome de recursos podem colidir apesar de states separados.
Manter nomes exclusivos e não compartilhar responsabilidade de recursos com
Compose. Isolamento vale para configuração fornecida; alterar host/provider,
backend ou importar recursos muda escopo e requer decisão própria.

## Validação

Formatar/validar Terraform; plan contém somente imagem, rede e container Docker.
Apply cria servidor HTTP; plan seguinte sem alterações comprova convergência.
Host remoto é rejeitado. Destroy encerra laboratório sem remover containers
existentes. Não executar suíte da aplicação, pois não altera código financeiro.
