using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using NomoAI.API.Common.Abstractions;
using NomoAI.API.Common.Jwt;
using NomoAI.API.Domain.Entities;
using NomoAI.API.Persistence;

namespace NomoAI.API.Features.Auth.Login_User
{
    public class LoginCommandHandler : IRequestHandler<LoginCommand, Result<LoginAuthResult>>
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IJwtService _jwtTokenService;
        private readonly IRefreshTokenService _refreshTokenService;
        private readonly ILogger<LoginCommandHandler> _logger;
        private readonly AppDbContext _dbContext;

        public LoginCommandHandler(
            UserManager<ApplicationUser> userManager,
            IJwtService jwtTokenService,
            IRefreshTokenService refreshTokenService,
            AppDbContext dbContext,
            ILogger<LoginCommandHandler> logger)
        {
            _userManager = userManager;
            _jwtTokenService = jwtTokenService;
            _refreshTokenService = refreshTokenService;
            _logger = logger;
            _dbContext = dbContext;
        }

        public async Task<Result<LoginAuthResult>> Handle(LoginCommand request, CancellationToken cancellationToken)
        {
            // 1. Find user by email
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user is null || user.IsDeleted)
            {
                return Result.Failure<LoginAuthResult>(AuthErrors.InvalidCredentials);
            }

            // 2. Check password
            var isPasswordValid = await _userManager.CheckPasswordAsync(user, request.Password);
            if (!isPasswordValid)
            {
                // Optional: track failed attempts / lockout
                await _userManager.AccessFailedAsync(user);
                return Result.Failure<LoginAuthResult>(AuthErrors.InvalidCredentials);
            }

            if (!user.EmailConfirmed)
            {
                return Result.Failure<LoginAuthResult>(AuthErrors.EmailNotConfirmed);
            }
            var roles = await _userManager.GetRolesAsync(user);
            string role = roles.FirstOrDefault();

            if(role == "Doctor")
            {
                bool isApproved = await _dbContext.Doctor
                    .Where(d => d.UserId == user.Id && !d.IsDeleted)
                    .Select(d => d.IsApproved)
                    .FirstOrDefaultAsync();
                if (!isApproved)
                {
                    return Result.Failure<LoginAuthResult>(AuthErrors.DoctorNotApproved);
                }
            }

            await _userManager.ResetAccessFailedCountAsync(user);


            var (token, expiration) = await _jwtTokenService.GenerateTokenAsync(user);

            Result<IssuedRefreshToken> refreshResult =
                await _refreshTokenService.IssueAsync(user, cancellationToken);

            if (refreshResult.IsFailure)
            {
                _logger.LogError(
                    "Login succeeded for user {UserId} but refresh token issuance failed: {ErrorCode}.",
                    user.Id,
                    refreshResult.Error.Code);

                return Result.Failure<LoginAuthResult>(refreshResult.Error);
            }

            var response = LoginResponseDto.Create(
                user.Id,
                user.Email!,
                token,
                expiration,
                roles.FirstOrDefault()!);

            return Result<LoginAuthResult>.Success(
                new LoginAuthResult(
                    response,
                    refreshResult.Value.RawToken,
                    refreshResult.Value.ExpiresAtUtc));
        }
    }
}
