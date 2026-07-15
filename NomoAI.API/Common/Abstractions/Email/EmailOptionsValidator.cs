using Microsoft.Extensions.Options;

namespace NomoAI.API.Common.Email;

public sealed class EmailOptionsValidator
	: IValidateOptions<EmailOptions>
{
	public ValidateOptionsResult Validate(
		string? name,
		EmailOptions options)
	{
		var errors = new List<string>();

		if (string.IsNullOrWhiteSpace(options.Host))
		{
			errors.Add("Email host is required.");
		}

		if (options.Port is <= 0 or > 65535)
		{
			errors.Add("Email port must be between 1 and 65535.");
		}

		if (string.IsNullOrWhiteSpace(options.Username))
		{
			errors.Add("Email username is required.");
		}

		if (string.IsNullOrWhiteSpace(options.Password))
		{
			errors.Add("Email password is required.");
		}

		if (string.IsNullOrWhiteSpace(options.FromAddress))
		{
			errors.Add("Sender email address is required.");
		}
		else if (!IsValidEmail(options.FromAddress))
		{
			errors.Add("Sender email address is invalid.");
		}

		if (string.IsNullOrWhiteSpace(options.FromName))
		{
			errors.Add("Sender name is required.");
		}

		return errors.Count > 0
			? ValidateOptionsResult.Fail(errors)
			: ValidateOptionsResult.Success;
	}

	private static bool IsValidEmail(string email)
	{
		try
		{
			var address = new System.Net.Mail.MailAddress(email);

			return string.Equals(
				address.Address,
				email,
				StringComparison.OrdinalIgnoreCase);
		}
		catch
		{
			return false;
		}
	}
}