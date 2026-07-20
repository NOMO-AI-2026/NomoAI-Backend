namespace NomoAI.API.Common.Abstractions.Email
{
	public class FrontendOptions
	{
		public const string SectionName = "Frontend";
        public string BaseUrl { get; init; } = string.Empty;
        public string ResetPasswordUrl { get; init; } = string.Empty;
        public string ConfirmEmailUrl { get; init; } = string.Empty;


        public string ConfirmEmailChangePageUrl { get; init; }
      = string.Empty;
    }
}
