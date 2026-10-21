using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinTrack.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCreditCardInvoices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "InvoiceId",
                table: "installments",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "credit_card_invoices",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    credit_card_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    closing_date = table.Column<DateOnly>(type: "date", nullable: false),
                    due_date = table.Column<DateOnly>(type: "date", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_credit_card_invoices", x => x.id);
                    table.CheckConstraint("ck_credit_card_invoices_dates", "due_date > closing_date");
                    table.ForeignKey(
                        name: "FK_credit_card_invoices_credit_cards_credit_card_id_user_id",
                        columns: x => new { x.credit_card_id, x.user_id },
                        principalTable: "credit_cards",
                        principalColumns: new[] { "id", "user_id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_installments_InvoiceId",
                table: "installments",
                column: "InvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_credit_card_invoices_credit_card_id_user_id",
                table: "credit_card_invoices",
                columns: new[] { "credit_card_id", "user_id" });

            migrationBuilder.CreateIndex(
                name: "ux_credit_card_invoices_cycle",
                table: "credit_card_invoices",
                columns: new[] { "credit_card_id", "closing_date", "due_date" },
                unique: true);

            // Upgrade phase-15 data without changing any installment amount or date.
            migrationBuilder.Sql("""
                INSERT INTO credit_card_invoices (id, credit_card_id, user_id, closing_date, due_date)
                SELECT gen_random_uuid(), cycles.card_id, cycles.user_id, cycles.closing_date, cycles.due_date
                FROM (
                    SELECT DISTINCT p."CreditCardId" AS card_id, p."UserId" AS user_id,
                        i."ClosingDate" AS closing_date, i."DueDate" AS due_date
                    FROM installments i JOIN purchases p ON p."Id" = i."PurchaseId"
                ) cycles;
                UPDATE installments i SET "InvoiceId" = invoice.id
                FROM purchases p, credit_card_invoices invoice
                WHERE p."Id" = i."PurchaseId" AND invoice.credit_card_id = p."CreditCardId"
                    AND invoice.user_id = p."UserId" AND invoice.closing_date = i."ClosingDate"
                    AND invoice.due_date = i."DueDate";
                """);
            migrationBuilder.AlterColumn<Guid>(
                name: "InvoiceId", table: "installments", type: "uuid", nullable: false,
                oldClrType: typeof(Guid), oldType: "uuid", oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_installments_credit_card_invoices_InvoiceId",
                table: "installments",
                column: "InvoiceId",
                principalTable: "credit_card_invoices",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_installments_credit_card_invoices_InvoiceId",
                table: "installments");

            migrationBuilder.DropTable(
                name: "credit_card_invoices");

            migrationBuilder.DropIndex(
                name: "IX_installments_InvoiceId",
                table: "installments");

            migrationBuilder.DropColumn(
                name: "InvoiceId",
                table: "installments");
        }
    }
}
