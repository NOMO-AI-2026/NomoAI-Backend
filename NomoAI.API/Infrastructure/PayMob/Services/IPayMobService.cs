using NomoAI.API.Infrastructure.PayMob.Models;

namespace NomoAI.API.Infrastructure.PayMob.Services
{
    public interface IPayMobService
    {
        Task<string> GetAuthTokenAsync();
        Task<CreateQuickLinkResponse> CreateQuickLinkAsync(QuickLinkRequest request);
    }
}
