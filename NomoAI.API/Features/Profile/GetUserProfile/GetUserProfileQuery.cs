using MediatR;
using NomoAI.API.Common.Abstractions;

namespace NomoAI.API.Features.Profile.GetUserProfile
{
    public record GetUserProfileQuery(string? UserId) : IRequest<Result<UserProfileResponse>>;
}
