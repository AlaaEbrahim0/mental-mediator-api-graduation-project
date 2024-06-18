using Application.Contracts;

namespace Application.Utilities;
public class MailTemplates
{
	private readonly IWebRootFileProvider _fileProvider;

	public MailTemplates(IWebRootFileProvider fileProvider)
	{
		_fileProvider = fileProvider;
	}

	public string EmailConfirmation(string username, string confirmationLink)
	{
		string htmlTemplate = _fileProvider.ReadFromWebRoot("emailConfirmationTemplate.html");

		htmlTemplate = htmlTemplate.Replace("{username}", username)
								   .Replace("{confirmationLink}", confirmationLink);

		return htmlTemplate;
	}
	public string SuccessfulEmailConfirmation()
	{
		string htmlTemplate = _fileProvider.ReadFromWebRoot("successfulEmailConfirmation.html");
		return htmlTemplate;
	}
	public string ResetPassword(string resetPasswordLink)
	{
		string htmlTemplate = _fileProvider.ReadFromWebRoot("resetPasswordTemplate.html");

		htmlTemplate = htmlTemplate.Replace("{resetPasswordLink}", resetPasswordLink);
		return htmlTemplate;
	}

}
