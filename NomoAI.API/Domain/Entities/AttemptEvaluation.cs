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

        public decimal AttemptId { get; set; }

        public decimal AccuracyScore { get; set; }

        public decimal FluencyScore { get; set; }

        public decimal PronunciationScore { get; set; }

        public decimal CompletenessScore { get; set; }

        public decimal OverallScore => AccuracyScore + FluencyScore + PronunciationScore + CompletenessScore;

        public string? Feedback { get; set; }

        public bool IsSuccessful { get; set; }

        public SessionAttempts Attempt { get; set; } = null!;


    }
}
