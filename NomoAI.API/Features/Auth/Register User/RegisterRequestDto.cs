using NomoAI.API.Common.Enums;
using NomoAI.API.Domain.Enums;

namespace NomoAI.API.Features.Auth.Register_User
{
    public class RegisterRequestDto
    {
        public required string FullName { get; set; }

        public required string Username { get; set; }

        public required string Password { get; set; }

        public int Age { get; set; }

        public required Gender Gender { get; set; }

        public UserRole Role { get; set; }
    }
}
