namespace NomoAI.API.Features.Auth.Login_User
{
    public class LoginRequestDto
    {
        public required string Email { get; set; }

        public required string Password { get; set; }
    }
}
