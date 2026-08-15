using NomoAI.API.Common.Abstractions;

namespace NomoAI.API.Features.Profile
{
    public static class ProfileErrors
    {
        public static Error UserNotFound => new Error("Profile.UserNotFound", "User not found.", 404);

        public static Error DoctorNotFound => new Error(
            "Profile.DoctorNotFound",
            "Doctor profile not found.",
            404);
    }
}
