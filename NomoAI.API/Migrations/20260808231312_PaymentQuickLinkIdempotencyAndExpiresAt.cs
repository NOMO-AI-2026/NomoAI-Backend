using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NomoAI.API.Migrations
{
    /// <inheritdoc />
    public partial class PaymentQuickLinkIdempotencyAndExpiresAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Idempotency",
                table: "PaymentQuickLink",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<DateTime>(
                name: "ExpiresAt",
                table: "PaymentQuickLink",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PaymentQuickLink_Idempotency",
                table: "PaymentQuickLink",
                column: "Idempotency",
                unique: true,
                filter: "[IsDeleted] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PaymentQuickLink_Idempotency",
                table: "PaymentQuickLink");

            migrationBuilder.DropColumn(
                name: "ExpiresAt",
                table: "PaymentQuickLink");

            migrationBuilder.AlterColumn<string>(
                name: "Idempotency",
                table: "PaymentQuickLink",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);
        }
    }
}
