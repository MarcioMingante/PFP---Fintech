using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace FinTrack.Infrastructure.Persistence.Migrations;

[DbContext(typeof(FinTrackDbContext))]
[Migration("20261014020000_AddOutboxTraceContext")]
public sealed class AddOutboxTraceContext : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) => migrationBuilder.Sql("""
        ALTER TABLE messaging_outbox ADD COLUMN trace_parent text NULL, ADD COLUMN trace_state text NULL;
        """);
    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.Sql("""
        ALTER TABLE messaging_outbox DROP COLUMN trace_parent, DROP COLUMN trace_state;
        """);
}
