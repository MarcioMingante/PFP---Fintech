# 010 — Cartões de crédito como aggregate

## Contexto

A fase 14 introduz cartões pertencentes a usuários autenticados. Compras,
parcelas e faturas serão desenvolvidas nas fases seguintes.

## Decisão

`CreditCard` é raiz do aggregate, com identidade, proprietário imutável, nome,
limite, dia de fechamento e dia de vencimento. Alterações validam todos os
campos antes de modificar o estado. Nome possui 1–100 caracteres após trim.

Limite é `decimal` no domínio e `numeric(18,2)` no PostgreSQL: maior que zero,
até 9999999999999999.99, sem arredondamento silencioso na entrada. Não há
saldo disponível calculado nesta fase. Valores usam unidade monetária única
implícita; conversão cambial não faz parte do módulo.

Dias aceitam 1–31, independentemente de sua ordem. Fechamento e vencimento
podem cair em meses diferentes. Não se calcula data concreta nesta fase;
as fases 15/16 deverão definir tratamento de meses curtos e inclusão de
compras no dia do fechamento.

Proprietário é atribuído pelo contexto autenticado, nunca pelo payload.
Filtro global de consulta protege leitura, atualização e exclusão.
FK obrigatória relaciona cartão ao usuário; exclusão do usuário é restrita.
Limite positivo e dias válidos também possuem constraints no banco.

## Alternativas

- `float`/`double`: rejeitados por precisão binária inadequada a dinheiro.
- Value object Money: adiado até haver moeda ou operações monetárias comuns
  que justifiquem abstração adicional.
- Implementar compras agora: adiado para fases 15/16, junto das regras de
  parcelas e faturas; não se cria entidade sem comportamento definido.

## Consequências

CRUD autenticado disponível em `/credit-cards`. Recursos inexistentes ou de
outro usuário retornam 404. Sem autenticação, retornam 401. Validação retorna
400 com erros por campo. Exclusão física é permitida enquanto não existem
compras; sua política deverá ser revista quando esse relacionamento existir.
Testes cobrem precisão, invariantes, persistência, CRUD e isolamento.
