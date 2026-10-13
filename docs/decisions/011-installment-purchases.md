# 011 — Compras parceladas e calendário

## Contexto

Fase 15 transforma compra em N parcelas, preservando dinheiro e definindo
mês de fechamento/vencimento. Faturas e limite disponível pertencem à fase 16.

## Decisão

`Purchase` mantém cartão, proprietário, descrição, valor total, data, quantidade,
chave de idempotência e snapshot dos dias de fechamento/vencimento. Parcelas
são filhas imutáveis desse aggregate; compra e parcelas persistem juntas em
um único `SaveChanges`, com transação automática do EF Core.

API oferece POST e GET em `/credit-cards/{id}/purchases`. Sem edição ou exclusão
de compras nesta fase. Propriedade vem do cartão autenticado; filtros protegem
compras e parcelas. FK composta garante que compra e cartão têm mesmo usuário.
Cartão com compras não pode ser excluído (409), inclusive sob concorrência.

### Precisão

Valor usa `decimal`/`numeric(18,2)`, positivo, sem arredondamento silencioso.
Quantidade aceita 1–120; total deve comportar ao menos um centavo por parcela.
Divisão ocorre em centavos inteiros: quociente comum e centavos restantes nas
primeiras parcelas. R$100 em 3 resulta em R$33,34 + R$33,33 + R$33,33. Soma
sempre corresponde ao total; diferença entre parcelas é no máximo um centavo.

### Calendário

Compra anterior ao fechamento entra no ciclo do mesmo mês. Compra **no dia
ou após fechamento** entra no próximo ciclo. Cada parcela avança um mês de
fechamento. Vencimento é primeiro dia configurado estritamente posterior ao
fechamento: mesmo mês se possível; caso contrário, mês seguinte.

Dia configurado 29–31 é limitado ao último dia do mês. Datas são recalculadas
com dia original a cada mês, evitando propagação de fevereiro para março.
Mesmo dia de fechamento/vencimento produz vencimento no mês seguinte. Quando
meses curtos tornam os dois dias iguais, também se usa mês seguinte.

`DateOnly` evita fuso/horário. Data obrigatória aceita 0001-01-02 até 9989-01-01;
limite superior reserva espaço para 120 parcelas e vencimento posterior sem
estourar calendário. Compras futuras são permitidas. Alterar cartão preserva
datas já geradas; novas compras usam configuração atual.

### Idempotência

Cliente envia GUID não vazio em `idempotencyKey`, estável para reenvios de uma
mesma compra. Índice único por cartão/chave protege inclusive POSTs concorrentes.
Primeiro envio retorna 201. Mesmo payload normalizado retorna 200 e mesmos IDs;
chave com descrição, valor, quantidade ou data diferentes retorna 409.
Snapshot de calendário não integra comparação: reenvio após alteração do cartão
mantém compra original. Chave pode ser reutilizada em outro cartão.

## Alternativas

- Arredondar cada parcela separadamente: rejeitado por alterar soma.
- Gerar parcelas sob demanda: rejeitado por mudar histórico após editar cartão.
- Criar invoices agora: adiado para fase 16; parcelas já possuem datas de ciclo.

## Consequências

Regras de fechamento são política explícita do projeto; podem diferir entre
emissores. Sem cálculo de limite disponível ou bloqueio por limite nesta fase.
Não há interface de compras nesta entrega. Testes cobrem calendário, precisão,
reenvio concorrente, persistência e autorização.
