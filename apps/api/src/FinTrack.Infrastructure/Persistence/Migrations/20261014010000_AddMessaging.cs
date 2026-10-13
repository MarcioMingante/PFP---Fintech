using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace FinTrack.Infrastructure.Persistence.Migrations;

[DbContext(typeof(FinTrackDbContext))]
[Migration("20261014010000_AddMessaging")]
public sealed class AddMessaging : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) => migrationBuilder.Sql("""
        CREATE TABLE messaging_outbox (
            event_id uuid PRIMARY KEY, payload jsonb NOT NULL,
            created_at timestamptz NOT NULL DEFAULT now(), published_at timestamptz NULL);
        CREATE INDEX ix_messaging_outbox_pending ON messaging_outbox(created_at) WHERE published_at IS NULL;
        CREATE TABLE transaction_analytics (
            event_id uuid PRIMARY KEY, transaction_id uuid NOT NULL, user_id uuid NOT NULL,
            amount numeric(18,2) NOT NULL, type integer NOT NULL, occurred_on date NOT NULL);
        CREATE INDEX ix_transaction_analytics_owner_date ON transaction_analytics(user_id, occurred_on);
        CREATE TABLE transaction_notifications (
            event_id uuid PRIMARY KEY, transaction_id uuid NOT NULL, user_id uuid NOT NULL,
            created_at timestamptz NOT NULL DEFAULT now());
        CREATE INDEX ix_transaction_notifications_owner ON transaction_notifications(user_id);
        """);
    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.Sql("""
        DROP TABLE transaction_notifications;
        DROP TABLE transaction_analytics;
        DROP TABLE messaging_outbox;
        """);
}
