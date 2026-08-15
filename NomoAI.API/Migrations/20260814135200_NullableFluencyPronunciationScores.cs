using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NomoAI.API.Migrations
{
    /// <inheritdoc />
    public partial class NullableFluencyPronunciationScores : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "PronunciationScore",
                table: "AttemptEvaluations",
                type: "decimal(18,2)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "FluencyScore",
                table: "AttemptEvaluations",
                type: "decimal(18,2)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            // SAFE legacy repair only — never wipe every zero.
            // Clear DB zeros only when EvaluationJson explicitly proves missing evidence.
            migrationBuilder.Sql(
                """
                UPDATE AttemptEvaluations
                SET FluencyScore = NULL
                WHERE FluencyScore = 0
                  AND IsDeleted = 0
                  AND EvaluationJson IS NOT NULL
                  AND (
                        EvaluationJson LIKE '%"fluency":null%'
                     OR EvaluationJson LIKE '%"fluency": null%'
                     OR EvaluationJson LIKE '%"fluencyScore":null%'
                     OR EvaluationJson LIKE '%"fluencyScore": null%'
                     OR EvaluationJson LIKE '%insufficient_fluency_evidence%'
                  );

                UPDATE AttemptEvaluations
                SET PronunciationScore = NULL
                WHERE PronunciationScore = 0
                  AND IsDeleted = 0
                  AND EvaluationJson IS NOT NULL
                  AND (
                        EvaluationJson LIKE '%"pronunciation":null%'
                     OR EvaluationJson LIKE '%"pronunciation": null%'
                     OR EvaluationJson LIKE '%"pronunciationProxyScore":null%'
                     OR EvaluationJson LIKE '%"pronunciationProxyScore": null%'
                     OR EvaluationJson LIKE '%insufficient_pronunciation_evidence%'
                  );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                UPDATE AttemptEvaluations SET FluencyScore = 0 WHERE FluencyScore IS NULL;
                UPDATE AttemptEvaluations SET PronunciationScore = 0 WHERE PronunciationScore IS NULL;
                """);

            migrationBuilder.AlterColumn<decimal>(
                name: "PronunciationScore",
                table: "AttemptEvaluations",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "FluencyScore",
                table: "AttemptEvaluations",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldNullable: true);
        }
    }
}
