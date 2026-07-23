using NomoAI.API.Common.Enums;

namespace NomoAI.API.Common.Roles
{
    public static class RolesNames
    {
        public static string GetRoleName(this UserRole role)
        {
            return role switch
            {
                UserRole.Doctor => "Doctor",
                UserRole.Parent => "Parent",
                UserRole.Admin => "Admin",
                _ => throw new ArgumentOutOfRangeException(nameof(role), role, "Unknown role.")
            };
        }
    }
}
