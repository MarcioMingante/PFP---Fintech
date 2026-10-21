-- Diagnóstico somente leitura. Não exibe payloads, usuários ou valores financeiros.
-- Execute no PostgreSQL local; não modifica outbox, filas ou efeitos dos consumidores.
SELECT
    count(*) FILTER (WHERE published_at IS NULL) AS pending,
    count(*) FILTER (WHERE published_at IS NOT NULL) AS published,
    min(created_at) FILTER (WHERE published_at IS NULL) AS oldest_pending_at
FROM messaging_outbox;

SELECT 'analytics' AS consumer, count(*) AS processed_events
FROM transaction_analytics
UNION ALL
SELECT 'notifications' AS consumer, count(*) AS processed_events
FROM transaction_notifications;
