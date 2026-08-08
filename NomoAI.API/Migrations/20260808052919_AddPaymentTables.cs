using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NomoAI.API.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DoctorCreditWallet",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DoctorId = table.Column<int>(type: "int", nullable: false),
                    AvailableMinutes = table.Column<int>(type: "int", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DoctorCreditWallet", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DoctorCreditWallet_Doctor_DoctorId",
                        column: x => x.DoctorId,
                        principalTable: "Doctor",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                });

            migrationBuilder.CreateTable(
                name: "Payment",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DoctorId = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Currency = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ReferenceId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Provider = table.Column<int>(type: "int", nullable: false),
                    PaidAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Payment", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Payment_Doctor_DoctorId",
                        column: x => x.DoctorId,
                        principalTable: "Doctor",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                });

            migrationBuilder.CreateTable(
                name: "SupscriptionPlan",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IncludedMinutes = table.Column<int>(type: "int", nullable: false),
                    Price = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Currency = table.Column<int>(type: "int", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupscriptionPlan", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DoctorPlanPurchase",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DoctorId = table.Column<int>(type: "int", nullable: false),
                    PlanId = table.Column<int>(type: "int", nullable: false),
                    PaymentId = table.Column<int>(type: "int", nullable: false),
                    PurchasedMinutes = table.Column<int>(type: "int", nullable: false),
                    PurchasedPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PurchasedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DoctorPlanPurchase", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DoctorPlanPurchase_Doctor_DoctorId",
                        column: x => x.DoctorId,
                        principalTable: "Doctor",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                    table.ForeignKey(
                        name: "FK_DoctorPlanPurchase_Payment_PaymentId",
                        column: x => x.PaymentId,
                        principalTable: "Payment",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                    table.ForeignKey(
                        name: "FK_DoctorPlanPurchase_SupscriptionPlan_PlanId",
                        column: x => x.PlanId,
                        principalTable: "SupscriptionPlan",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                });

            migrationBuilder.CreateTable(
                name: "DoctorTransaction",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DoctorId = table.Column<int>(type: "int", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Minutes = table.Column<int>(type: "int", nullable: false),
                    BalanceAfter = table.Column<int>(type: "int", nullable: false),
                    PlanPurchaseId = table.Column<int>(type: "int", nullable: true),
                    SessionId = table.Column<int>(type: "int", nullable: true),
                    PlanId = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DoctorTransaction", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DoctorTransaction_DoctorPlanPurchase_PlanId",
                        column: x => x.PlanId,
                        principalTable: "DoctorPlanPurchase",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_DoctorTransaction_Doctor_DoctorId",
                        column: x => x.DoctorId,
                        principalTable: "Doctor",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                    table.ForeignKey(
                        name: "FK_DoctorTransaction_Sessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "Sessions",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_DoctorCreditWallet_DoctorId",
                table: "DoctorCreditWallet",
                column: "DoctorId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DoctorPlanPurchase_DoctorId",
                table: "DoctorPlanPurchase",
                column: "DoctorId");

            migrationBuilder.CreateIndex(
                name: "IX_DoctorPlanPurchase_PaymentId",
                table: "DoctorPlanPurchase",
                column: "PaymentId");

            migrationBuilder.CreateIndex(
                name: "IX_DoctorPlanPurchase_PlanId",
                table: "DoctorPlanPurchase",
                column: "PlanId");

            migrationBuilder.CreateIndex(
                name: "IX_DoctorTransaction_DoctorId",
                table: "DoctorTransaction",
                column: "DoctorId");

            migrationBuilder.CreateIndex(
                name: "IX_DoctorTransaction_PlanId",
                table: "DoctorTransaction",
                column: "PlanId");

            migrationBuilder.CreateIndex(
                name: "IX_DoctorTransaction_SessionId",
                table: "DoctorTransaction",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_Payment_DoctorId",
                table: "Payment",
                column: "DoctorId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DoctorCreditWallet");

            migrationBuilder.DropTable(
                name: "DoctorTransaction");

            migrationBuilder.DropTable(
                name: "DoctorPlanPurchase");

            migrationBuilder.DropTable(
                name: "Payment");

            migrationBuilder.DropTable(
                name: "SupscriptionPlan");
        }
    }
}
