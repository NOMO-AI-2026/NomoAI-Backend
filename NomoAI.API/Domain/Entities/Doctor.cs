using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NomoAI.API.Domain.Entities
{
    public class Doctor:BaseEntity<int>
    {
        public string UserId { get; set; }
        public int? YearsOfExperience { get; set; }
        public string? ClinicName { get; set; } 
        public string? ProfessionalBio { get; set; }
        public bool IsApproved { get; set; }
        public ApplicationUser User { get; set; } = null!;

        public ICollection<Children> Children { get; set; }

    }
}
