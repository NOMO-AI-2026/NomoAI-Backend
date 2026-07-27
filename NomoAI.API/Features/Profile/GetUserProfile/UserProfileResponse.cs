using NomoAI.API.Domain.Enums;

namespace NomoAI.API.Features.Profile.GetUserProfile
{
	public class UserProfileResponse
	{
		public string FullName { get; set; }
		public string Email { get; set; }
		public string PhoneNumber { get; set; }
		public Gender gender { get; set; }
		public int Age { get; set; }
		public DoctorData DoctorSpecificData { get; set; }
	
	}
}
