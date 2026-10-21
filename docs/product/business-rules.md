# Regras de negócio

## Propriedade e contas

- Recursos financeiros pertencem a um usuário. Recursos alheios são tratados como
  ausentes. Contas legadas sem proprietário não recebem usuário automaticamente.
- Conta inicia sem lançamentos, com saldo zero; não há campo de saldo inicial.
- Nome é obrigatório, aparado e limitado a 100 caracteres. Renomear não altera IDs,
  saldo ou histórico. Não há obrigação de unicidade de nome.
- Conta vinculada a lançamento, recorrência ou qualquer lado de transferência
  não pode ser excluída; API retorna 409 e UI mantém conta e explica bloqueio.

## Dinheiro e saldo

- Moeda BRL; valores individuais positivos até `9999999999999999.99`, no máximo
  duas casas. `decimal` no domínio e `numeric(18,2)` no banco, sem `float`/`double`.
- API emite dinheiro como string decimal com ponto e duas casas. Entradas web
  permanecem strings; não converter por `Number`, nem aceitar expoente ou vírgula
  no contrato. Clientes numéricos antigos são aceitos, mas devem migrar para
  strings, pois precisão perdida no cliente não pode ser recuperada pelo servidor.
- Totais podem exceder limite individual; tabelas/cartões/tooltip mantêm string
  exata. Número aproximado é usado somente nas coordenadas dos gráficos ECharts.
- Saldo da conta até hoje UTC = receitas - despesas + transferências recebidas -
  transferências enviadas, considerando datas registradas até hoje.
- Saldo negativo é permitido; não existe autorização por limite de cheque especial.
  Exigir saldo suficiente seria nova política e precisaria considerar concorrência.

## Transações e transferências

- Receita aumenta saldo da conta; despesa reduz. Categoria é opcional e, se
  informada, pertence ao mesmo usuário. Descrição possui 1–200 caracteres aparados.
- Lançamento/transferência manual não aceita data futura. Data financeira é UTC.
- Origem/destino de transferência são contas diferentes, do mesmo usuário. Um
  registro associa as duas contas, portanto não existe “metade” de transferência.
- Chave GUID de idempotência é obrigatória e única por usuário. Mesma chave e mesmo
  conteúdo retornam transferência existente; conteúdo diferente retorna 409.
- Registro da transferência e revisão de cache compartilham transação. Falha
  reverte ambos. Concorrência não atualiza contadores de saldo: saldo é agregado
  de registros, evitando perda de atualização.
- Transferências não são receitas/despesas, não geram TransactionCreated v1 e não
  mudam saldo agregado do usuário, categorias, histórico de renda ou previsões.
  São consultadas em `/transfers`, separadamente de `/transactions`.

## Cartões, parcelas e recorrências

- Compra possui 1–120 parcelas; centavos são distribuídos preservando total exato.
- Compra no fechamento entra no próximo ciclo; dia inexistente ajusta ao último
  dia do mês. Configuração histórica da compra é preservada.
- Faturas agrupam parcelas por cartão, proprietário e datas. Limite disponível
  desconta todas as parcelas; tempo não presume pagamento nem libera limite.
- Recorrência mensal preserva dia original, tem início/fim inclusivo e gera no
  máximo uma transação por ocorrência. Geração manual/job compartilham locks.
- Pagamento, quitação, estorno e conciliação bancária não estão implementados.

[ADRs](../decisions/README.md) detalham implementação e alternativas.
