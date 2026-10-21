# Autenticação e propriedade dos recursos

## Contexto

API de contas/categorias era pública e dados não tinham proprietário. Fase 13
exige registro, login, refresh, logout e isolamento entre usuários. Projeto é
ambiente de aprendizado; este fluxo próprio não implementa servidor OAuth/OIDC.

## Decisão

Usar PasswordHasher do ASP.NET Core e JWT bearer HS256, com configuração de
chave externa, issuer/audience e expiração. Sessões persistidas identificadas
por `sid` tornam revogação imediata possível. Refresh token aleatório é armazenado
somente como hash e rotacionado com atualização atômica condicional.

Frontend Next.js usa Server Actions e cookies HttpOnly/SameSite=Lax. Cookie Secure
é configurável para HTTPS, com HTTP permitido apenas no ambiente local. Renovação
é explícita por POST após expiração do access token. GET não renova sessão.

Application define contratos; Domain contém usuário e sessão sem dependência de
framework; Infrastructure implementa hashing, tokens, identificação do usuário
e persistência. API aplica autenticação, autorização e rate limiting.

Contas e categorias recebem proprietário no repositório durante criação. Filtros
globais EF usam usuário do request; usuário ausente não vê recursos. IDs de outro
usuário retornam 404. Não há papéis, logo nenhuma política adicional de 403.

Migration mantém dados legados sem proprietário, sem atribuição automática.
Recuperação depende de revisão e atribuição explícita; integridade referencial
impede vincular recurso a usuário inexistente.

## Alternativas

ASP.NET Identity completo ofereceria mais funcionalidades de gestão de identidade.
Provedor OIDC externo seria alternativa para autenticação gerenciada. Ambos podem
ser avaliados quando produto exigir recuperação de senha, MFA e federação.
Access tokens totalmente stateless evitariam consulta por request, mas logout
não poderia revogá-los imediatamente sem mecanismo adicional.

## Consequências

Request autenticado consulta sessão no banco; API depende de sua disponibilidade.
Refresh tokens não sobrevivem a reutilização e expiram no limite absoluto da
sessão. Rate limiting por IP protege emissão de tokens, mas clientes de um mesmo
servidor Next.js compartilham IP observado pela API. Logout com API indisponível
remove cookies locais, mas não consegue revogar sessão persistida.

Autenticação não implementa transações ausentes da fase 6 nem CRUD frontend da
fase 10. Testes cobrem concorrência de refresh, revogação, isolamento e upgrade
com dados legados. Visualização por navegador ainda precisa de revisão manual.

## Referência

[Configuração oficial JWT bearer](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/configure-jwt-bearer-authentication?view=aspnetcore-10.0).
