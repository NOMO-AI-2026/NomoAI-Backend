namespace NomoAI.API.Features.Auth.Login_User
{
    public class LoginResponseDto
    {
        public required string UserId { get; set; } 

        public required string Email { get; set; }

        public required string Token { get; set; }

        public required DateTime TokenExpiryTime { get; set; }

        public string UserRole { get; set; } = string.Empty;
    }
}
