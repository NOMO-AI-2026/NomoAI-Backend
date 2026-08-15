using MediatR;
using NomoAI.API.Common.Enums;
using NomoAI.API.Domain.Enums;
using NomoAI.API.Common.Abstractions;

namespace NomoAI.API.Features.Auth.Register_User
{
    public class RegisterUserCommand: IRequest<Result<RegisterResponseDto>>
    {
        public required string FullName { get; set; }

        public required string Email { get; set; }

        public string? PhoneNumber { get; set; }
        public required string Password { get; set; }

        public int Age { get; set; }

        public required Gender Gender { get; set; }

        public UserRole Role { get; set; }

        public int? YearsOfExperience { get; set; }

        public string? ClinicName { get; set; }

        public string? ProfessionalBio { get; set; }

        public string? IdentityDocumentUrl { get; set; }

        public string? PracticeLicenseUrl { get; set; }

        public string? SyndicateCardUrl { get; set; }

        public string? SyndicateRegistrationNumber { get; set; }
    }
}
