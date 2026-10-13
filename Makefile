COMPOSE ?= docker compose
API_PORT ?= 5168
WEB_PORT ?= 3000

.PHONY: help dev-db dev-migrate dev-api dev-web full status stop

help:
	@echo "dev-db       Para web/API Docker e inicia somente PostgreSQL"
	@echo "dev-migrate  Aplica migrations com API local e conexão configurada"
	@echo "dev-api      Executa API local com dotnet watch"
	@echo "dev-web      Executa Next.js local com npm run dev"
	@echo "full         Build e execução completa em Docker"
	@echo "status       Mostra serviços Docker"
	@echo "stop         Encerra Compose preservando dados"

dev-db:
	$(COMPOSE) stop web api
	$(COMPOSE) up -d --wait postgres

dev-migrate:
	dotnet run --project apps/api/src/FinTrack.Api --launch-profile http -- --migrate

dev-api:
	dotnet watch --project apps/api/src/FinTrack.Api run --launch-profile http -- --urls http://localhost:$(API_PORT)

dev-web:
	npm --prefix apps/web run dev -- --port $(WEB_PORT)

full:
	$(COMPOSE) up --build -d --wait

status:
	$(COMPOSE) ps

stop:
	$(COMPOSE) down
