using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NomoAI.API.Domain.Entities
{
    public class Parent:BaseEntity<int> 
    {
        public string UserId { get; set; }

        public ApplicationUser User { get; set; } = null!;
    }
}
