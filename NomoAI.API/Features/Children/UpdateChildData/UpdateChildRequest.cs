using NomoAI.API.Domain.Enums;

namespace NomoAI.API.Features.Children.UpdateChildData
{
    public class UpdateChildRequest
    {
        public string FullName { get; set; } = string.Empty;
        public DateOnly DateOfBirth { get; set; }
        public Gender Gender { get; set; }
        public DateOnly TherapyStartDate { get; set; }
        public int Age { get; set; }

        public int SpeechLevelId { get; set; }

        public string? SpeechLevelChangeReasons { get; set; }
    }
}
