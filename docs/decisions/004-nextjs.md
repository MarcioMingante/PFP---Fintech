# 004 — Frontend Next.js com App Router

## Contexto

Frontend precisa integrar API .NET, formulários autenticados e dashboard com
visualização financeira. Aplicação existente usa Next.js/TypeScript; este registro
formaliza desenho atual retrospectivamente, sem atribuir data histórica à escolha.

## Decisão

Usar App Router com Server Components para leitura e composição no servidor,
Client Components para interação e gráficos, e Server Actions nos fluxos que
já as utilizam. Centralizar comunicação com API em serviços e validar contratos
com schemas Zod. Sessão usa cookies HttpOnly conforme ADR 009.

Build standalone executa em Node.js no container; `API_URL` aponta para API
em runtime. ECharts renderiza gráficos. Não declarar adoção de Shadcn por estar
no planejamento: implementação atual determina componentes e dependências reais.

## Alternativas

- SPA com React e API diretamente no browser: simplificaria servidor frontend,
  mas mudaria gestão da sessão e comunicação atuais.
- Exportação estática: não comporta os fluxos atuais que dependem de servidor,
  cookies e ações; não é substituto direto do runtime Node.js.
- Renderização de interface no backend .NET: possível, mas não atende escolha
  de aprendizado e separação frontend/backend do projeto.

## Consequências

API e frontend têm builds/runtimes separados. Código cliente não deve receber
segredos do servidor. Container web exige conectividade interna com API;
SSR não torna aplicação hospedável apenas como arquivos estáticos. E2E cobre
integração pelo navegador, conforme ADR 016.

## Evidências

- [Rotas e layouts](../../apps/web/src/app/).
- [Dependências](../../apps/web/package.json).
- [Standalone](../../apps/web/next.config.ts).
