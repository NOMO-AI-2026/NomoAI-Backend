using MediatR;
using NomoAI.API.Common.Abstractions;
using NomoAI.API.Common.Jwt;

namespace NomoAI.API.Features.Auth.Logout;

public sealed class LogoutHandler
    : IRequestHandler<LogoutCommand, Result>
{
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly ILogger<LogoutHandler> _logger;

    public LogoutHandler(
        IRefreshTokenService refreshTokenService,
        ILogger<LogoutHandler> logger)
    {
        _refreshTokenService = refreshTokenService;
        _logger = logger;
    }

    public async Task<Result> Handle(
        LogoutCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(request.RefreshToken))
            {
                await _refreshTokenService.RevokeAsync(
                    request.RefreshToken,
                    cancellationToken);
            }
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Logout refresh-token revocation failed.");
        }

        return Result.Success();
    }
}
