using NomoAI.API.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NomoDoc.Domain.Entities
{
    public class SessionSummary:BaseEntity<int>
    {
        public int SessionId { get; set; }
        public int TotalAttempts { get; set; }

        public int SuccessfulAttempts { get; set; }

        public decimal AverageScore { get; set; }
        public decimal BestScore { get; set; }
        public decimal ImprovementPercentage { get; set; }
        public string Strengths { get; set; }
        public string Weaknesses { get; set; }
        public string Recommendations { get ; set; }
        public string  AISummary { get; set; } = null!;

        public Session session { get; set; }
    }
}
