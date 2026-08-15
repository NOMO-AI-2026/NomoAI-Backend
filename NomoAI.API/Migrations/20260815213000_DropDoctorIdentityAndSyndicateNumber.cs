using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NomoAI.API.Migrations
{
    /// <inheritdoc />
    public partial class DropDoctorIdentityAndSyndicateNumber : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IdentityDocumentUrl",
                table: "Doctor");

            migrationBuilder.DropColumn(
                name: "SyndicateRegistrationNumber",
                table: "Doctor");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "IdentityDocumentUrl",
                table: "Doctor",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SyndicateRegistrationNumber",
                table: "Doctor",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);
        }
    }
}
