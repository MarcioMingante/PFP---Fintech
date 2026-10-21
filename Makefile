COMPOSE ?= docker compose
API_PORT ?= 5168
WEB_PORT ?= 3000
REDIS_PORT ?= 6379
IMAGE_TAG ?= local
export IMAGE_TAG
TERRAFORM ?= terraform
TF_LOCAL = env -u DOCKER_CONTEXT -u DOCKER_HOST $(TERRAFORM) -chdir=ops/terraform/local

.PHONY: help dev-db dev-migrate dev-api dev-web full status stop test-api test-integration test-fast test-e2e dev-observability
.PHONY: ci ci-web ci-api ci-e2e ci-install ci-install-e2e
.PHONY: images-build images-run
.PHONY: setup config-check logs migrate
.PHONY: test-messaging
.PHONY: iac-init iac-validate iac-plan iac-apply iac-destroy

help:
	@echo "backup-local    Backup PostgreSQL privado (BACKUP=backups/fintrack.dump)"
	@echo "demo-release    Grava demonstração em stack descartável explicitamente informada"
	@echo "iac-init/validate/plan/apply/destroy  Exercício Terraform Docker isolado"
	@echo "test-messaging  Valida mensageria com testes direcionados (Docker necessário)"
	@echo "setup           Cria .env local com segredos aleatórios, sem sobrescrever"
	@echo "config-check    Valida configuração Compose sem exibir segredos"
	@echo "logs            Acompanha logs locais (Ctrl+C encerra acompanhamento)"
	@echo "migrate         Aplica migrations em container, sem iniciar API/web"
	@echo "images-build    Constrói API/frontend locais (IMAGE_TAG=local por padrão)"
	@echo "images-run      Inicia Compose com imagens locais, sem build ou pull"
	@echo "ci              Executa verificações equivalentes ao CI público (Docker necessário)"
	@echo "ci-install      Instala dependências frontend pelo lockfile"
	@echo "ci-install-e2e  Instala Chromium e dependências de sistema do Playwright"
	@echo "dev-observability Inicia Collector, Jaeger e Prometheus locais"
	@echo "test-e2e         Executa Playwright desktop/mobile em stack isolada"
	@echo "test-api         Executa toda suíte backend com Testcontainers"
	@echo "test-integration Executa somente integração (Docker necessário)"
	@echo "test-fast        Executa unitários e arquitetura sem Docker"
	@echo "dev-db       Para web/API Docker e inicia PostgreSQL/Redis/RabbitMQ"
	@echo "dev-migrate  Aplica migrations com API local e conexão configurada"
	@echo "dev-api      Executa API local com dotnet watch"
	@echo "dev-web      Executa Next.js local com npm run dev"
	@echo "full         Build e execução completa em Docker"
	@echo "status       Mostra serviços Docker"
	@echo "stop         Encerra Compose preservando dados"

dev-db:
	$(COMPOSE) stop web api
	$(COMPOSE) up -d --wait postgres redis rabbitmq

dev-observability:
	$(COMPOSE) up -d otel-collector jaeger prometheus

dev-migrate:
	dotnet run --project apps/api/src/FinTrack.Api --launch-profile http -- --migrate

dev-api:
	ConnectionStrings__Redis=127.0.0.1:$(REDIS_PORT) DashboardCache__Enabled=true BackgroundJobs__Enabled=true dotnet watch --project apps/api/src/FinTrack.Api run --launch-profile http -- --urls http://localhost:$(API_PORT)

dev-web:
	npm --prefix apps/web run dev -- --port $(WEB_PORT)

full:
	$(COMPOSE) up --build -d --wait

setup:
	sh scripts/setup-local.sh

config-check:
	$(COMPOSE) config --quiet

logs:
	$(COMPOSE) logs --follow --tail=100

migrate:
	$(COMPOSE) run --rm migrate

images-build:
	docker build --tag fintrack-api:$(IMAGE_TAG) apps/api
	docker build --tag fintrack-web:$(IMAGE_TAG) apps/web

images-run:
	$(COMPOSE) up --no-build --pull never -d --wait

status:
	$(COMPOSE) ps

stop:
	$(COMPOSE) down


test-api:
	dotnet test apps/api/FinTrack.sln -m:1

test-integration:
	dotnet test apps/api/tests/FinTrack.IntegrationTests -m:1

test-messaging:
	dotnet test apps/api/tests/FinTrack.UnitTests -m:1 --filter FullyQualifiedName~FinTrack.UnitTests.Messaging.MessagingTests
	dotnet test apps/api/tests/FinTrack.IntegrationTests -m:1 --filter FullyQualifiedName~FinTrack.IntegrationTests.MessagingTests

iac-init:
	$(TF_LOCAL) init

iac-validate:
	$(TF_LOCAL) fmt -check -recursive
	$(TF_LOCAL) validate

iac-plan:
	$(TF_LOCAL) plan -out=local.tfplan

iac-apply:
	$(TF_LOCAL) apply local.tfplan

iac-destroy:
	$(TF_LOCAL) destroy

test-fast:
	dotnet test apps/api/tests/FinTrack.UnitTests -m:1
	dotnet test apps/api/tests/FinTrack.ArchitectureTests -m:1


test-e2e:
	npm --prefix apps/web run e2e

# Sequential: frontend checks and E2E both generate apps/web/next-env.d.ts.
ci:
	$(MAKE) ci-web
	$(MAKE) ci-api
	$(MAKE) ci-e2e

ci-install:
	npm --prefix apps/web ci

ci-install-e2e:
	cd apps/web && npx --no-install playwright install --with-deps chromium

ci-web:
	npm --prefix apps/web run lint
	npm --prefix apps/web run typecheck
	npm --prefix apps/web test
	npm --prefix apps/web run build -- --webpack

ci-api:
	dotnet restore apps/api/FinTrack.sln
	dotnet build apps/api/FinTrack.sln --configuration Release --no-restore -m:1
	dotnet test apps/api/tests/FinTrack.UnitTests --configuration Release --no-build -m:1
	dotnet test apps/api/tests/FinTrack.ArchitectureTests --configuration Release --no-build -m:1
	dotnet test apps/api/tests/FinTrack.IntegrationTests --configuration Release --no-build -m:1

ci-e2e:
	CI=true npm --prefix apps/web run e2e

.PHONY: backup-local demo-release
BACKUP ?= backups/fintrack.dump
backup-local:
	mkdir -p "$(dir $(BACKUP))"
	sh scripts/backup-local.sh "$(BACKUP)" $(COMPOSE)

demo-release:
	node apps/web/scripts/record-demo.mjs
