# 029 — Dinheiro exato na fronteira JSON e frontend

Status: aceita. Escopo: implementação local. Registro: 21/10/2026.

## Contexto

Converter entrada para JavaScript Number antes de validar perde centavos dentro
da faixa numeric(18,2). JSON numérico de saída também é arredondado por JSON.parse.
Defeito reproduzido com `90071992547409.91` e `9999999999999999.99`.

## Decisão

Backend conserva decimal; banco conserva numeric(18,2). Converter HTTP decimal
emite strings invariantes com duas casas. Aceita strings decimais com ponto e
até duas casas e continua aceitando clientes JSON numéricos existentes; serviços
ainda validam limite/positividade. Expoentes em string, separador decimal vírgula
e frações menores que centavo são rejeitados.

Formulários usam entrada textual com teclado decimal e validam string antes de
qualquer conversão. Zod normaliza casas e compara centavos por BigInt. Contratos
web de leitura exigem strings para evitar aceitar valores já arredondados. Formatação
agrupa inteiro com BigInt e preserva fração textual, sem Intl.NumberFormat(number).
Totais derivados podem ultrapassar limite individual e não são limitados a numeric(18,2)
no schema de leitura; backend continua limitado ao alcance de decimal.

ECharts recebe Number somente para coordenadas e escala visual. Tooltips e tabelas
usam valores originais exatos, nunca números reconvertidos do gráfico. Não há
cálculo financeiro autoritativo em Number no frontend.

Serialização interna da outbox e Redis permanece .NET/decimal; contrato do evento
TransactionCreated não muda nesta entrega.

## Alternativas

- Limitar entrada a faixa segura de Number: reduziria suporte monetário sem resolver
  totais grandes ou todos os caminhos de leitura.
- Arredondar depois do parse: não recupera centavos perdidos.
- Biblioteca decimal no frontend: viável para cálculos futuros; operações atuais
  exigem validação/formatação, atendidas por strings e BigInt sem dependência nova.

## Consequências

Tipo JSON de saída monetária muda de number para string. Atualizar API e frontend
juntos; consumidores próprios devem adaptar leitura. Valores já corrompidos em
cliente/banco não são corrigidos automaticamente: não há fonte segura para inferir
centavos originais. Campos numéricos legados aceitos no request não recuperam
precisão já perdida pelo emissor. Contrato recomendado usa strings.

## Evidências e validação

[Regressões frontend](../../apps/web/tests/money.test.mjs),
[round-trip API/PostgreSQL](../../apps/api/tests/FinTrack.IntegrationTests/MoneyPrecisionEndpointTests.cs)
e [E2E](../../apps/web/e2e/finance.spec.ts) verificam extremos, agregados acima do
limite individual, rejeições, exibição e persistência em desktop/mobile.
