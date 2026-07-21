using MediatR;
using NomoAI.API.Common.Abstractions;

namespace NomoAI.API.Features.Profile.UpdateUserProfile
{
    public record UpdateUserProfileCommand(string UserId) : IRequest<Result<bool>>
    {
        public UpdateProfileRequest Request { get; set; } = new UpdateProfileRequest();
    }
}
