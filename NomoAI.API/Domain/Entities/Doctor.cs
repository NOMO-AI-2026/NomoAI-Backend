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

        /// <summary>Public URL of the professional practice license. Required for new Doctor registration.</summary>
        public string? PracticeLicenseUrl { get; set; }

        /// <summary>Public URL of the Doctors' Syndicate membership card. Required for new Doctor registration.</summary>
        public string? SyndicateCardUrl { get; set; }

        /// <summary>Replacement practice license awaiting admin review. Active URL stays in use until accepted.</summary>
        public string? PendingPracticeLicenseUrl { get; set; }

        /// <summary>Replacement syndicate card awaiting admin review. Active URL stays in use until accepted.</summary>
        public string? PendingSyndicateCardUrl { get; set; }

        public ApplicationUser User { get; set; } = null!;

        public ICollection<Children> Children { get; set; }

        public ICollection<Payment>Payments { get; set; }

        public DoctorCreditWallet creditWallet { get; set; }

        public ICollection<DoctorPlanPurchase> PlanPurchases { get; set; }

        public ICollection<DoctorTransaction> Transactions { get; set; }
    }
}
