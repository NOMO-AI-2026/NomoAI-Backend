using NomoAI.API.Domain.Entities;
using NomoAI.API.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NomoAI.API.Domain.Entities
{
    public class Session:BaseEntity<int>    
    {
        
        public int ChildId { get; set; }

        public int ActivityId { get; set; }

        public string SessionTitle { get; set; }

        public SessionStatus Status { get; set; }

        public DateTime? StartedAt { get; set; }

        public DateTime? EndedAt { get; set; }

        public string? ParentNotes { get; set; }

        public Children Child { get; set; } = null!;

        public Activity Activity { get; set; } = null!;

    }
}
