# API FinTrack

Base local padrão: `http://localhost:5168`. Rotas conferidas no
[Program.cs](../../apps/api/src/FinTrack.Api/Program.cs). Exemplos usam JSON com
propriedades camelCase, GUIDs para IDs e datas `YYYY-MM-DD`. Valores monetários
são números decimais positivos; tipo define receita ou despesa.

Não há Swagger UI ou documento OpenAPI HTTP mapeado atualmente. Referência abaixo
cobre rotas existentes; contratos e validadores do código definem limites completos.

## Autenticação

| Método | Rota | Autenticação | Resultado principal |
| --- | --- | --- | --- |
| GET | `/health` | Pública | 200, `{"status":"healthy"}`; liveness HTTP |
| POST | `/auth/register` | Pública | 201 com tokens; 400 validação, 409 e-mail existente |
| POST | `/auth/login` | Pública | 200 com tokens; 401 credenciais inválidas |
| POST | `/auth/refresh` | Refresh token no corpo | 200 com tokens novos; 401 token inválido/consumido |
| POST | `/auth/logout` | Bearer | 204; revoga sessão |
| GET | `/auth/me` | Bearer | 200 com `id` e `email` |

Registro/login recebem `{"email":"demo@example.com","password":"SenhaFicticia!123"}`.
Cadastro exige senha de 12–128 caracteres. Resposta de sucesso contém
`accessToken`, `expiresAt`, `refreshToken` e `refreshExpiresAt`.
Refresh recebe `{"refreshToken":"TOKEN_ATUAL"}`; rotação consome token uma única vez.
Use novo token na próxima renovação. Endpoints de registro/login/refresh têm
limite por IP (padrão 20 requests/minuto); excesso retorna 429.

Para testar manualmente, com stack ativa e dados fictícios:

```bash
curl --fail-with-body http://localhost:5168/health
curl --fail-with-body http://localhost:5168/auth/register \
  -H 'Content-Type: application/json' \
  -d '{"email":"demo@example.com","password":"SenhaFicticia!123"}'
# Substitua TOKEN pelo accessToken recebido; não versione tokens.
curl --fail-with-body http://localhost:5168/accounts \
  -H 'Authorization: Bearer TOKEN'
```

API recebe Bearer; cookies HttpOnly são responsabilidade do frontend Next.js.
JWT é validado junto à sessão persistida, permitindo revogação imediata por logout.
Recursos de outro usuário retornam 404 para não revelar sua existência.

## Recursos protegidos

Todas as rotas abaixo exigem `Authorization: Bearer TOKEN`.
`{id}` e `{invoiceId}` são GUIDs. `asOf` é query string opcional de data;
quando omitida, serviços usam data atual UTC.

| Método | Rota | Operação |
| --- | --- | --- |
| GET, POST | `/accounts` | Listar/criar contas |
| GET, PUT, DELETE | `/accounts/{id}` | Consultar/atualizar/excluir conta |
| GET, POST | `/categories` | Listar/criar categorias |
| GET, PUT | `/categories/{id}` | Consultar/atualizar categoria |
| GET, POST | `/transactions` | Listar lançamentos ou criar receita/despesa manual |
| GET, POST | `/credit-cards` | Listar/criar cartões |
| GET, PUT, DELETE | `/credit-cards/{id}` | Consultar/atualizar/excluir cartão |
| GET, POST | `/credit-cards/{id}/purchases` | Listar/criar compras parceladas |
| GET | `/credit-cards/{id}/invoices` | Listar faturas |
| GET | `/credit-cards/{id}/invoices/{invoiceId}` | Consultar fatura |
| GET | `/credit-cards/{id}/invoices/summary?asOf=2024-03-31` | Totais e limite disponível por data |
| GET, POST | `/recurring-transactions` | Listar/criar recorrências mensais |
| GET | `/recurring-transactions/{id}` | Consultar recorrência |
| POST | `/recurring-transactions/{id}/deactivate` | Desativar recorrência |
| POST | `/recurring-transactions/generate` | Gerar lançamentos até data informada |
| GET | `/dashboard/summary?asOf=2024-03-31` | Agregações e projeções do usuário |

Contas/categorias usam `{"name":"Nome fictício"}` no POST/PUT.
Cartões usam `{"name":"Cartão fictício","limit":8000,"closingDay":25,"dueDay":5}`.
PUT recebe campos completos previstos pelo contrato, não é atualização parcial.

### Lançamento manual

POST `/transactions`:

```json
{
  "accountId": "11111111-1111-4111-8111-111111111111",
  "categoryId": null,
  "description": "Salário fictício",
  "amount": 5000.00,
  "type": "Income",
  "occurredOn": "2024-03-05"
}
```

Substitua `accountId` por conta criada pelo usuário autenticado; GUID de exemplo
não é dado inicial. Tipos aceitos: `Income` e `Expense`. Data não pode ser futura;
categoria opcional, quando fornecida, deve pertencer ao mesmo usuário.

### Compra parcelada

POST `/credit-cards/{id}/purchases`:

```json
{
  "description": "Notebook fictício",
  "amount": 1200.00,
  "installmentCount": 4,
  "purchaseDate": "2024-01-25",
  "idempotencyKey": "22222222-2222-4222-8222-222222222222"
}
```

Use nova chave GUID por compra. Repetir mesma chave/conteúdo no mesmo cartão
retorna compra existente (200); conteúdo diferente gera 409. Compra nova retorna
201. Motor divide centavos sem alterar soma e calcula calendário das parcelas.
Compra no fechamento entra no ciclo seguinte; dias inexistentes ajustam ao fim do mês.

### Recorrência mensal

POST `/recurring-transactions`:

```json
{
  "accountId": "11111111-1111-4111-8111-111111111111",
  "categoryId": null,
  "description": "Aluguel fictício",
  "amount": 1500.00,
  "type": "Expense",
  "startDate": "2024-01-31",
  "endDate": null
}
```

POST `/recurring-transactions/generate` recebe `{"throughDate":"2024-03-31"}`
ou `{}` para data atual. Resposta inclui `throughDate`, `generatedCount` e
`hasMore`; backlog pode exigir novos ciclos. Job e endpoint compartilham controle
idempotente/concorrente. Desativação impede novas gerações, preservando histórico.

## Respostas e limites

Leituras/atualizações normalmente retornam 200; criações, 201; exclusões bem-sucedidas,
204. Validação retorna 400 (regras usam ValidationProblem com `errors`);
sem sessão válida retorna 401; recurso ausente ou inacessível, 404.
Excluir conta em uso ou cartão com compras retorna 409. Nem toda rota emite todos
esses status. Falhas inesperadas não têm contrato uniforme de erro documentado.

Não existem endpoints de transferências, pagamento de faturas, exclusão de
categorias ou edição/exclusão de lançamentos. Consultas de dashboard usam
PostgreSQL/cache; consumidores de mensageria não alteram seu contrato.
Consulte [ADRs](../decisions/README.md) para regras, isolamento e limites operacionais.
