# 016 — Fluxos E2E com Playwright

## Contexto

Fase 20 deve testar produto pelo navegador: login, conta, transação, dashboard,
cartão, compra parcelada e fatura. Formulários dessas operações ainda estavam
pendentes. Testes exclusivos de API não verificariam experiência solicitada.

## Decisão

Adicionar Playwright Test com Chromium desktop e mobile (Pixel 7 emulado),
usuário exclusivo por cenário, seletores por papel/label e assertions que
aguardam estado visível. Sem mocks HTTP, sleeps fixos nos cenários ou retries
que ocultem falhas. Registrar trace, screenshot e vídeo somente em falha;
relatório HTML local permanece ignorado pelo Git.

Runner `npm run e2e` cria PostgreSQL 17 Alpine descartável, porta aleatória,
credenciais efêmeras e chave JWT de teste. Builda API, aplica migrations e faz
build Next.js Webpack em `.next-e2e`, separado de `.next` de desenvolvimento.
Inicia API/web em loopback com portas livres, aguarda prontidão e executa testes.
Cleanup encerra processos e remove somente container criado, inclusive em erro.
Config Playwright exige flag E2E e URL de loopback do runner, sem uso de stack
pessoal/produção. Não inicia Compose existente nem reutiliza banco.

### Operações necessárias

- Formulário de criação de conta na rota existente.
- POST autenticado `/transactions` e formulário/listagem de receitas/despesas.
  Recorrência torna-se referência opcional; migration preserva gerados existentes.
  Data manual não pode ser futura. Propriedade de conta/categoria é validada.
- Rota `/credit-cards`: criação de cartão, compra parcelada e tabela de faturas.
  Compra usa chave de idempotência por formulário, preservada em reenvio.
- Server Actions mantêm token em cookies HttpOnly e enviam escrita pela API;
  validam Zod, retornam erros e revalidam páginas/dashboard após sucesso.

Transações manuais não implementam transferências, edição/exclusão, saldo
inicial ou pagamentos. Usuário registra fatos; saldo segue agregação da fase 18.
Migration reversa só é possível após tratar lançamentos sem recorrência, pois
schema anterior não os representava.

### Cenários

1. Cadastro, logout, erro de login, login, conta persistida, receita/despesa,
   saldo do dashboard, histórico acessível e gráficos ECharts renderizados.
2. Cartão, compra no dia do fechamento, divisão de centavos e datas/valores de
   três faturas persistidas, dashboard sem misturar compras ao saldo das contas.
3. Bloqueio de página sem sessão, logout completo, isolamento entre usuários
   e recuperação de filtro de data inválida.

Mesmos cenários executam em desktop/mobile. Teste de gráfico verifica SVG e
captura erros JavaScript; cenário principal verifica ausência de overflow.

## Alternativas

- Preparar dados financeiros via API em vez dos formulários: rejeitado nos fluxos
  principais para testar interação real do produto.
- Browser externo/manual: não é requisito; Chromium oficial headless é instalado
  por Playwright para execução reproduzível.
- Banco/porta fixos: rejeitados por colisão e interferência em desenvolvimento.

## Consequências

Docker, SDK .NET, Node/dependências e Chromium Playwright são requisitos.
Primeira execução faz downloads; instalar dependências Linux do navegador pode
ser necessário conforme ambiente. CI fica para fase 25. Relatórios contêm dados
sintéticos e tokens temporários da stack de teste e não devem ser publicados.
