# Auditoria do projeto — 13/10/2026

Revisão de código, documentação, configuração Docker, banco ativo e validações selecionadas. Nenhum segredo foi exibido. Nenhuma migration foi aplicada ao banco pessoal; testes usaram containers isolados. Código da aplicação não foi alterado.

## Conclusão

Existe uma base funcional de finanças pessoais, com backend mais completo que a interface. Não está concluído todo o roadmap. O ambiente Docker atualmente ligado está anterior ao código do checkout: banco tem somente `20260930173644_InitialCreate`, API responde 404 em `/auth/me` e apenas web/API/PostgreSQL estão ativos. Healthchecks verdes não comprovam versão atual nem integrações funcionando.

## Prioridades e falhas

1. **Alta — ambiente ativo desatualizado.** Rebuild/recriação dos serviços e execução do serviço `migrate` são necessários para disponibilizar módulos atuais. A migration de autenticação preserva contas/categorias antigas com proprietário nulo; registros ficam invisíveis para usuários novos. Antes de atualizar banco pessoal, preservar backup e definir titularidade dos registros existentes. Evidências: histórico de migrations consultado por SQL e status HTTP de `/auth/me`.
2. **Alta — configuração informada incompleta para Compose atual.** Além das cinco variáveis informadas, `RABBITMQ_PASSWORD` é obrigatória em `docker-compose.yml`. `RABBITMQ_USER` tem default `fintrack`; Redis e observabilidade têm defaults. `MESSAGING_ENABLED=false` sozinho não remove exigência de senha nem dependência do broker. Não foi necessário ler valores do `.env` para essa conclusão. A chave JWT precisa de pelo menos 32 bytes; conteúdo da chave do usuário não foi validado.
3. **Média — perda de precisão monetária na web.** `apps/web/src/schemas/finance-forms.ts` usa `z.coerce.number()` com limite além da precisão segura de JavaScript. Reprodução: entrada `9999999999999999.99` passa no schema, saída vira `10000000000000000`; API decimal rejeita valor acima do máximo. Outros valores grandes podem perder centavos. Correção: restringir faixa segura ou transportar decimais como strings com validação exata, incluindo contratos de leitura.
4. **Média — dia financeiro calculado em UTC.** Páginas de transações/cartões usam `new Date().toISOString()`, serviços/jobs usam `GetUtcNow()`. Entre 21h e meia-noite em São Paulo, dia padrão é amanhã no horário local. UI e backend concordam em UTC, mas comportamento difere do dia percebido pelo usuário. Definir fuso do produto/usuário e aplicar em defaults, validação e recorrências.
5. **Média — healthcheck não verifica dependências.** `/health` retorna resposta fixa, sem PostgreSQL, versão/schema, Redis ou RabbitMQ. A stack atual demonstra o efeito: `health=200`, autenticação inexistente. Separar liveness de readiness e informar versão da aplicação; dependências opcionais podem ter estado degradado.
6. **Média — limite/fatura sem ciclo de pagamento.** `InvoiceService` calcula limite usado como soma de todas as parcelas históricas. Sem pagamento/quitação, limite nunca é liberado; compra acima do limite é permitida. Política documentada, não falha acidental, mas impede tratar módulo como cartão completo. Compras também não reduzem saldo de conta automaticamente.
7. **Baixa — documentação mistura estágios antigos e atuais.** README/roadmap ainda dizem que transações são “em breve” e interfaces de cartões/compras/faturas estão ausentes, embora páginas e formulários atuais existam. Alguns trechos de desenvolvimento dizem que `dev-db` mantém somente PostgreSQL, mas Makefile inicia também Redis/RabbitMQ. Consolidar status por funcionalidade.

## Inventário funcional

| Módulo | Backend atual | Interface atual | Falta principal |
| --- | --- | --- | --- |
| Autenticação | Cadastro, login, refresh com rotação, logout, sessão revogável, isolamento por usuário | Cadastro/login/logout, cookies HttpOnly, renovação explícita | Recuperação de senha, verificação de e-mail e gestão de sessões não implementadas |
| Contas | Criar/listar/consultar/editar/excluir; bloqueio de exclusão quando referenciada | Criar/listar/buscar | Editar/excluir; saldo inicial e saldo individual |
| Categorias | Criar/listar/consultar/editar | Listar/buscar | Cadastro/edição na web; exclusão não existe na API |
| Transações | Criar receita/despesa e listar, com conta/categoria e proprietário | Criar/listar | Consultar por ID, editar/excluir, transferir, filtrar/paginar; idempotência de lançamento manual |
| Cartões | CRUD com calendário e limite | Criar/listar | Editar/excluir; exibir fechamento/vencimento e limite utilizado/disponível |
| Compras/parcelas | Criar/listar; 1–120 parcelas, centavos exatos, calendário histórico, idempotência e transação | Criar compra parcelada | Histórico detalhado de compras/parcelas; edição/cancelamento/estorno |
| Faturas | Listar/consultar/resumir, itens e totais | Tabela de datas/totais | Detalhes dos itens, resumo de limite e pagamento/quitação |
| Recorrências | Criar/listar/consultar/desativar/gerar; geração periódica com lock/checkpoint | Sem fluxo próprio | Formulário/listagem/desativação e identificação da origem dos lançamentos |
| Dashboard | Agregações por usuário, histórico 12 meses, categorias, faturas e previsão 3 meses | Gráficos, tabelas, filtro de data | Não equivale a saldo bancário conciliado; não inclui pagamento de cartão |
| Redis | Cache de dashboard por usuário/data/revisão; fallback PostgreSQL | Transparente | Serviço ausente na stack ativa |
| Background jobs | Worker de recorrências, retry e limite de processamento | Sem gestão de jobs | Estado operacional/backlog visível; confirmar ativação na stack atualizada |
| RabbitMQ | Outbox, publicação confirmada, dois consumidores idempotentes, retries e dead-letter | Nenhuma | Broker ausente na stack ativa; retenção/reprocessamento operacional |
| Analytics/notificações | Projeções locais em tabelas | Nenhuma | Dashboard não usa projeção analytics; notificações não têm endpoint/UI nem envio de e-mail/push |
| Observabilidade | Logs JSON, OpenTelemetry, propagação de trace; Compose com Collector/Jaeger/Prometheus | UIs das ferramentas previstas | Serviços ausentes na stack ativa; alertas e coleta centralizada de logs |

Sem `NotImplementedException`, TODO ou mocks de produção encontrados no código revisado. Ausência desses marcadores não comprova conclusão: lacunas acima são fluxos efetivamente ausentes ou políticas ainda parciais.

## Arquitetura, persistência e entrega

- Camadas Domain/Application/Infrastructure/Api existem; solução compila. Repositórios usam filtros por proprietário. Compras e recorrências usam transações e proteções de concorrência; compras têm idempotência explícita.
- Migrations incluem autenticação, cartões, compras, faturas, recorrências, transações manuais, revisões de cache, outbox/projeções e contexto de trace. Migrations novas foram exercitadas em bancos efêmeros pelos testes selecionados.
- Dockerfiles de API/web, runtime sem privilégios, persistência PostgreSQL/RabbitMQ e comandos Make existem. `.env` da raiz alimenta Compose; API executada diretamente não o carrega automaticamente. Desenvolvimento local exige connection string e JWT via configuração .NET; mensageria local exige habilitação/conexão próprias.
- Não há workflows `.github` no checkout. CI, registro de imagens, AWS, SQS, Terraform e CD permanecem etapas futuras do roadmap.
- Plano inicial cita `requirements.md`, `business-rules.md` e `architecture.md`; esses documentos separados não existem. README, roadmap e ADRs cobrem parte do conteúdo.

## Validação desta auditoria

- Build da solução .NET: passou, sem warnings ou erros.
- Frontend: lint e typecheck passaram.
- Testes frontend selecionados de formulários/dashboard: 2 arquivos passaram.
- Integração selecionada: **43 testes passaram, zero falhas e zero ignorados**, cobrindo AuthenticationTests, PurchaseEndpointTests, ManualTransactionEndpointTests, DashboardEndpointTests, DashboardCacheTests, BackgroundJobTests e MessagingTests, com bancos/brokers/cache isolados.
- Build Next.js de produção via runner E2E: passou.
- Cenário Playwright Chromium selecionado: **1 passou**, cobrindo cadastro/login/logout, criação e persistência de conta, receita/despesa, totais do dashboard e gráficos. Chromium foi instalado temporariamente em `/tmp` porque navegador esperado pelo Playwright não estava instalado. Containers de teste foram removidos pelo runner.
- Ambiente pessoal: HTTP `/health=200`, `/auth/me=404`, frontend `/dashboard=200`; somente migration inicial. Não houve cadastro/escrita na stack pessoal.
- Não foi executada suíte completa. Não foram validados todos os caminhos de falha, todos os endpoints nem comportamento mobile nesta auditoria. Serviços locais de observabilidade não foram iniciados.

## Ordem recomendada

1. Completar configuração RabbitMQ, preservar backup, atualizar imagens/migrations e conferir autenticação e dependências na stack pessoal.
2. Corrigir precisão monetária e decidir fuso financeiro.
3. Completar categorias e edição/exclusão de contas/cartões; integrar recorrências e detalhes de compras/faturas.
4. Completar transações (correções, exclusão, transferências, saldo por conta) e pagamentos de cartão.
5. Adicionar readiness, retenção/reprocessamento de eventos e documentação consolidada; avançar CI e deployment.
