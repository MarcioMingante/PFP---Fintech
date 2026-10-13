# 012 — Faturas persistidas e limite disponível

## Contexto

Fase 16 agrupa parcelas em faturas e expõe totais, vencimento, ciclo atual,
ciclos futuros e limite disponível. Fase 15 já definiu calendário imutável
por compra. Alterações posteriores no cartão devem preservar esse histórico.

## Decisão

`CreditCardInvoice` possui identidade, cartão, proprietário, fechamento e
vencimento. Cada parcela recebe `InvoiceId` obrigatório. Ciclo é identificado
por `(creditCardId, closingDate, dueDate)`, com índice único no PostgreSQL.
Cartão/proprietário usam FK composta; faturas e parcelas têm filtros de usuário.

Datas da fatura vêm das parcelas, não da configuração atual do cartão. Quando
alterações produzem vencimentos diferentes no mesmo mês de fechamento, existem
faturas separadas: não se modifica compromisso já registrado. Parcelas de
compras diferentes com as mesmas datas compartilham uma fatura.

Criação ocorre na transação da compra: upsert por ciclo, atribuição das parcelas,
gravação da compra e commit. Ciclos são processados em ordem cronológica para
manter ordem de bloqueios consistente. Conflitos de idempotência fazem rollback
também das faturas tentativas, antes de consultar compra original. Não há
criação de registros durante GET.

Migration `AddCreditCardInvoices` gera faturas para parcelas da fase 15, agrupa
por cartão/proprietário/datas e preenche `InvoiceId` antes de torná-lo obrigatório.
IDs de compra/parcela, valores e datas existentes são preservados.

### Leitura

- `GET /credit-cards/{id}/invoices`: todas as faturas persistidas, incluindo
  históricas, em ordem de fechamento/vencimento; parcelas detalhadas e total.
- `GET /credit-cards/{id}/invoices/{invoiceId}`: uma fatura pertencente ao cartão.
- `GET /credit-cards/{id}/invoices/summary?asOf=YYYY-MM-DD`: resumo atual/futuro
  e limite. Sem `asOf`, usa data UTC de `TimeProvider` injetado.

Total é soma decimal das parcelas, calculada na leitura; não existe total
persistido redundante. Uma consulta SQL reúne faturas e parcelas, preservando
consistência entre itens e totais no resultado.

Fatura atual corresponde ao próximo ciclo de coleta. No dia do fechamento,
essa fatura já está fechada: ciclo atual avança, conforme regra da fase 15.
Calcula-se próximo fechamento configurado; se houver fatura persistida ainda
aberta com fechamento até essa data, usa-se a primeira por fechamento/vencimento.
Isso preserva ciclos criados com configuração anterior. Sem fatura para esse
intervalo, resposta contém ciclo vazio projetado, com `id: null`, total zero e
sem itens. Não se persiste fatura vazia.

Faturas futuras são demais faturas persistidas com fechamento posterior à data
de referência, incluindo segundo snapshot do mesmo mês. Faturas fechadas
permanecem acessíveis na listagem histórica. `asOf` controla classificação do
calendário; não simula histórico contábil das compras registradas.

### Limite

`usedLimit` soma todas as parcelas registradas, inclusive futuras e fechadas.
`availableLimit = limit - usedLimit`. Sem pagamento registrado, passagem do
tempo não libera limite. Valores negativos mostram excesso (por compras ou
redução de limite); não são truncados a zero. Compra não é bloqueada por limite
nesta fase. Pagamentos/quitação, juros e integração com saldo de contas não
foram implementados; futuro módulo de pagamentos deverá abater dívida quitada.

## Alternativas

- Uma fatura por mês com datas mutáveis: rejeitada por alterar histórico.
- Liberar limite no vencimento: rejeitado por presumir pagamento inexistente.
- Persistir total da fatura: rejeitado por duplicar valores das parcelas.
- Criar faturas em GET: rejeitado para manter leitura sem efeitos colaterais.

## Consequências

Interface de faturas permanece pendente. Datas e itens persistidos suportam
futura evolução de pagamentos e dashboard. Testes cobrem agrupamento, limites,
calendário, backfill, concorrência, idempotência e isolamento entre usuários.
