# Terraform Docker local — Fase 30

Exercício opcional de IaC, separado da aplicação. Docker Compose continua único
gerenciador da stack FinTrack. Este laboratório cria apenas rede própria e
servidor BusyBox HTTP descartável, sem volumes, banco, senhas ou dados pessoais.
Não usa AWS, HCP Terraform, backend remoto, GHCR ou registry privado.

## Pré-requisitos

Terraform CLI >= 1.6 e < 2, Docker Engine/Docker Desktop com containers Linux,
GNU Make e acesso ao socket Unix local. Linux/WSL2 são ambiente de referência.
Instale CLI pela [distribuição oficial](https://developer.hashicorp.com/terraform/install).
Não é necessário `terraform login`, conta cloud ou configurar `.env` da aplicação.
`init` baixa provider; primeiro `apply` pode baixar imagem pública BusyBox.
Esses downloads não provisionam serviços externos.

Na raiz do repositório:

```bash
make iac-init
make iac-validate
make iac-plan
make iac-apply
env -u DOCKER_CONTEXT -u DOCKER_HOST terraform -chdir=ops/terraform/local output -raw url
```

Abra URL exibida: resposta `FinTrack Terraform local lab`. Porta do host é
aleatória e vinculada a `127.0.0.1`; container usa porta 8080. Imagem BusyBox
1.37.0, usuário 65534, filesystem somente leitura e `/tmp` em memória.
Rede bridge própria não conecta aplicação ou banco pessoal; acesso HTTP do
host fica somente no loopback. Não há bloqueio adicional de saída do container.

`iac-plan` grava `local.tfplan` para revisão. Plano novo deve conter apenas
`docker_network.lab`, `module.http_demo.docker_image.busybox` e
`module.http_demo.docker_container.http`. `iac-apply` executa **plano salvo**,
sem segunda pergunta de confirmação; revise-o antes. Alterou configuração?
Gere novo plano. Depois de aplicar, novo `plan` deve mostrar ausência de mudanças.

Encerrar laboratório, revisando confirmação de remoção de seus recursos:

```bash
make iac-destroy
```

Remove container e rede do exercício. `keep_locally=true` preserva imagem no
cache Docker, evitando remover imagem que outro exercício possa usar. Não
remove containers/volumes Compose. `make stop` não encerra laboratório Terraform.

## Providers, resources, modules e state

| Conceito | Aplicação neste exercício |
| --- | --- |
| Provider | `kreuzwerker/docker` 4.6.0 comunica com Docker API local; versão/checksums no lockfile |
| Resource | Rede, imagem e container declarados; referências estabelecem ordem de dependências |
| Module | `modules/http-demo` agrupa imagem/container e expõe nome/URL; provider herdado do módulo raiz |
| State | `terraform.tfstate` local vincula recursos declarados a IDs Docker; não editar manualmente |
| Plan | Compara configuração, state e recursos atuais; mostra alterações sem aplicá-las |
| Apply | Executa plano aprovado e atualiza state |
| Destroy | Planeja e remove somente objetos registrados no state do laboratório |

Backend local é explícito. `.terraform.lock.hcl` entra no Git; `.terraform/`,
state/backups, planos e `.tfvars` não entram. State e planos podem conter dados
sensíveis em outros projetos: não publicar ou enviar como artefatos. Mesmo
neste exemplo sem segredos, preservar state até concluir `destroy`; apagar
state antes deixa recursos órfãos. Lock local protege operações sobre mesmo
state, não coordena diferentes cópias do exercício.
[State](https://developer.hashicorp.com/terraform/language/state) e
[backend local](https://developer.hashicorp.com/terraform/language/backend/local).

## Isolamento e personalização

Nomes sempre começam por `fintrack-iac-`; `lab_name` aceita sufixo de 1 a 32
caracteres minúsculos/números/hífens. Labels identificam `terraform-local-lab`.
Não importar containers/volumes Compose, apontar module para recursos reais ou
gerenciar mesmo objeto por Compose e Terraform. Duas cópias do laboratório
precisam de `lab_name` diferentes para evitar colisões; compartilhar nome não
compartilha state.

Exemplo de sufixo próprio (usar mesma configuração durante ciclo):

```bash
export TF_VAR_lab_name=estudo
make iac-plan
make iac-apply
make iac-destroy
unset TF_VAR_lab_name
```

Provider aponta explicitamente para `unix:///var/run/docker.sock`; variável
`docker_host` aceita somente sockets Unix. Alvos Make removem `DOCKER_CONTEXT`
e `DOCKER_HOST` do processo Terraform para impedir uso acidental de contexto
remoto. Use mesmos alvos e mesmo socket durante plan/apply/destroy. Terraform
direto com `DOCKER_CONTEXT` pode sobrescrever host; não use contexto remoto.

Docker rootless/Desktop com socket diferente:

```bash
export TF_VAR_docker_host="unix:///CAMINHO/ABSOLUTO/docker.sock"
make iac-init
make iac-plan
make iac-apply
make iac-destroy
unset TF_VAR_docker_host
```

Use caminho real do socket local. Erro de permissão: ajuste acesso ao Docker;
não substitua host por daemon remoto. Terraform não seleciona automaticamente
contexto do Docker CLI. Binário alternativo local pode ser informado via
`make iac-init TERRAFORM=/caminho/terraform`; mesmo para outros alvos.

Não existem provisioners, execução shell no host ou provider AWS. Comando shell
do resource cria somente página fixa dentro do container. Arquitetura AWS da
Fase 28 permanece proposta documental.

## Referências

- [Provider Docker](https://registry.terraform.io/providers/kreuzwerker/docker/latest/docs)
- [Container](https://github.com/kreuzwerker/terraform-provider-docker/blob/v4.6.0/docs/resources/container.md)
- [Image e keep_locally](https://github.com/kreuzwerker/terraform-provider-docker/blob/v4.6.0/docs/resources/image.md)
- [ADR 026](../../../docs/decisions/026-local-terraform-lab.md)

## Validação da entrega

Terraform 1.16.5 e provider Docker 4.6.0: fmt/validate aprovados. Plan criou
somente três recursos Docker; apply e HTTP aprovados; segundo plan sem mudanças.
Endpoint TCP remoto rejeitado; destroy deixou state sem recursos e preservou
containers existentes. State/planos/plugins ignorados pelo Git; lockfile versionável.
CLI usada na validação foi baixada para `/tmp`, sem instalação global.
