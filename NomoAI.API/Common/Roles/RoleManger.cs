using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
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
                IList<string> existingRoles =
                    await _userManager.GetRolesAsync(user);

                bool alreadyInRole = existingRoles.Any(role =>
                    string.Equals(role, roleName, StringComparison.OrdinalIgnoreCase));

                if (!alreadyInRole)
                {
                    return false;
                }
            }

            if (userRole == UserRole.Doctor)
            {
                await UpsertDoctorProfileAsync(user, doctorProfile);
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

        private async Task UpsertDoctorProfileAsync(
            ApplicationUser user,
            DoctorRegistrationProfile? doctorProfile)
        {
            Doctor? existingDoctor = await dbContext.Doctor
                .Where(doctor => doctor.UserId == user.Id)
                .OrderBy(doctor => doctor.IsDeleted)
                .ThenByDescending(doctor => doctor.Id)
                .FirstOrDefaultAsync();

            if (existingDoctor is not null)
            {
                ApplyDoctorProfile(existingDoctor, doctorProfile);
                existingDoctor.IsDeleted = false;
                existingDoctor.IsApproved = false;

                await EnsureDoctorWalletAsync(existingDoctor.Id);
                return;
            }

            Doctor doctor = new Doctor
            {
                UserId = user.Id,
                IsApproved = false
            };
            ApplyDoctorProfile(doctor, doctorProfile);
            dbContext.Add(doctor);
            await dbContext.SaveChangesAsync();

            await EnsureDoctorWalletAsync(doctor.Id);
        }

        private static void ApplyDoctorProfile(
            Doctor doctor,
            DoctorRegistrationProfile? doctorProfile)
        {
            doctor.YearsOfExperience = doctorProfile?.YearsOfExperience;
            doctor.ClinicName = doctorProfile?.ClinicName;
            doctor.ProfessionalBio = doctorProfile?.ProfessionalBio;
            doctor.IdentityDocumentUrl = doctorProfile?.IdentityDocumentUrl?.Trim();
            doctor.PracticeLicenseUrl = doctorProfile?.PracticeLicenseUrl?.Trim();
            doctor.SyndicateCardUrl = doctorProfile?.SyndicateCardUrl?.Trim();
            doctor.SyndicateRegistrationNumber =
                string.IsNullOrWhiteSpace(doctorProfile?.SyndicateRegistrationNumber)
                    ? doctorProfile?.SyndicateRegistrationNumber
                    : doctorProfile.SyndicateRegistrationNumber.Trim();
        }

        private async Task EnsureDoctorWalletAsync(int doctorId)
        {
            DoctorCreditWallet? wallet = await dbContext.DoctorCreditWallets
                .Where(item => item.DoctorId == doctorId)
                .OrderBy(item => item.IsDeleted)
                .ThenByDescending(item => item.Id)
                .FirstOrDefaultAsync();

            if (wallet is not null)
            {
                wallet.IsDeleted = false;
                return;
            }

            dbContext.Add(
                new DoctorCreditWallet
                {
                    DoctorId = doctorId,
                    AvailableMinutes =
                        _configuration.GetValue<int>("AppGeneralSettings:NumberOfFreeHours") * 60,
                    UpdatedAtUtc = DateTime.UtcNow,
                });
        }
    }
}
