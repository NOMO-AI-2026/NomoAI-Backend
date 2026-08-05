using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NomoAI.API.Migrations
{
    /// <inheritdoc />
    public partial class AddSessionIsDoctorReviewed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsDoctorReviewed",
                table: "Sessions",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_Status_IsDoctorReviewed_IsDeleted",
                table: "Sessions",
                columns: new[] { "Status", "IsDoctorReviewed", "IsDeleted" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Sessions_Status_IsDoctorReviewed_IsDeleted",
                table: "Sessions");

            migrationBuilder.DropColumn(
                name: "IsDoctorReviewed",
                table: "Sessions");
        }
    }
}
