using NomoAI.API.Common.Enums;
using NomoAI.API.Domain.Entities;

namespace NomoAI.API.Common.Roles
{
    public interface IRoleManger
    {
        Task<bool> AddToRole(ApplicationUser user, UserRole userRole);

        Task<bool>DeleteRolesFromUser(ApplicationUser user);
    }
}
