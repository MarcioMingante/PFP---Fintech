# 001 — Backend monolítico em camadas

## Contexto

Domínio financeiro precisa compartilhar transações e regras enquanto projeto evolui.
Código atual reúne módulos em uma API e quatro projetos .NET. Este registro
formaliza retrospectivamente arquitetura existente; não declara adoção de microserviços.

## Decisão

Manter backend monolítico organizado por funcionalidades, com Domain, Application,
Infrastructure e Api. Domain não referencia outras camadas ou frameworks de
persistência/HTTP. Application referencia Domain e define casos de uso e contratos.
Infrastructure implementa esses contratos e referencia Application/Domain. Api
referencia Application/Infrastructure para endpoints e composição por DI.

Separação por Commands/Queries é usada onde já existe; serviços de aplicação
continuam válidos. Não impor novos assemblies por módulo nem bancos separados.
Jobs e mensageria executam dentro da API, conforme ADRs 018/019.

## Alternativas

- Microserviços: permitiriam deploy e escala independentes, mas exigiriam operação
  distribuída e coordenação de consistência antes de uma necessidade demonstrada.
- Toda lógica nos endpoints: menos estrutura inicial, porém acoplaria regras ao HTTP.
- Isolamento rígido por módulo: possível evolução; estrutura atual não garante
  bounded contexts independentes ou ausência de acesso compartilhado ao DbContext.

## Consequências

Deploy do backend é único e operações podem compartilhar transações PostgreSQL.
Camadas aumentam número de contratos e arquivos; mudanças devem preservar direção
das dependências. Não há escala independente dos workers nesta composição.

## Evidências

- [Projetos backend](../../apps/api/src/).
- [Testes de dependências](../../apps/api/tests/FinTrack.ArchitectureTests/LayerDependencyTests.cs).
