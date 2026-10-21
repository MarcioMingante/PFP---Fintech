# 027 — Release versionada local

## Contexto

Fase 31 consolida entrega, atualização e recuperação. Proprietário manteve GHCR
fora do escopo: nenhuma publicação de imagem nem deploy remoto é necessário.

## Decisão

Usar versão `v0.1.0`, tag Git anotada local e imagens `fintrack-api:v0.1.0` e
`fintrack-web:v0.1.0`. Preservar imagens anteriores; transporte opcional por
save/load e checksum. Não sobrescrever tags já entregues. Registro de evidências
em `docs/releases/`; mídia fictícia em `docs/demo/`.

Backup PostgreSQL custom-format por Make/script com permissão privada, catálogo
validado e publicação atômica sem sobrescrita. Suspender escritas e drenar broker
antes do backup. Restauração em banco vazio isolado; dump não inclui RabbitMQ.
Retorno de imagens exige schema compatível; incompatibilidade exige recuperação
de backup e aceita perda de alterações posteriores. Nenhum downgrade automático.

## Alternativas

Registry simplificaria transporte, mas GHCR foi excluído pelo proprietário.
Deploy automático no computador pessoal exigiria conectividade do runner e
operação adicionais. Apenas reconstruir imagens sem versão dificultaria retorno;
versões locais e save/load preservam snapshots sem publicação.

## Consequências

Release independe de serviços externos. Operador preserva fontes, imagens, `.env`
e backups privados separadamente. Redis é descartável; telemetria não integra
backup financeiro. Dependências Docker continuam necessárias e imagens dependem
da arquitetura. Backups e arquivos temporários não entram no Git.

## Validação

Build das duas imagens, stack Compose isolada, captura Playwright com dados
fictícios, restauração com comparação de todo conteúdo SQL, rejeição de sobrescrita,
migrations repetidas e retorno às imagens phase26 com schema compatível.
Procedimento e limitações em [release local](../releases/local-release.md).
