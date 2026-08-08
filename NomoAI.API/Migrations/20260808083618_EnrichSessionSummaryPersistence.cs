using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NomoAI.API.Migrations
{
    /// <inheritdoc />
    public partial class EnrichSessionSummaryPersistence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SessionSummaries_Sessions_SessionId",
                table: "SessionSummaries");

            migrationBuilder.DropIndex(
                name: "IX_SessionSummaries_SessionId",
                table: "SessionSummaries");

            migrationBuilder.AlterColumn<string>(
                name: "Recommendations",
                table: "SessionSummaries",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "AISummary",
                table: "SessionSummaries",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<string>(
                name: "DoctorReviewNote",
                table: "SessionSummaries",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "DoctorReviewRecommended",
                table: "SessionSummaries",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<double>(
                name: "DurationSeconds",
                table: "SessionSummaries",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FinalAdaptiveAction",
                table: "SessionSummaries",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "FinalOverallScore",
                table: "SessionSummaries",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "FirstOverallScore",
                table: "SessionSummaries",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "GeneratedAt",
                table: "SessionSummaries",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "InterventionCount",
                table: "SessionSummaries",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ModelName",
                table: "SessionSummaries",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "NoSpeechAttempts",
                table: "SessionSummaries",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Outcome",
                table: "SessionSummaries",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RawResponseJson",
                table: "SessionSummaries",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RulesVersion",
                table: "SessionSummaries",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ScoreTrend",
                table: "SessionSummaries",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SummaryGenerationMode",
                table: "SessionSummaries",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "UsedFallback",
                table: "SessionSummaries",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_SessionSummaries_SessionId",
                table: "SessionSummaries",
                column: "SessionId",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.AddForeignKey(
                name: "FK_SessionSummaries_Sessions_SessionId",
                table: "SessionSummaries",
                column: "SessionId",
                principalTable: "Sessions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SessionSummaries_Sessions_SessionId",
                table: "SessionSummaries");

            migrationBuilder.DropIndex(
                name: "IX_SessionSummaries_SessionId",
                table: "SessionSummaries");

            migrationBuilder.DropColumn(
                name: "DoctorReviewNote",
                table: "SessionSummaries");

            migrationBuilder.DropColumn(
                name: "DoctorReviewRecommended",
                table: "SessionSummaries");

            migrationBuilder.DropColumn(
                name: "DurationSeconds",
                table: "SessionSummaries");

            migrationBuilder.DropColumn(
                name: "FinalAdaptiveAction",
                table: "SessionSummaries");

            migrationBuilder.DropColumn(
                name: "FinalOverallScore",
                table: "SessionSummaries");

            migrationBuilder.DropColumn(
                name: "FirstOverallScore",
                table: "SessionSummaries");

            migrationBuilder.DropColumn(
                name: "GeneratedAt",
                table: "SessionSummaries");

            migrationBuilder.DropColumn(
                name: "InterventionCount",
                table: "SessionSummaries");

            migrationBuilder.DropColumn(
                name: "ModelName",
                table: "SessionSummaries");

            migrationBuilder.DropColumn(
                name: "NoSpeechAttempts",
                table: "SessionSummaries");

            migrationBuilder.DropColumn(
                name: "Outcome",
                table: "SessionSummaries");

            migrationBuilder.DropColumn(
                name: "RawResponseJson",
                table: "SessionSummaries");

            migrationBuilder.DropColumn(
                name: "RulesVersion",
                table: "SessionSummaries");

            migrationBuilder.DropColumn(
                name: "ScoreTrend",
                table: "SessionSummaries");

            migrationBuilder.DropColumn(
                name: "SummaryGenerationMode",
                table: "SessionSummaries");

            migrationBuilder.DropColumn(
                name: "UsedFallback",
                table: "SessionSummaries");

            migrationBuilder.AlterColumn<string>(
                name: "Recommendations",
                table: "SessionSummaries",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(1000)",
                oldMaxLength: 1000);

            migrationBuilder.AlterColumn<string>(
                name: "AISummary",
                table: "SessionSummaries",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(2000)",
                oldMaxLength: 2000);

            migrationBuilder.CreateIndex(
                name: "IX_SessionSummaries_SessionId",
                table: "SessionSummaries",
                column: "SessionId");

            migrationBuilder.AddForeignKey(
                name: "FK_SessionSummaries_Sessions_SessionId",
                table: "SessionSummaries",
                column: "SessionId",
                principalTable: "Sessions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
