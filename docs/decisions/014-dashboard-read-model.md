# 014 — Dashboard por modelos de leitura

## Contexto

Fase 18 reúne transações geradas, parcelas, faturas e recorrências em visão
financeira. Não há saldo inicial de conta, pagamentos de cartão ou CRUD manual.

## Decisão

GET autenticado `/dashboard/summary?asOf=YYYY-MM-DD` usa projeções e agregações
SQL em repositório de leitura. Validação aceita 0002-01-01 até 9998-12-01,
reservando calendário para histórico e previsão. Padrão é data UTC injetada.
Filtros globais isolam usuário em todas as consultas.

Consultas compartilham snapshot PostgreSQL RepeatableRead. Não se gera
recorrência ou altera banco em GET. Não se implementa infraestrutura CQRS.

- Saldo registrado: receitas menos despesas com ocorrência até `asOf`, em todas
  as contas; saldo inicial não existe no modelo atual.
- Receitas/despesas do mês: início do mês até `asOf`, inclusive.
- Categorias: despesas desse intervalo, incluindo grupo Sem categoria.
- Histórico: 12 meses, incluindo atual parcial; meses vazios retornam zero.
- Faturas: parcelas agrupadas por fatura com vencimento no mês selecionado,
  inclusive vencimentos após `asOf`. São compromissos separados do saldo.
- Previsão: próximos três meses completos. Soma parcelas por vencimento,
  despesas já registradas nesses meses e próximas ocorrências das recorrências
  mensais ativas. Checkpoint evita projetar novamente ocorrência registrada;
  término e dia original ajustado ao mês são respeitados. Receitas futuras
  não são somadas às despesas previstas.

`asOf` corta calendário de lançamentos, mas não representa snapshot histórico
completo do sistema: projeções usam configuração/estado atualmente registrados.
Quitação não é presumida; faturas não reduzem saldo até existir fluxo de pagamento.

Frontend usa Server Component, fetch sem cache com cookie HttpOnly, validação
Zod e Client Component ECharts. Gráficos importados no navegador, resize por
ResizeObserver e dispose ao desmontar. Tooltip richText evita interpretar nomes
de categorias como HTML. Tabelas/listas fornecem alternativa acessível e dados
permanecem disponíveis se gráficos não carregarem. Filtro de data usa formulário
GET, mantendo URL compartilhável dentro da sessão autenticada.

## Alternativas

- Carregar entidades completas: rejeitado em favor de GROUP BY e projeções.
- Persistir resumo: adiado; snapshot de leitura evita duplicar estado financeiro.
- Somar cartão às despesas das contas: rejeitado para evitar dupla contabilização
  quando pagamento for implementado.
- Gerar automaticamente recorrências na leitura: rejeitado por efeitos colaterais.

## Consequências

Sem migration adicional: índices existentes de usuário/data e ciclos atendem
consultas iniciais. Cache e otimizações por volume poderão evoluir nas fases
21/24. Dashboard usa somente registros reais, incluindo estado vazio.
