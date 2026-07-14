using MediatR;
using NomoAI.API.Common.Abstractions;

namespace NomoAI.API.Features.Auth.Login_User
{
    public class LoginCommand:IRequest<Result<LoginResponseDto>>
    {
        public string Email { get; set; }

        public string Password { get; set; }
    }
}
