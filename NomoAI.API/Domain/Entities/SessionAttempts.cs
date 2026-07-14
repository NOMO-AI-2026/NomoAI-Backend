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

        public string AudioUrl { get; set; }


    }
}
