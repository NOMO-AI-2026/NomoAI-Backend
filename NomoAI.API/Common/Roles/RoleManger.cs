using Microsoft.AspNetCore.Identity;
using NomoAI.API.Common.Enums;
using NomoAI.API.Domain.Entities;
using NomoAI.API.Persistence;

namespace NomoAI.API.Common.Roles
{
    public class RoleManger : IRoleManger
    {
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly AppDbContext dbContext;
        private readonly IConfiguration _configuration;

        public RoleManger(
            RoleManager<IdentityRole> roleManager,
            UserManager<ApplicationUser> userManager,
            AppDbContext dbContext,
            IConfiguration configuration)
        {
            _roleManager = roleManager;
            _userManager = userManager;
            this.dbContext = dbContext;
            _configuration = configuration;
        }

        public async Task<bool> AddToRole(
            ApplicationUser user,
            UserRole userRole,
            DoctorRegistrationProfile? doctorProfile = null)
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
                Doctor doctor = new Doctor
                {
                    UserId = user.Id,
                    YearsOfExperience = doctorProfile?.YearsOfExperience,
                    ClinicName = doctorProfile?.ClinicName,
                    ProfessionalBio = doctorProfile?.ProfessionalBio,
                    IsApproved = false
                };
                dbContext.Add(doctor);
                await dbContext.SaveChangesAsync();

                DoctorCreditWallet wallet = new DoctorCreditWallet
                {
                    DoctorId = doctor.Id,
                    AvailableMinutes = _configuration.GetValue<int>("AppGeneralSettings:NumberOfFreeHours") * 60,
                    UpdatedAtUtc = DateTime.UtcNow,
                };
                dbContext.Add(wallet);
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
