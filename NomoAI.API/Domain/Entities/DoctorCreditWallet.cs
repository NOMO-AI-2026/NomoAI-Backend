namespace NomoAI.API.Domain.Entities
{
    public class DoctorCreditWallet:BaseEntity<int>
    {
        public int DoctorId { get; set; }

        public int AvailableMinutes { get; set; }

        public DateTime UpdatedAtUtc { get; set; }

        public Doctor Doctor { get; set; } 
    }
}
