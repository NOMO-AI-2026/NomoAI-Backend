using NomoAI.API.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NomoAI.API.Domain.Entities
{
    public class Activity: BaseEntity<int>
    {
        public int ChildId { get; set; }
        public ActivityTargetType ActivityTarget { get; set; }
        public string Content { get; set; } = string.Empty;
        public int EstimatedDurationMinutes { get; set; }
        public Children Child { get; set; } = null!;
        public ICollection<Session> Sessions { get; set; }
    }
}
