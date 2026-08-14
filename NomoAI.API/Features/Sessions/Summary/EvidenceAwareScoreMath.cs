namespace NomoAI.API.Features.Sessions.Summary;

/// <summary>
/// Evidence-safe score helpers: missing fluency/pronunciation must not become zero.
/// </summary>
internal static class EvidenceAwareScoreMath
{
    /// <summary>
    /// Average only present components (0–100 scale). Null ≠ zero.
    /// </summary>
    public static double? AverageAvailable(
        double? accuracy,
        double? completeness,
        double? fluency,
        double? pronunciation)
    {
        var parts = new List<double>(4);
        if (accuracy is double a)
        {
            parts.Add(a);
        }

        if (completeness is double c)
        {
            parts.Add(c);
        }

        if (fluency is double f)
        {
            parts.Add(f);
        }

        if (pronunciation is double p)
        {
            parts.Add(p);
        }

        if (parts.Count == 0)
        {
            return null;
        }

        return Math.Round(parts.Average(), 2);
    }

    public static decimal? AverageAvailable(
        decimal? accuracy,
        decimal? completeness,
        decimal? fluency,
        decimal? pronunciation)
    {
        double? value = AverageAvailable(
            accuracy is null ? null : (double)accuracy.Value,
            completeness is null ? null : (double)completeness.Value,
            fluency is null ? null : (double)fluency.Value,
            pronunciation is null ? null : (double)pronunciation.Value);
        return value is null ? null : Convert.ToDecimal(value.Value);
    }

    /// <summary>
    /// Prefer AI overall; otherwise average available components only.
    /// </summary>
    public static double? ResolveOverall(
        double? aiOverall,
        double? accuracy,
        double? completeness,
        double? fluency,
        double? pronunciation)
    {
        if (aiOverall is double overall)
        {
            return NormalizeScore(overall);
        }

        return NormalizeScore(AverageAvailable(accuracy, completeness, fluency, pronunciation));
    }

    /// <summary>If scores look like 0–1 ratios, lift them to 0–100.</summary>
    public static double? NormalizeScore(double? score)
    {
        if (score is null)
        {
            return null;
        }

        double value = score.Value;
        if (value is >= 0 and <= 1.0)
        {
            value *= 100;
        }

        return Math.Clamp(Math.Round(value, 2), 0, 100);
    }
}
