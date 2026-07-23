using Microsoft.AspNetCore.Identity;
using NomoAI.API.Common.Abstractions;
using NomoAI.API.Common.Enums;
using NomoAI.API.Domain.Entities;
using NomoAI.API.Features.Auth;
using NomoAI.API.Features.Auth.Register_User;
using NomoAI.API.Persistence;

namespace NomoAI.API.Common.Roles
{
    public class RoleManger:IRoleManger
    {
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly AppDbContext dbContext;
        public RoleManger(RoleManager<IdentityRole> roleManager, UserManager<ApplicationUser> userManager, AppDbContext dbContext)
        {
            _roleManager = roleManager;
            _userManager = userManager;
            this.dbContext = dbContext;
        }

        public async Task<bool> AddToRole(ApplicationUser user, UserRole userRole)
        {
            string roleName = userRole.GetRoleName();

            bool roleExists =
                await _roleManager.RoleExistsAsync(
                    roleName);

            if (!roleExists)
            {
                IdentityResult createRoleResult =
                    await _roleManager.CreateAsync(
                        new IdentityRole(roleName));

                if (!createRoleResult.Succeeded)
                {
                    return false;
                }
            }

            IdentityResult addToRoleResult =
                await _userManager.AddToRoleAsync(
                    user,
                    roleName);

            if (!addToRoleResult.Succeeded)
            {
                return false;
            }

            if (userRole == UserRole.Doctor)
            {
                dbContext.Add(
                    new Doctor
                    {
                        UserId = user.Id
                    });
            }
            else if (userRole == UserRole.Parent)
            {
                dbContext.Add(
                    new Parent
                    {
                        UserId = user.Id
                    });
            }
            await dbContext.SaveChangesAsync();

            return true;
        }

        public async Task<bool> DeleteRolesFromUser(ApplicationUser user)
        {
            var roles = await _userManager.GetRolesAsync(user);

            var result = await _userManager.RemoveFromRolesAsync(user, roles);

            if (result.Succeeded)
            {
                return true;
            }
            else return false;
        }
        
    }
}
