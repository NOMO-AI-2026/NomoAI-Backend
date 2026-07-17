using MediatR;
using NomoAI.API.Common.Abstractions;
using NomoAI.API.Domain.Enums;

namespace NomoAI.API.Features.Children.Add_Child
{
    public class AddChildCommand : IRequest<Result<AddChildResponseDto>>
    {
        public required string FullName { get; set; }
        public DateOnly DateOfBirth { get; set; }
        public Gender Gender { get; set; }
        public DateOnly TherapyStartDate { get; set; }
        public int Age { get; set; }
        public int SpeechLevelId { get; set; }

        public string UserId { get; set; }
    }

}
