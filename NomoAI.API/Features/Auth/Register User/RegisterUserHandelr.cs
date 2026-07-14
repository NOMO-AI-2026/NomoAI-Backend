using MediatR;
using NomoAI.API.Persistence;
using NomoAI.API.Common.Abstractions;
using Microsoft.AspNetCore.Identity;
using NomoAI.API.Domain.Entities;
using NomoAI.API.Common.Enums;

namespace NomoAI.API.Features.Auth.Register_User
{
    public class RegisterUserHandler(
      UserManager<ApplicationUser> userManager,
      RoleManager<IdentityRole> roleManager,
      AppDbContext dbContext) : IRequestHandler<RegisterUserCommand, Result<RegisterResponseDto>>
    {
        public async Task<Result<RegisterResponseDto>> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
        {
            var existingUser = await userManager.FindByEmailAsync(request.Email);
            if (existingUser is not null)
            {
                return Result.Failure<RegisterResponseDto>(AuthErrors.UserAlreadyExists);
            }

            var user = new ApplicationUser
            {
                UserName = request.Email        ,
                Email = request.Email,
                Fullname = request.FullName,
                Age = request.Age,
                Gender = request.Gender
            };

            var createResult = await userManager.CreateAsync(user, request.Password);
            if (!createResult.Succeeded)
            {
                var errorMessage = string.Join(", ", createResult.Errors.Select(e => e.Description));
                return Result.Failure<RegisterResponseDto>(AuthErrors.UserRegistrationFailed);
            }

            var roleName = request.Role == UserRole.Doctor ? "Doctor" : "Parent";
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new IdentityRole(roleName));
            }
            await userManager.AddToRoleAsync(user, roleName);

            if (request.Role == UserRole.Doctor)
            {
                dbContext.Add(new Doctor { UserId = user.Id });
            }
            else
            {
                dbContext.Add(new Parent { UserId = user.Id });
            }
            await dbContext.SaveChangesAsync(cancellationToken);

            return Result.Success(new RegisterResponseDto
            {
                UserId = user.Id,
                FullName = user.Fullname,
                Username = user.Email
            });
        }
    }
}
