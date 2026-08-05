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

        public decimal FluencyScore { get; set; }

        public decimal PronunciationScore { get; set; }

        public decimal CompletenessScore { get; set; }

        public decimal OverallScore => AccuracyScore + FluencyScore + PronunciationScore + CompletenessScore;

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
