using NomoAI.API.Domain.Enums;

namespace NomoAI.API.Features.Children.Add_Child
{
    public class AddChildrenRequestDto
    {
        public required string FullName { get; set; } = string.Empty;
        public DateOnly DateOfBirth { get; set; }
        public Gender Gender { get; set; }
        public DateOnly TherapyStartDate { get; set; }
        public int Age { get; set; }
        public int SpeechLevelId { get; set; }
    }
}
