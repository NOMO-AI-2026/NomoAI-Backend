using NomoAI.API.Common.Abstractions;

namespace NomoAI.API.Features.Auth
{
    public static class AuthErrors
    {
        public static readonly Error InvalidCredentials = new("Auth.InvalidCredentials", "Invalid username or password.", 401);
        public static readonly Error UserAlreadyExists = new("Auth.UserAlreadyExists", "A user with the given email already exists.", 409);
        public static readonly Error UserNotFound = new("Auth.UserNotFound", "User not found.", 404);
        public static readonly Error InvalidToken = new("Auth.InvalidToken", "The provided token is invalid or expired.", 401);
        public static readonly Error UnauthorizedAccess = new("Auth.UnauthorizedAccess", "You do not have permission to access this resource.", 403);
    }
}
