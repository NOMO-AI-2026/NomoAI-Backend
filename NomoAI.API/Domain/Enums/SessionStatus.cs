using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NomoAI.API.Domain.Enums
{
    public enum SessionStatus
    {
        Scheduled,
        InProgress,
        Completed,
        Cancelled,
        Missed 
    }
}
