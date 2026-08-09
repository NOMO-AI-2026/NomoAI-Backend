using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NomoAI.API.Migrations
{
    /// <inheritdoc />
    public partial class FixPlanPurchaseFk : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DoctorTransaction_DoctorPlanPurchase_PlanId",
                table: "DoctorTransaction");

            migrationBuilder.DropIndex(
                name: "IX_DoctorTransaction_PlanId",
                table: "DoctorTransaction");

            migrationBuilder.DropColumn(
                name: "PlanId",
                table: "DoctorTransaction");

            migrationBuilder.CreateIndex(
                name: "IX_DoctorTransaction_PlanPurchaseId",
                table: "DoctorTransaction",
                column: "PlanPurchaseId");

            migrationBuilder.AddForeignKey(
                name: "FK_DoctorTransaction_DoctorPlanPurchase_PlanPurchaseId",
                table: "DoctorTransaction",
                column: "PlanPurchaseId",
                principalTable: "DoctorPlanPurchase",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DoctorTransaction_DoctorPlanPurchase_PlanPurchaseId",
                table: "DoctorTransaction");

            migrationBuilder.DropIndex(
                name: "IX_DoctorTransaction_PlanPurchaseId",
                table: "DoctorTransaction");

            migrationBuilder.AddColumn<int>(
                name: "PlanId",
                table: "DoctorTransaction",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_DoctorTransaction_PlanId",
                table: "DoctorTransaction",
                column: "PlanId");

            migrationBuilder.AddForeignKey(
                name: "FK_DoctorTransaction_DoctorPlanPurchase_PlanId",
                table: "DoctorTransaction",
                column: "PlanId",
                principalTable: "DoctorPlanPurchase",
                principalColumn: "Id");
        }
    }
}
