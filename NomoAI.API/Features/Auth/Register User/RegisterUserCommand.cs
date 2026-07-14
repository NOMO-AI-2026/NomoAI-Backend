using MediatR;
using NomoAI.API.Common.Enums;
using NomoAI.API.Domain.Enums;
using NomoAI.API.Common.Abstractions;

namespace NomoAI.API.Features.Auth.Register_User
{
    public class RegisterUserCommand: IRequest<Result<RegisterResponseDto>>
    {
        public required string FullName { get; set; }

        public required string Email { get; set; }

        public required string Password { get; set; }

        public int Age { get; set; }

        public required Gender Gender { get; set; }

        public UserRole Role { get; set; }
    }
}
