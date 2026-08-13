using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using NomoAI.API.Common.Abstractions;
using NomoAI.API.Common.Jwt;
using NomoAI.API.Domain.Entities;
using NomoAI.API.Features.Auth.Login_User;
using NomoAI.API.Persistence;

namespace NomoAI.API.Features.Auth.Refresh;

public sealed class RefreshHandler
    : IRequestHandler<RefreshCommand, Result<LoginResponseDto>>
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

    public async Task<Result<LoginResponseDto>> Handle(
        RefreshCommand request,
        CancellationToken cancellationToken)
    {
        Result<RefreshTokenRotationResult> rotationResult =
            await _refreshTokenService.RotateAsync(
                request.RefreshToken,
                cancellationToken);

        if (rotationResult.IsFailure)
        {
            return Result.Failure<LoginResponseDto>(
                rotationResult.Error);
        }

        ApplicationUser? user =
            await _userManager.FindByIdAsync(
                rotationResult.Value.User.Id);

        if (user is null || user.IsDeleted)
        {
            return Result.Failure<LoginResponseDto>(
                AuthErrors.InvalidRefreshToken);
        }

        if (!user.EmailConfirmed)
        {
            return Result.Failure<LoginResponseDto>(
                AuthErrors.EmailNotConfirmed);
        }

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
                return Result.Failure<LoginResponseDto>(
                    AuthErrors.DoctorNotApproved);
            }
        }

        var (token, expiration) =
            await _jwtService.GenerateTokenAsync(user);

        var response = LoginResponseDto.Create(
            user.Id,
            user.Email!,
            token,
            expiration,
            rotationResult.Value.RawRefreshToken,
            rotationResult.Value.RefreshTokenExpiresAtUtc,
            roles.FirstOrDefault()!);

        return Result<LoginResponseDto>.Success(response);
    }
}
