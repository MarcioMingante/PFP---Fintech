# 022 — Imagens Docker locais

## Contexto

Fase 26 precisa entregar executáveis versionados sem exigir publicação em
registry. Aplicação já possui Dockerfiles e runtime via Compose.

## Decisão

Fase 26 entrega imagens locais `fintrack-api:<tag>` e `fintrack-web:<tag>`.
GHCR, ECR e publicação em qualquer registry ficam fora do escopo por decisão
do proprietário. Não alteramos workflow para publicar imagens.

`make images-build IMAGE_TAG=phase26` constrói ambas sem carregar `.env` ou
iniciar serviços. Compose usa `IMAGE_TAG` para API, migrations e frontend,
preservando `local` como padrão. `make images-run IMAGE_TAG=phase26` executa
imagens existentes com `--no-build --pull never`; dependências também precisam
estar disponíveis localmente. `make full` continua oferecendo build e execução.

API publica Release em estágio SDK e executa em ASP.NET Alpine, com usuário
não root. Frontend instala dependências pelo lockfile, compila Next.js com
webpack (mesmo compilador do CI/E2E) e copia somente saída standalone, estáticos
e public para runtime Node Alpine com usuário node. Healthchecks existentes
permanecem. Tag identifica snapshot local; reconstruir mesma tag substitui
referência. Não há garantia de build byte a byte: bases e pacotes NuGet podem
evoluir. Para preservar snapshot use tag nova e, opcionalmente, docker save.

Contextos excluem `.env`, certificados/chaves privadas, logs e saídas de testes.
Frontend também exclui `.next-e2e`, traces, vídeos e relatórios Playwright,
que podem conter dados de sessões de teste. Dockerfiles não recebem segredos
por ARG/ENV de build. JWT, senhas e conexões são fornecidos em runtime; não
editar appsettings ou arquivos public para incluir dados reais.

## Alternativas

Publicar GHCR facilitaria distribuição, mas foi excluído pelo proprietário.
ECR exigiria integração cloud fora do escopo. Build em cada máquina continua
possível; save/load permite transportar snapshot local sem registry.

## Consequências

Operador preserva imagens e tags localmente e fornece dependências Docker
separadamente. Tags podem ser substituídas; release do ADR 027 estabelece
preservação das tags entregues. Não há endereço remoto para pull das imagens.

## Validação

Construir ambas as imagens; verificar usuário runtime, ausência de arquivos
sensíveis e healthchecks. Smoke isolado usa PostgreSQL descartável, aplica
migrations e verifica API e frontend sem tocar volumes de desenvolvimento.
