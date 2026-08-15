using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using NomoAI.API.Common.Abstractions;
using NomoAI.API.Common.Jwt;
using NomoAI.API.Domain.Entities;
using NomoAI.API.Persistence;

namespace NomoAI.API.Features.Auth.Refresh;

public sealed class RefreshHandler
    : IRequestHandler<RefreshCommand, Result<RefreshAuthResult>>
{
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly IJwtService _jwtService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly AppDbContext _dbContext;

    public RefreshHandler(
        IRefreshTokenService refreshTokenService,
        IJwtService jwtService,
        UserManager<ApplicationUser> userManager,
        AppDbContext dbContext)
    {
        _refreshTokenService = refreshTokenService;
        _jwtService = jwtService;
        _userManager = userManager;
        _dbContext = dbContext;
    }

    public async Task<Result<RefreshAuthResult>> Handle(
        RefreshCommand request,
        CancellationToken cancellationToken)
    {
        Result<RefreshTokenRotationResult> rotationResult =
            await _refreshTokenService.RotateAsync(
                request.RefreshToken,
                cancellationToken);

        if (rotationResult.IsFailure)
        {
            return Result.Failure<RefreshAuthResult>(
                rotationResult.Error);
        }

        ApplicationUser? user =
            await _userManager.FindByIdAsync(
                rotationResult.Value.User.Id);

        if (user is null || user.IsDeleted)
        {
            return Result.Failure<RefreshAuthResult>(
                AuthErrors.InvalidRefreshToken);
        }

        // TEMP: allow refresh without email confirmation (matches login bypass for testing).
        // if (!user.EmailConfirmed)
        // {
        //     return Result.Failure<RefreshAuthResult>(
        //         AuthErrors.EmailNotConfirmed);
        // }

        var roles = await _userManager.GetRolesAsync(user);
        string role = roles.FirstOrDefault();

        if (role == "Doctor")
        {
            bool isApproved = await _dbContext.Doctor
                .Where(d => d.UserId == user.Id && !d.IsDeleted)
                .Select(d => d.IsApproved)
                .FirstOrDefaultAsync(cancellationToken);

            if (!isApproved)
            {
                return Result.Failure<RefreshAuthResult>(
                    AuthErrors.DoctorNotApproved);
            }
        }

        var (token, expiration) =
            await _jwtService.GenerateTokenAsync(user);

        var response = new AccessTokenResponseDto(
            token,
            expiration);

        return Result<RefreshAuthResult>.Success(
            new RefreshAuthResult(
                response,
                rotationResult.Value.RawRefreshToken,
                rotationResult.Value.RefreshTokenExpiresAtUtc));
    }
}
