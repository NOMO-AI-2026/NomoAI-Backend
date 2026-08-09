using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NomoAI.API.Migrations
{
    /// <inheritdoc />
    public partial class DoctorCreditWalletFilteredUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DoctorCreditWallet_DoctorId",
                table: "DoctorCreditWallet");

            migrationBuilder.CreateIndex(
                name: "IX_DoctorCreditWallet_DoctorId",
                table: "DoctorCreditWallet",
                column: "DoctorId",
                unique: true,
                filter: "[IsDeleted] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DoctorCreditWallet_DoctorId",
                table: "DoctorCreditWallet");

            migrationBuilder.CreateIndex(
                name: "IX_DoctorCreditWallet_DoctorId",
                table: "DoctorCreditWallet",
                column: "DoctorId",
                unique: true);
        }
    }
}
