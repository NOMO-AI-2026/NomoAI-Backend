using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NomoAI.API.Migrations
{
    /// <inheritdoc />
    public partial class addCanMakeSessionBoolean : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "canMakeSession",
                table: "Activities",
                type: "bit",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "canMakeSession",
                table: "Activities");
        }
    }
}
