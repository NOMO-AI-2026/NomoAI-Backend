using NomoAI.API.Common.Abstractions;

namespace NomoAI.API.Features.Child
{
    public static class ChildrenErrors
    {
        public static Error SpeechLevelNotFound = new Error("Children.SpeechLevelNotFound", "Speech level not found.", 404);
        public static Error DoctorNotFound = new Error("Children.DoctorNotFound", "Doctor not found.", 404);
    }
}
