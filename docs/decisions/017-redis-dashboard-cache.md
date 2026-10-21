# 017 — Redis com revisão transacional do dashboard

## Contexto

Dashboard executa várias agregações PostgreSQL por usuário/data. Fase 21
introduz cache distribuído sem sacrificar isolamento e atualização após escrita.
Apagar uma chave depois do commit permite corrida com consultas em andamento;
falha de Redis durante escrita também pode deixar dados antigos armazenados.

## Decisão

Decorator `CachedDashboardRepository` mantém contrato existente. Cache-aside:
consulta revisão do usuário em PostgreSQL, busca Redis, retorna hit ou executa
repositório de agregações no miss e armazena DTO JSON com TTL absoluto.

Chave: `fintrack:dashboard:v1:{userId}:{asOf}:{revision}`. Usuário autenticado é
obrigatório; data é a mesma validada no serviço. Schema do cache tem versão no
prefixo. Serialização preserva `decimal`; cache não executa cálculos monetários.

### Invalidação

Tabela `dashboard_cache_revisions` tem uma linha por usuário que realizou
escrita financeira. SaveChanges/SaveChangesAsync incrementam revisão via upsert,
na mesma transação da alteração. Inclui contas, categorias, cartões, compras,
parcelas, faturas, recorrências e transações. Se já houver transação de compra
ou geração, utiliza-a; caso contrário, abre transação própria. Auth/sessões não
incrementam revisão. Escrita falha não invalida cache.

Commit torna novas leituras elegíveis somente a chave com nova revisão, para
todas as datas do usuário. Consulta concorrente pode terminar com snapshot
anterior, mas só preenche chave antiga, invisível a leitores após commit.
Invalidação não precisa falar com Redis; escritas durante outage continuam
alterando revisão durable em PostgreSQL. Não há SCAN ou exclusão global.
Chaves antigas expiram naturalmente ou são expulsas por limite de memória.

Hit ainda consulta revisão por chave primária (e autenticação existente usa
banco); economia está em evitar agregações. Não se promete leitura sem SQL.
Futuros jobs/bulk updates/SQL financeiro que não passem pelo DbContext precisam
incrementar revisão na mesma transação. Migrations atuais executam antes de
servir tráfego; alterações administrativas em dados devem respeitar invalidação.

### Disponibilidade/configuração

Cache desabilitado por padrão fora de Compose/Makefile. Habilitado exige
ConnectionStrings:Redis. TTL configurável de 1 a 3600 segundos, padrão 60.
ConnectionMultiplexer singleton é reutilizado; startup não aborta por Redis
indisponível, timeout curto e backlog fail-fast limitam impacto de falhas.

RedisException e RedisTimeoutException resultam em fallback SQL; JSON inválido
é miss. Falha ao armazenar cache não muda resposta financeira. Cancelamento de
request continua sendo cancelamento. PostgreSQL permanece fonte de verdade.
Debug logs registram hit/miss; warnings de indisponibilidade não incluem tokens,
valores financeiros ou detalhes da connection string.

Compose adiciona `redis:7-alpine`, healthcheck, porta de host em loopback,
memória de 128 MB com allkeys-lru e persistência desabilitada. Cache pode ser
perdido sem perda de dados. API usa `redis:6379`, TTL da variável de ambiente.
`make dev-db` inicia PostgreSQL/Redis; `make dev-api` habilita cache local.

## Alternativas

- Somente TTL: rejeitado por atraso após mutações.
- DEL após commit: rejeitado por corrida de repopulação e outage entre sistemas.
- Revisão somente no Redis: rejeitada por perder invalidação durante outage.
- Cache em memória: rejeitado por divergência entre instâncias de API.
- Outbox: desnecessária para este desenho; revisão compartilha transação SQL.

## Consequências

Migration `AddDashboardCacheRevisions` é obrigatória antes de executar código.
Uma escrita SQL pequena adicional por usuário alterado, independentemente de
cache habilitado. API/DTO/UI do dashboard permanecem iguais. Stampede em misses
concorrentes não é coalescido nesta fase: pode haver trabalho duplicado, mas
resultado continua isolado por revisão.

Testcontainers Redis valida hit sem agregações, TTL, invalidação, dados inválidos,
outage, timeout e corrida com commit. E2E usa Redis próprio e prima cache antes
de alterações para validar atualização do saldo no navegador.
