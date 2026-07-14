using NomoAI.API.Domain.Entities;

namespace NomoAI.API.Common.Jwt
{
    public interface IJwtService
    {
        Task<(string token, DateTime expiration)> GenerateTokenAsync(ApplicationUser user);
    }
}
