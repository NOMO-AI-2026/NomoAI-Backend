using MediatR;
using NomoAI.API.Persistence;
using NomoAI.API.Common.Abstractions;
using Microsoft.AspNetCore.Identity;
using NomoAI.API.Domain.Entities;
using NomoAI.API.Common.Enums;

namespace NomoAI.API.Features.Auth.Register_User
{
    public class RegisterUserHandler(AppDbContext dbContext) : IRequestHandler<RegisterUserCommand, Result<RegisterResponseDto>>
    {

        private readonly AppDbContext _appDbContext = dbContext;
        public async Task<Result<RegisterResponseDto>> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
        {
            bool isUsernameTaken = _appDbContext.Users.Any(u => u.Email == request.Username);

            if (isUsernameTaken)
            {
                return Result.Failure<RegisterResponseDto>(AuthErrors.UserAlreadyExists);
            }

            // create application user
            var user = new ApplicationUser
            {
                Id = Guid.NewGuid().ToString(),
                UserName = request.Username,
                Email = request.Username,
                Fullname = request.FullName,
                Age = request.Age,
                Gender = request.Gender
            };

            // hash password
            var hasher = new PasswordHasher<ApplicationUser>();
            user.PasswordHash = hasher.HashPassword(user, request.Password);

            _appDbContext.Users.Add(user);
            await _appDbContext.SaveChangesAsync(cancellationToken);

            // ensure role exists
            var roleName = request.Role == UserRole.Doctor ? "Doctor" : "Parent";
            var role = _appDbContext.Roles.FirstOrDefault(r => r.Name == roleName);
            if (role == null)
            {
                role = new IdentityRole
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = roleName,
                    NormalizedName = roleName.ToUpper()
                };
                _appDbContext.Roles.Add(role);
                await _appDbContext.SaveChangesAsync(cancellationToken);
            }

            // add user role link
            var userRole = new IdentityUserRole<string>
            {
                UserId = user.Id,
                RoleId = role.Id
            };
            _appDbContext.UserRoles.Add(userRole);

            // add corresponding Doctor or Parent entity
            if (request.Role == UserRole.Doctor)
            {
                var doctor = new Doctor
                {
                    UserId = user.Id
                };
                _appDbContext.Add(doctor);
            }
            else
            {
                var parent = new Parent
                {
                    UserId = user.Id
                };
                _appDbContext.Add(parent);
            }

            await _appDbContext.SaveChangesAsync(cancellationToken);

            var response = new RegisterResponseDto
            {
                UserId = user.Id,
                FullName = user.Fullname,
                Username = user.Email
            };

            return Result.Success(response);
        }
    }
}
