using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinTrack.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTransfers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "transfers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    destination_account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    description = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    occurred_on = table.Column<DateOnly>(type: "date", nullable: false),
                    idempotency_key = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_transfers", x => x.id);
                    table.CheckConstraint("ck_transfers_accounts", "source_account_id <> destination_account_id");
                    table.CheckConstraint("ck_transfers_amount", "amount > 0");
                    table.ForeignKey(
                        name: "FK_transfers_accounts_destination_account_id",
                        column: x => x.destination_account_id,
                        principalTable: "accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_transfers_accounts_source_account_id",
                        column: x => x.source_account_id,
                        principalTable: "accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_transfers_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            // Nullable legacy owners remain untouched; transfers require both accounts' actual owner.
            migrationBuilder.Sql("""
                CREATE UNIQUE INDEX ux_accounts_id_owner ON accounts (id, user_id);
                ALTER TABLE transfers ADD CONSTRAINT fk_transfers_source_owner
                    FOREIGN KEY (source_account_id, user_id) REFERENCES accounts (id, user_id) ON DELETE RESTRICT;
                ALTER TABLE transfers ADD CONSTRAINT fk_transfers_destination_owner
                    FOREIGN KEY (destination_account_id, user_id) REFERENCES accounts (id, user_id) ON DELETE RESTRICT;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_transfers_destination_account_id",
                table: "transfers",
                column: "destination_account_id");

            migrationBuilder.CreateIndex(
                name: "IX_transfers_source_account_id",
                table: "transfers",
                column: "source_account_id");

            migrationBuilder.CreateIndex(
                name: "IX_transfers_user_id_idempotency_key",
                table: "transfers",
                columns: new[] { "user_id", "idempotency_key" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "transfers");
            migrationBuilder.Sql("DROP INDEX ux_accounts_id_owner;");
        }
    }
}
