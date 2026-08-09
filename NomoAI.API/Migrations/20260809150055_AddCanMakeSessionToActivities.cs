using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NomoAI.API.Migrations
{
    /// <inheritdoc />
    public partial class AddCanMakeSessionToActivities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Production may already have this column from an older branch migration
            // (20260808151935_addCanMakeSessionBoolean) that is not present in this repo.
            migrationBuilder.Sql(
                """
                IF COL_LENGTH(N'Activities', N'CanMakeSession') IS NULL
                BEGIN
                    ALTER TABLE [Activities]
                    ADD [CanMakeSession] bit NOT NULL
                        CONSTRAINT [DF_Activities_CanMakeSession] DEFAULT CAST(1 AS bit);
                END
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                IF COL_LENGTH(N'Activities', N'CanMakeSession') IS NOT NULL
                BEGIN
                    DECLARE @constraint sysname;
                    SELECT @constraint = dc.name
                    FROM sys.default_constraints dc
                    INNER JOIN sys.columns c
                        ON c.default_object_id = dc.object_id
                    WHERE dc.parent_object_id = OBJECT_ID(N'Activities')
                      AND c.name = N'CanMakeSession';

                    IF @constraint IS NOT NULL
                        EXEC(N'ALTER TABLE [Activities] DROP CONSTRAINT [' + @constraint + N']');

                    ALTER TABLE [Activities] DROP COLUMN [CanMakeSession];
                END
                """);
        }
    }
}
