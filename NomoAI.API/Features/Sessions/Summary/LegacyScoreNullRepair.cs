using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NomoAI.API.Persistence;
using NomoDoc.Domain.Entities;

namespace NomoAI.API.Features.Sessions.Summary;

/// <summary>
/// App-level legacy repair for coerced fluency/pronunciation zeros.
/// Only clears DB zeros when EvaluationJson explicitly proves missing evidence.
/// </summary>
internal static class LegacyScoreNullRepair
{
    public sealed record RepairResult(int FluencyRepaired, int PronunciationRepaired, int PreservedZeros);

    public static RepairResult ApplyInMemory(IEnumerable<AttemptEvaluation> rows)
    {
        int fluency = 0;
        int pronunciation = 0;
        int preserved = 0;

        foreach (AttemptEvaluation row in rows)
        {
            bool touched = false;

            if (row.FluencyScore == 0m && ShouldClearFluency(row.EvaluationJson))
            {
                row.FluencyScore = null;
                fluency++;
                touched = true;
            }
            else if (row.FluencyScore == 0m)
            {
                preserved++;
            }

            if (row.PronunciationScore == 0m && ShouldClearPronunciation(row.EvaluationJson))
            {
                row.PronunciationScore = null;
                pronunciation++;
                touched = true;
            }
            else if (row.PronunciationScore == 0m && !touched)
            {
                // count preserved pronunciation zeros separately only when fluency wasn't already counted
                if (row.FluencyScore != 0m)
                {
                    preserved++;
                }
            }
        }

        return new RepairResult(fluency, pronunciation, preserved);
    }

    public static async Task<RepairResult> ApplyToDatabaseAsync(
        AppDbContext db,
        ILogger? logger,
        CancellationToken cancellationToken)
    {
        List<AttemptEvaluation> candidates = await db.AttemptEvaluations
            .Where(e => !e.IsDeleted
                        && (e.FluencyScore == 0m || e.PronunciationScore == 0m)
                        && e.EvaluationJson != null)
            .ToListAsync(cancellationToken);

        RepairResult result = ApplyInMemory(candidates);
        if (result.FluencyRepaired + result.PronunciationRepaired > 0)
        {
            await db.SaveChangesAsync(cancellationToken);
        }

        logger?.LogInformation(
            "LegacyScoreNullRepair FluencyRepaired={FluencyRepaired} PronunciationRepaired={PronunciationRepaired} PreservedZeros={PreservedZeros}",
            result.FluencyRepaired,
            result.PronunciationRepaired,
            result.PreservedZeros);

        return result;
    }

    internal static bool ShouldClearFluency(string? evaluationJson)
    {
        if (string.IsNullOrWhiteSpace(evaluationJson))
        {
            return false;
        }

        if (ContainsInsensitive(evaluationJson, "insufficient_fluency_evidence"))
        {
            return true;
        }

        if (ContainsExplicitNull(evaluationJson, "fluency")
            || ContainsExplicitNull(evaluationJson, "fluencyScore"))
        {
            return true;
        }

        // Snapshot scores present but fluency omitted after nullable serialize = missing evidence.
        return IsComponentOmittedWhileScoresPresent(evaluationJson, fluency: true);
    }

    internal static bool ShouldClearPronunciation(string? evaluationJson)
    {
        if (string.IsNullOrWhiteSpace(evaluationJson))
        {
            return false;
        }

        if (ContainsInsensitive(evaluationJson, "insufficient_pronunciation_evidence"))
        {
            return true;
        }

        if (ContainsExplicitNull(evaluationJson, "pronunciation")
            || ContainsExplicitNull(evaluationJson, "pronunciationProxyScore"))
        {
            return true;
        }

        return IsComponentOmittedWhileScoresPresent(evaluationJson, fluency: false);
    }

    private static bool IsComponentOmittedWhileScoresPresent(string evaluationJson, bool fluency)
    {
        try
        {
            using JsonDocument doc = JsonDocument.Parse(evaluationJson);
            JsonElement root = doc.RootElement;

            if (TryGetScoresObject(root, out JsonElement scores))
            {
                string prop = fluency ? "fluency" : "pronunciation";
                string alt = fluency ? "fluencyScore" : "pronunciationProxyScore";

                if (TryGetPropertyIgnoreCase(scores, prop, out JsonElement value)
                    || TryGetPropertyIgnoreCase(scores, alt, out value))
                {
                    return value.ValueKind == JsonValueKind.Null;
                }

                // Property omitted while scores object exists → treat as missing evidence
                // only when sibling evidence fields exist (accuracy/completeness/overall).
                bool hasSibling =
                    TryGetPropertyIgnoreCase(scores, "accuracy", out _)
                    || TryGetPropertyIgnoreCase(scores, "accuracyScore", out _)
                    || TryGetPropertyIgnoreCase(scores, "completeness", out _)
                    || TryGetPropertyIgnoreCase(scores, "completenessScore", out _)
                    || TryGetPropertyIgnoreCase(scores, "overall", out _)
                    || TryGetPropertyIgnoreCase(scores, "overallScore", out _);

                return hasSibling;
            }
        }
        catch (JsonException)
        {
            return false;
        }

        return false;
    }

    private static bool TryGetScoresObject(JsonElement root, out JsonElement scores)
    {
        if (TryGetPropertyIgnoreCase(root, "scores", out scores)
            && scores.ValueKind == JsonValueKind.Object)
        {
            return true;
        }

        if (TryGetPropertyIgnoreCase(root, "speechAnalysis", out JsonElement analysis)
            && analysis.ValueKind == JsonValueKind.Object
            && TryGetPropertyIgnoreCase(analysis, "scores", out scores)
            && scores.ValueKind == JsonValueKind.Object)
        {
            return true;
        }

        scores = default;
        return false;
    }

    private static bool TryGetPropertyIgnoreCase(JsonElement element, string name, out JsonElement value)
    {
        foreach (JsonProperty property in element.EnumerateObject())
        {
            if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                value = property.Value;
                return true;
            }
        }

        value = default;
        return false;
    }

    private static bool ContainsExplicitNull(string json, string propertyName)
    {
        // Match `"property":null` / `"property": null` without matching numeric zero.
        return ContainsInsensitive(json, $"\"{propertyName}\":null")
               || ContainsInsensitive(json, $"\"{propertyName}\": null");
    }

    private static bool ContainsInsensitive(string haystack, string needle) =>
        haystack.Contains(needle, StringComparison.OrdinalIgnoreCase);
}
