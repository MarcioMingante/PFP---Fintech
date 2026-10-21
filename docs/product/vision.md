# Visão do produto

## Problema e público

FinTrack organiza finanças pessoais em contas, lançamentos, cartões e recorrências.
Serve ao proprietário para uso pessoal e a avaliadores de portfólio que precisam
reproduzir fluxos reais sem serviços pagos obrigatórios. Não é instituição
financeira, conexão bancária ou sistema comercial de escrituração.

## Resultado esperado

Usuário cadastra acesso, organiza contas/categorias, registra receitas/despesas,
transfere entre contas próprias e acompanha saldo registrado e compromissos de
cartão. Dados permanecem isolados por usuário. Outra pessoa consegue executar
stack local pelo README e avaliar código, testes, decisões e demonstração.

## Escopo atual e limites

Backend .NET, frontend Next.js e PostgreSQL, com Redis/RabbitMQ e observabilidade
locais. Contas têm CRUD na API/web. Cartões têm CRUD API e criação/listagem na web.
Compras geram parcelas/faturas; recorrências têm API e job, sem gestão web própria.
Saldo começa em zero e é derivado do histórico; saldo negativo é permitido.
Não presume saldo bancário conciliado nem quitação de faturas.

Pagamentos de cartão, estornos, edição/exclusão de lançamentos e gestão completa
de cartões/categorias/recorrências na web continuam evolução do produto, sem
impedir aceite dos milestones M0/M3/M4 definidos nesta entrega.

## Restrições

- Execução funcional local com Docker Compose; sem GHCR ou deploy cloud obrigatório.
- AWS/SQS são estudos documentais; laboratório Terraform é opcional e isolado.
- Sem dados reais em demo, segredos no Git ou acesso público ao banco.
- Moeda única BRL, duas casas decimais; sem conversão cambial.
- Dia financeiro atual usa UTC em toda aplicação; fuso por usuário é evolução
  explícita, não comportamento local brasileiro presumido.

## Critérios de sucesso

Requisitos e regras abaixo são verificáveis por API, testes e fluxos de navegador.
Transferências preservam soma dos saldos e não inflam receitas/despesas. Valores
mantêm centavos na entrada, banco, JSON e exibição. README reproduz setup local.
Não se define meta de SLA, escala ou certificação de segurança sem medição.

[Requisitos](requirements.md) · [Regras](business-rules.md) · [Casos de uso](use-cases.md)
