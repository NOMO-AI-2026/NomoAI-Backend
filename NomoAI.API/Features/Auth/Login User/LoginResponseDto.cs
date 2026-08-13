namespace NomoAI.API.Features.Auth.Login_User
{
    public class LoginResponseDto
    {
        public required string UserId { get; set; } 

        public required string Email { get; set; }

        public required string AccessToken { get; set; }

        public required DateTime AccessTokenExpiresAt { get; set; }

        public string UserRole { get; set; } = string.Empty;

        public static LoginResponseDto Create(
            string userId,
            string email,
            string accessToken,
            DateTime accessTokenExpiresAt,
            string userRole)
        {
            return new LoginResponseDto
            {
                UserId = userId,
                Email = email,
                AccessToken = accessToken,
                AccessTokenExpiresAt = accessTokenExpiresAt,
                UserRole = userRole
            };
        }
    }
}
