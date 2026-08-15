using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NomoAI.API.Migrations
{
    /// <inheritdoc />
    public partial class AddDoctorVerificationDocuments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "IdentityDocumentUrl",
                table: "Doctor",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PracticeLicenseUrl",
                table: "Doctor",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SyndicateCardUrl",
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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IdentityDocumentUrl",
                table: "Doctor");

            migrationBuilder.DropColumn(
                name: "PracticeLicenseUrl",
                table: "Doctor");

            migrationBuilder.DropColumn(
                name: "SyndicateCardUrl",
                table: "Doctor");

            migrationBuilder.DropColumn(
                name: "SyndicateRegistrationNumber",
                table: "Doctor");
        }
    }
}
