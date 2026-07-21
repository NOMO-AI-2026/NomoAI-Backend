using NomoAI.API.Domain.Enums;
using NomoAI.API.Features.Profile.GetUserProfile;

namespace NomoAI.API.Features.Profile.UpdateUserProfile
{
    public class UpdateProfileRequest
    {
        public string FullName { get; set; }

        public string PhoneNumber { get; set; }

        public Gender gender { get; set; }

        public int Age { get; set; }

        public DoctorData? DoctorSpecificData { get; set; }
    }
}
