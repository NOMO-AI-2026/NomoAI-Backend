using NomoAI.API.Domain.Enums;

namespace NomoAI.API.Features.Children.GetChildDetails
{
    public class ChildDeatailsResponse
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public DateOnly DateOfBirth { get; set; }
        public Gender Gender { get; set; }
        public DateOnly TherapyStartDate { get; set; }
        public int Age { get; set; }
        public string? ParentFullName { get; set; } = string.Empty;

        public  string? ParentEmail { get; set; }

        public string? ParentPhoneNumber { get; set; }
    }
}
