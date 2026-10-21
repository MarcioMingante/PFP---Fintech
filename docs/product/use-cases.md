# Casos de uso principais

Pré-condição comum: stack local executando; usuário autenticado, exceto cadastro/login.
Todos os exemplos usam dados fictícios. IDs devem ser obtidos na própria sessão.

| Caso | Fluxo principal | Alternativa / resultado esperado |
| --- | --- | --- |
| UC01 — Acessar | Cadastrar em `/register`, entrar em `/login`, consultar dashboard e sair | Credenciais inválidas: erro sem sessão; recurso protegido sem sessão: login/401 |
| UC02 — Criar conta | Abrir `/accounts`, informar nome e salvar | Nome inválido: erro; sucesso persiste após recarga e saldo inicial é zero |
| UC03 — Editar conta | Na lista, clicar Editar, preencher novo nome e Salvar alteração | Cancelar não grava; recurso inexistente/alheio não pode ser alterado; saldo/histórico preservados |
| UC04 — Excluir conta | Clicar Excluir, revisar conta e Confirmar exclusão | Cancelar preserva; em uso exibe bloqueio e mantém dados; conta vazia desaparece após recarga |
| UC05 — Lançar receita/despesa | Criar conta, abrir `/transactions`, escolher tipo/conta, descrição, valor e data | Categoria opcional; valor fracionário inválido/data futura não grava; sucesso aparece na lista/saldo/dashboard |
| UC06 — Transferir | Com duas contas próprias, abrir `/transactions`, escolher origem/destino, valor, descrição e data | Contas iguais, alheias ou valor inválido não gravam; retry da mesma chave não duplica; conflito retorna 409 |
| UC07 — Conferir saldo | Consultar `/accounts` após receita de 100.02 e transferência de 10.01 para outra conta | Origem 90.01, destino 10.01; dashboard 100.02, sem nova receita/despesa causada por transferência |
| UC08 — Parcelar compra | Criar cartão, registrar 1200.00 em quatro parcelas e consultar faturas | Total das parcelas 1200.00; calendário segue fechamento/vencimento e snapshot histórico |
| UC09 — Gerar recorrência | Pela API, cadastrar template mensal e gerar até data; ou aguardar job | Repetir geração não duplica; desativação preserva lançamentos anteriores |
| UC10 — Validar centavos | Registrar 90071992547409.91 como string, recarregar transações/contas/dashboard | Todos exibem `90.071.992.547.409,91`; gráfico não altera valor autoritativo |

## Caminhos técnicos

[Guia API](../api/README.md) descreve rotas. [Testes E2E](../../apps/web/e2e/finance.spec.ts)
validam fluxos de contas, transferência e centavos em desktop/mobile.
[Testes de transferência](../../apps/api/tests/FinTrack.IntegrationTests/TransferEndpointTests.cs)
verificam concorrência, isolamento, integridade de saldos e rollback.

Esses casos não prometem fluxos pendentes de pagamento, estorno ou gestão web de
recorrências. Consulte [requisitos](requirements.md) antes de ampliar escopo.
