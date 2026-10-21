# Release e entrega local — Fase 31

Sem GHCR, ECR, ECS ou deploy remoto. Git identifica fontes; imagens Docker
versionadas identificam executáveis. Nunca sobrescreva uma tag de release já usada.
Execute na raiz; mantenha mesmo nome de projeto Compose e `.env` entre atualizações.

## Preparar versão

1. Revise alterações e escolha nova versão (exemplo: `v0.1.0`).
2. Execute verificações pertinentes às alterações. Para esta entrega operacional:
   build das duas imagens, demonstração pelo navegador, backup/restore em banco
   descartável, migrations repetidas e reinício. Suíte completa somente quando
   solicitada; `make ci` continua disponível para validação ampla voluntária.
3. Construa `make images-build IMAGE_TAG=v0.1.0`. Não reconstrua essa tag depois
   da validação. Registre IDs com `docker image inspect fintrack-api:v0.1.0
   fintrack-web:v0.1.0 --format '{{.RepoTags}} {{.Id}}'`.
4. Registre fontes/documentação em commit e crie tag anotada local:
   `git tag -a v0.1.0 -m 'FinTrack v0.1.0 — release local'`.
   Tag deve apontar ao commit validado; nunca criar tag sobre alterações não commitadas.
5. Publicação Git/GitHub não é necessária. Para transporte offline:

```bash
docker save -o /tmp/fintrack-v0.1.0.tar fintrack-api:v0.1.0 fintrack-web:v0.1.0
sha256sum /tmp/fintrack-v0.1.0.tar > /tmp/fintrack-v0.1.0.tar.sha256
# Destino, com ambos os arquivos no mesmo caminho:
sha256sum -c /tmp/fintrack-v0.1.0.tar.sha256
docker load -i /tmp/fintrack-v0.1.0.tar
```

Arquivo não contém `.env`, banco ou dependências Docker. Preserve `.env` e backups
em local privado separado. Imagens são específicas da arquitetura de origem;
reprodução byte a byte não é garantida por bases/dependências mutáveis.

## Backup antes da atualização

Interrompa acesso de usuários e pare frontend: `docker compose stop web`.
Deixe API finalizar jobs/mensagens; confira outbox e filas usando
[diagnóstico SQL](../database/messaging-diagnostics.sql) e RabbitMQ UI.
Não prossiga com publicações pendentes, mensagens prontas ou sem ack; investigue
DLQs separadamente. Depois `docker compose stop api` interrompe workers.
Não mantenha outra API local escrevendo no banco.

```bash
make backup-local BACKUP=backups/pre-v0.1.0.dump
```

Backup custom-format PostgreSQL contém dados e histórico de migrations, tem
permissão privada e não sobrescreve destino. Catálogo é validado; isso não
substitui ensaio de restauração. Falha mantém serviços parados: corrija ou reinicie
versão atual explicitamente. Não publique dumps. Alvo aceita `COMPOSE='docker
compose --env-file /caminho/ambiente -p nome'` para projeto específico.

Backup cobre PostgreSQL. Redis é descartável; traces/métricas não fazem parte
da recuperação financeira. Filas RabbitMQ não estão no dump: somente restaure
com broker novo após drenar filas, ou use procedimento próprio consistente de
backup do broker. Não reutilize mensagens de uma linha temporal posterior ao dump.

## Atualizar manualmente

Mantenha imagens e fontes da versão anterior disponíveis. Com backup validado,
carregue novas imagens (ou construa antes de parar serviços), confira `.env`
contra `.env.example` e execute:

```bash
make config-check
make images-run IMAGE_TAG=v0.1.0
make status
```

Migrador usa mesma imagem da API; migration pendente roda antes dela. Confirme
`docker compose logs --tail=100 migrate api`, `/health`, login, conta, dashboard
e processamento da outbox. Falha de migration exige diagnóstico; não ignore erro.
Grave `IMAGE_TAG=v0.1.0` em `.env` para próximas execuções. Make permite override
explícito; evite `make full` numa tag já entregue, pois reconstrói imagens.

## Retornar versão

Se schema/dados continuam compatíveis, pare API/web e use imagens anteriores:
`make images-run IMAGE_TAG=VERSAO_ANTERIOR`. Limpe cache com
`docker compose restart redis`; revise configuração/Compose do commit anterior.
Migrations só avançam: trocar imagem não desfaz schema. Não execute downgrade
automático nem assuma compatibilidade por ter passado healthcheck.

Se incompatível, restaure backup **em projeto Compose novo**, com volumes novos,
portas livres e arquivo `.env` privado próprio. Preserve projeto original até
validar recuperação; restauração perde alterações posteriores ao dump.
Exemplo, depois de configurar `.env.recovery` conforme `.env.example`:

```bash
docker compose --env-file .env.recovery -p fintrack-recovery up -d --wait postgres
docker compose --env-file .env.recovery -p fintrack-recovery exec -T postgres \
  sh -c 'exec pg_restore --exit-on-error --no-owner --no-acl -U "$POSTGRES_USER" -d "$POSTGRES_DB"' \
  < backups/pre-v0.1.0.dump
IMAGE_TAG=VERSAO_ANTERIOR docker compose --env-file .env.recovery -p fintrack-recovery \
  up --no-build --pull never -d --wait
```

Use banco vazio, mesma versão major PostgreSQL e usuário do ambiente de destino.
Confira dados, histórico de migrations e autenticação antes de redirecionar uso.
Não execute `down --volumes` no projeto pessoal. Encerrar com `make stop` preserva dados.

## Demonstração de portfólio

[Release v0.1.0](v0.1.0.md) reúne evidências. Para regravar, somente em stack
Compose descartável, nunca banco pessoal:

```bash
FINTRACK_DEMO_URL=http://127.0.0.1:PORTA_WEB FINTRACK_DEMO_DISPOSABLE=1 make demo-release
```

Requer dependências frontend e Chromium Playwright já instalados. Script cria
usuário `example.com`, conta, receita, despesa e compra parcelada fictícios;
valida saldo e calendário antes de salvar PNGs e vídeo WebM. Arquivos em
`docs/demo/v0.1.0/` são substituídos na regravação. Nenhum cookie/dump é exportado.
