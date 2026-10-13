COMPOSE ?= docker compose
API_PORT ?= 5168
WEB_PORT ?= 3000
REDIS_PORT ?= 6379

.PHONY: help dev-db dev-migrate dev-api dev-web full status stop test-api test-integration test-fast test-e2e

help:
	@echo "test-e2e         Executa Playwright desktop/mobile em stack isolada"
	@echo "test-api         Executa toda suíte backend com Testcontainers"
	@echo "test-integration Executa somente integração (Docker necessário)"
	@echo "test-fast        Executa unitários e arquitetura sem Docker"
	@echo "dev-db       Para web/API Docker e inicia PostgreSQL/Redis"
	@echo "dev-migrate  Aplica migrations com API local e conexão configurada"
	@echo "dev-api      Executa API local com dotnet watch"
	@echo "dev-web      Executa Next.js local com npm run dev"
	@echo "full         Build e execução completa em Docker"
	@echo "status       Mostra serviços Docker"
	@echo "stop         Encerra Compose preservando dados"

dev-db:
	$(COMPOSE) stop web api
	$(COMPOSE) up -d --wait postgres redis

dev-migrate:
	dotnet run --project apps/api/src/FinTrack.Api --launch-profile http -- --migrate

dev-api:
	ConnectionStrings__Redis=127.0.0.1:$(REDIS_PORT) DashboardCache__Enabled=true dotnet watch --project apps/api/src/FinTrack.Api run --launch-profile http -- --urls http://localhost:$(API_PORT)

dev-web:
	npm --prefix apps/web run dev -- --port $(WEB_PORT)

full:
	$(COMPOSE) up --build -d --wait

status:
	$(COMPOSE) ps

stop:
	$(COMPOSE) down


test-api:
	dotnet test apps/api/FinTrack.sln -m:1

test-integration:
	dotnet test apps/api/tests/FinTrack.IntegrationTests -m:1

test-fast:
	dotnet test apps/api/tests/FinTrack.UnitTests -m:1
	dotnet test apps/api/tests/FinTrack.ArchitectureTests -m:1


test-e2e:
	npm --prefix apps/web run e2e
