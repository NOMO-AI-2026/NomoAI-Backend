namespace NomoAI.API.Features.Auth.ResetPassword;

public sealed record ResetPasswordRequest(
    string Email,
    string Otp,
    string NewPassword,
    string ConfirmNewPassword);