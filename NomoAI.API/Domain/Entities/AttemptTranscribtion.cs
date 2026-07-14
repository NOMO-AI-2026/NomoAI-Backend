using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NomoAI.API.Domain.Entities
{
    public class AttemptTranscribtion:BaseEntity<int>
    {
       
        public int AttemptId { get; set; }
        
        public string TranscribedText { get; set; } = string.Empty;

        public string DetectedLanguage { get; set; } = string.Empty;

        public SessionAttempts Attempt { get; set; } = null!;
    }
}
