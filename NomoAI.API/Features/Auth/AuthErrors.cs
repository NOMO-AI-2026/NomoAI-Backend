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
        public static readonly Error EmailNotConfirmed = new("Auth.EmailNotConfirmed", "You must confirm your email address.", 400);
        public static readonly Error UserRegistrationFailed = new("Auth.UserRegistrationFailed", "User Registration Failed, Try Again", 400);
		public static Error PasswordResetFailed(string description) =>new("Auth.PasswordResetFailed", description, 400);
        public static Error ChangePasswordFailed(
    string description) =>
    new(
        "Auth.ChangePasswordFailed",
        description,
        StatusCodes.Status400BadRequest);
        public static readonly Error IncorrectPassword = new(
    "Auth.IncorrectPassword",
    "The current password is incorrect.",
    StatusCodes.Status400BadRequest);

        public static readonly Error EmailAlreadyInUse = new(
            "Auth.EmailAlreadyInUse",
            "The given email address is already in use.",
            StatusCodes.Status409Conflict);

        public static readonly Error EmailUnchanged = new(
            "Auth.EmailUnchanged",
            "The new email address must be different from the current email address.",
            StatusCodes.Status400BadRequest);

        public static readonly Error InvalidEmailChangeToken = new(
            "Auth.InvalidEmailChangeToken",
            "The email change token is invalid or expired.",
            StatusCodes.Status400BadRequest);

        public static readonly Error EmailDeliveryFailed = new(
    "Auth.EmailDeliveryFailed",
    "The confirmation email could not be sent. Please try again later.",
    StatusCodes.Status503ServiceUnavailable);

        public static Error EmailChangeFailed(
            string description) =>
            new(
                "Auth.EmailChangeFailed",
                description,
                StatusCodes.Status400BadRequest);
    }
}
