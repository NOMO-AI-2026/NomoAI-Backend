using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NomoAI.API.Migrations
{
    /// <inheritdoc />
    public partial class resolveActivityConflict : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "EXEC sp_rename 'Activities.canMakeSession', 'CanMakeSession_tmp', 'COLUMN';");
            migrationBuilder.Sql(
                "EXEC sp_rename 'Activities.CanMakeSession_tmp', 'CanMakeSession', 'COLUMN';");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "EXEC sp_rename 'Activities.CanMakeSession', 'canMakeSession_tmp', 'COLUMN';");
            migrationBuilder.Sql(
                "EXEC sp_rename 'Activities.canMakeSession_tmp', 'canMakeSession', 'COLUMN';");
        }
    }
}