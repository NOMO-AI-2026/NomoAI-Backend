using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NomoAI.API.Migrations
{
    /// <inheritdoc />
    public partial class AddSessionRuntimePersistence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AttemptEvaluations_SessionAttempts_AttemptId1",
                table: "AttemptEvaluations");

            migrationBuilder.DropIndex(
                name: "IX_AttemptEvaluations_AttemptId1",
                table: "AttemptEvaluations");

            migrationBuilder.DropColumn(
                name: "AttemptId1",
                table: "AttemptEvaluations");

            migrationBuilder.AddColumn<string>(
                name: "ActivityType",
                table: "Sessions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ChildAge",
                table: "Sessions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CurrentAttemptNumber",
                table: "Sessions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CurrentStepNumber",
                table: "Sessions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "KnowledgeChunkIdsJson",
                table: "Sessions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "KnowledgeSourceIdsJson",
                table: "Sessions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Language",
                table: "Sessions",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "PlanGeneratedAt",
                table: "Sessions",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PlanJson",
                table: "Sessions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PlanModel",
                table: "Sessions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "PlanUsedFallback",
                table: "Sessions",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Prompt",
                table: "Sessions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "RequiresDoctorReview",
                table: "Sessions",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "SpeechLevel",
                table: "Sessions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "AudioUrl",
                table: "SessionAttempts",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<string>(
                name: "NormalizedText",
                table: "AttemptTranscribtions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "AttemptId",
                table: "AttemptEvaluations",
                type: "int",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AddColumn<string>(
                name: "AdaptiveAction",
                table: "AttemptEvaluations",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AvatarEmotion",
                table: "AttemptEvaluations",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AvatarSpokenText",
                table: "AttemptEvaluations",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EvaluationJson",
                table: "AttemptEvaluations",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "KnowledgeChunkIdsJson",
                table: "AttemptEvaluations",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "KnowledgeSourceIdsJson",
                table: "AttemptEvaluations",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Matched",
                table: "AttemptEvaluations",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NormalizedTranscript",
                table: "AttemptEvaluations",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SpeechOutcome",
                table: "AttemptEvaluations",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SessionAttempts_SessionId_AttemptNumber",
                table: "SessionAttempts",
                columns: new[] { "SessionId", "AttemptNumber" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_AttemptEvaluations_AttemptId",
                table: "AttemptEvaluations",
                column: "AttemptId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_AttemptEvaluations_SessionAttempts_AttemptId",
                table: "AttemptEvaluations",
                column: "AttemptId",
                principalTable: "SessionAttempts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SessionAttempts_Sessions_SessionId",
                table: "SessionAttempts",
                column: "SessionId",
                principalTable: "Sessions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AttemptEvaluations_SessionAttempts_AttemptId",
                table: "AttemptEvaluations");

            migrationBuilder.DropForeignKey(
                name: "FK_SessionAttempts_Sessions_SessionId",
                table: "SessionAttempts");

            migrationBuilder.DropIndex(
                name: "IX_SessionAttempts_SessionId_AttemptNumber",
                table: "SessionAttempts");

            migrationBuilder.DropIndex(
                name: "IX_AttemptEvaluations_AttemptId",
                table: "AttemptEvaluations");

            migrationBuilder.DropColumn(
                name: "ActivityType",
                table: "Sessions");

            migrationBuilder.DropColumn(
                name: "ChildAge",
                table: "Sessions");

            migrationBuilder.DropColumn(
                name: "CurrentAttemptNumber",
                table: "Sessions");

            migrationBuilder.DropColumn(
                name: "CurrentStepNumber",
                table: "Sessions");

            migrationBuilder.DropColumn(
                name: "KnowledgeChunkIdsJson",
                table: "Sessions");

            migrationBuilder.DropColumn(
                name: "KnowledgeSourceIdsJson",
                table: "Sessions");

            migrationBuilder.DropColumn(
                name: "Language",
                table: "Sessions");

            migrationBuilder.DropColumn(
                name: "PlanGeneratedAt",
                table: "Sessions");

            migrationBuilder.DropColumn(
                name: "PlanJson",
                table: "Sessions");

            migrationBuilder.DropColumn(
                name: "PlanModel",
                table: "Sessions");

            migrationBuilder.DropColumn(
                name: "PlanUsedFallback",
                table: "Sessions");

            migrationBuilder.DropColumn(
                name: "Prompt",
                table: "Sessions");

            migrationBuilder.DropColumn(
                name: "RequiresDoctorReview",
                table: "Sessions");

            migrationBuilder.DropColumn(
                name: "SpeechLevel",
                table: "Sessions");

            migrationBuilder.DropColumn(
                name: "NormalizedText",
                table: "AttemptTranscribtions");

            migrationBuilder.DropColumn(
                name: "AdaptiveAction",
                table: "AttemptEvaluations");

            migrationBuilder.DropColumn(
                name: "AvatarEmotion",
                table: "AttemptEvaluations");

            migrationBuilder.DropColumn(
                name: "AvatarSpokenText",
                table: "AttemptEvaluations");

            migrationBuilder.DropColumn(
                name: "EvaluationJson",
                table: "AttemptEvaluations");

            migrationBuilder.DropColumn(
                name: "KnowledgeChunkIdsJson",
                table: "AttemptEvaluations");

            migrationBuilder.DropColumn(
                name: "KnowledgeSourceIdsJson",
                table: "AttemptEvaluations");

            migrationBuilder.DropColumn(
                name: "Matched",
                table: "AttemptEvaluations");

            migrationBuilder.DropColumn(
                name: "NormalizedTranscript",
                table: "AttemptEvaluations");

            migrationBuilder.DropColumn(
                name: "SpeechOutcome",
                table: "AttemptEvaluations");

            migrationBuilder.AlterColumn<string>(
                name: "AudioUrl",
                table: "SessionAttempts",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "AttemptId",
                table: "AttemptEvaluations",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<int>(
                name: "AttemptId1",
                table: "AttemptEvaluations",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_AttemptEvaluations_AttemptId1",
                table: "AttemptEvaluations",
                column: "AttemptId1");

            migrationBuilder.AddForeignKey(
                name: "FK_AttemptEvaluations_SessionAttempts_AttemptId1",
                table: "AttemptEvaluations",
                column: "AttemptId1",
                principalTable: "SessionAttempts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
