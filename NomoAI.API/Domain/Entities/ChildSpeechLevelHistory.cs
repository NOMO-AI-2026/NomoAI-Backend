using NomoAI.API.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NomoDoc.Domain.Entities
{
    public class ChildSpeechLevelHistory:BaseEntity<int>
    {
        public int ChildId { get; set; }
        public int PreviousSpeechLevelId { get; set; }
        public int NewSpeechLevelId { get; set; }
        public DateTime ChangedAt { get; set; }
        public string? ChangeReasons { get; set; } 
        public Children Child { get; set; } = null!;
        public SpeechLevel PreviousSpeechLevel { get; set; } = null!;
        public SpeechLevel NewSpeechLevel { get; set; } = null!;
    }
}
