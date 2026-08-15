using NomoAI.API.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NomoDoc.Domain.Entities
{
    public class AttemptEvaluation:BaseEntity<int>
    {

        /// <summary>FK to SessionAttempts.Id (real int FK; previously a non-FK decimal column).</summary>
        public int AttemptId { get; set; }

        public decimal AccuracyScore { get; set; }

        /// <summary>Null when timing evidence was insufficient (Phase 2). Null ≠ 0.</summary>
        public decimal? FluencyScore { get; set; }

        /// <summary>Null when acoustic/phoneme evidence was absent (Phase 2). Null ≠ 0.</summary>
        public decimal? PronunciationScore { get; set; }

        public decimal CompletenessScore { get; set; }

        /// <summary>
        /// Evidence-safe overall from available components only (null fluency/pronunciation omitted).
        /// Prefer EvaluationJson scores.overall when present.
        /// </summary>
        public decimal? OverallScore
        {
            get
            {
                decimal sum = AccuracyScore + CompletenessScore;
                int count = 2;
                if (FluencyScore is decimal fluency)
                {
                    sum += fluency;
                    count++;
                }

                if (PronunciationScore is decimal pronunciation)
                {
                    sum += pronunciation;
                    count++;
                }

                return count == 0 ? null : Math.Round(sum / count, 2);
            }
        }

        public string? Feedback { get; set; }

        public bool IsSuccessful { get; set; }

        /// <summary>AdaptiveAction value returned by AI Core, e.g. "advance", "retry_same".</summary>
        public string? AdaptiveAction { get; set; }

        public string? AvatarSpokenText { get; set; }

        public string? AvatarEmotion { get; set; }

        /// <summary>scored | no_speech | empty_transcription.</summary>
        public string? SpeechOutcome { get; set; }

        public bool? Matched { get; set; }

        public string? NormalizedTranscript { get; set; }

        /// <summary>Full AiEvaluateAttemptV2Response snapshot for this attempt.</summary>
        public string? EvaluationJson { get; set; }

        public string? KnowledgeSourceIdsJson { get; set; }

        public string? KnowledgeChunkIdsJson { get; set; }

        public SessionAttempts Attempt { get; set; } = null!;


    }
}
