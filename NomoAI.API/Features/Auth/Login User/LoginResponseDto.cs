namespace NomoAI.API.Features.Auth.Login_User
{
    public class LoginResponseDto
    {
        public required string UserId { get; set; } 

        public required string Email { get; set; }

        public required string Token { get; set; }

        public required DateTime TokenExpiryTime { get; set; }

        public required string AccessToken { get; set; }

        public required DateTime AccessTokenExpiresAt { get; set; }

        public required string RefreshToken { get; set; }

        public required DateTime RefreshTokenExpiresAt { get; set; }

        public string UserRole { get; set; } = string.Empty;

        public static LoginResponseDto Create(
            string userId,
            string email,
            string accessToken,
            DateTime accessTokenExpiresAt,
            string refreshToken,
            DateTime refreshTokenExpiresAt,
            string userRole)
        {
            return new LoginResponseDto
            {
                UserId = userId,
                Email = email,
                Token = accessToken,
                TokenExpiryTime = accessTokenExpiresAt,
                AccessToken = accessToken,
                AccessTokenExpiresAt = accessTokenExpiresAt,
                RefreshToken = refreshToken,
                RefreshTokenExpiresAt = refreshTokenExpiresAt,
                UserRole = userRole
            };
        }
    }
}
