using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using NomoAI.API.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NomoAI.API.Domain.Entities
{
    public class SessionAttempts:BaseEntity<int>    
    {

        public int SessionId { get; set; }

        public  int AttemptNumber { get; set; }

        /// <summary>Nullable: audio may not be persisted to storage for every attempt.</summary>
        public string? AudioUrl { get; set; }

        public Session Session { get; set; } = null!;


    }
}
