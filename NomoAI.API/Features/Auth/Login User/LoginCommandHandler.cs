using MediatR;
using Microsoft.AspNetCore.Identity;
using NomoAI.API.Common.Abstractions;
using NomoAI.API.Common.Jwt;
using NomoAI.API.Domain.Entities;

namespace NomoAI.API.Features.Auth.Login_User
{
    public class LoginCommandHandler : IRequestHandler<LoginCommand, Result<LoginResponseDto>>
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IJwtService _jwtTokenService;
        private readonly ILogger<LoginCommandHandler> _logger;

        public LoginCommandHandler(
            UserManager<ApplicationUser> userManager,
            IJwtService jwtTokenService,
            ILogger<LoginCommandHandler> logger)
        {
            _userManager = userManager;
            _jwtTokenService = jwtTokenService;
            _logger = logger;
        }

        public async Task<Result<LoginResponseDto>> Handle(LoginCommand request, CancellationToken cancellationToken)
        {
            // 1. Find user by email
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user is null)
            {
                return Result.Failure<LoginResponseDto>(AuthErrors.InvalidCredentials);
            }

            // 2. Check password
            var isPasswordValid = await _userManager.CheckPasswordAsync(user, request.Password);
            if (!isPasswordValid)
            {
                // Optional: track failed attempts / lockout
                await _userManager.AccessFailedAsync(user);
                return Result.Failure<LoginResponseDto>(AuthErrors.InvalidCredentials);
            }

            //if (!user.EmailConfirmed)
            //{
            //    return Result.Failure<LoginResponseDto>(AuthErrors.EmailNotConfirmed);
            //}

            await _userManager.ResetAccessFailedCountAsync(user);

            var roles = await _userManager.GetRolesAsync(user);

            var (token, expiration) = await _jwtTokenService.GenerateTokenAsync(user);
           
            var response = new LoginResponseDto
            {
                UserId = user.Id,
                Email = user.Email,
                Token = token,
                TokenExpiryTime = expiration,
                UserRole = roles.FirstOrDefault()!
            };


            return Result<LoginResponseDto>.Success(response);
        }
    }
}
