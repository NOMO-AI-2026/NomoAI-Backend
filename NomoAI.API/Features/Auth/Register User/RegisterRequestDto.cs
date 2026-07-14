using NomoAI.API.Common.Enums;
using NomoAI.API.Domain.Enums;

namespace NomoAI.API.Features.Auth.Register_User
{
    public class RegisterRequestDto
    {
        public required string FullName { get; set; }

        public required string Email { get; set; }

        public required string Password { get; set; }
       
        public int Age { get; set; }
        /// <summary>
        /// Male = 0 ,Female=1
        /// </summary>
        public required Gender Gender { get; set; }
        /// <summary>
        /// Doctor = 0, Parent = 1
        /// </summary>
        public UserRole Role { get; set; }
    }
}
