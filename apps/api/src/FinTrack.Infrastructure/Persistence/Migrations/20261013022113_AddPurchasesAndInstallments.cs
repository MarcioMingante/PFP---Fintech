using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinTrack.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPurchasesAndInstallments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddUniqueConstraint(
                name: "AK_credit_cards_id_user_id",
                table: "credit_cards",
                columns: new[] { "id", "user_id" });

            migrationBuilder.CreateTable(
                name: "purchases",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreditCardId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    IdempotencyKey = table.Column<Guid>(type: "uuid", nullable: false),
                    Description = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    InstallmentCount = table.Column<int>(type: "integer", nullable: false),
                    PurchaseDate = table.Column<DateOnly>(type: "date", nullable: false),
                    ClosingDay = table.Column<int>(type: "integer", nullable: false),
                    DueDay = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_purchases", x => x.Id);
                    table.CheckConstraint("ck_purchases_amount", "\"Amount\" > 0");
                    table.CheckConstraint("ck_purchases_count", "\"InstallmentCount\" BETWEEN 1 AND 120");
                    table.ForeignKey(
                        name: "FK_purchases_credit_cards_CreditCardId_UserId",
                        columns: x => new { x.CreditCardId, x.UserId },
                        principalTable: "credit_cards",
                        principalColumns: new[] { "id", "user_id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "installments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PurchaseId = table.Column<Guid>(type: "uuid", nullable: false),
                    Number = table.Column<int>(type: "integer", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    ClosingDate = table.Column<DateOnly>(type: "date", nullable: false),
                    DueDate = table.Column<DateOnly>(type: "date", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_installments", x => x.Id);
                    table.CheckConstraint("ck_installments_amount", "\"Amount\" > 0");
                    table.CheckConstraint("ck_installments_dates", "\"DueDate\" > \"ClosingDate\"");
                    table.CheckConstraint("ck_installments_number", "\"Number\" BETWEEN 1 AND 120");
                    table.ForeignKey(
                        name: "FK_installments_purchases_PurchaseId",
                        column: x => x.PurchaseId,
                        principalTable: "purchases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_installments_PurchaseId_Number",
                table: "installments",
                columns: new[] { "PurchaseId", "Number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_purchases_CreditCardId_IdempotencyKey",
                table: "purchases",
                columns: new[] { "CreditCardId", "IdempotencyKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_purchases_CreditCardId_UserId",
                table: "purchases",
                columns: new[] { "CreditCardId", "UserId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "installments");

            migrationBuilder.DropTable(
                name: "purchases");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_credit_cards_id_user_id",
                table: "credit_cards");
        }
    }
}
